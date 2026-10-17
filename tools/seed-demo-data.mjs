#!/usr/bin/env node
// Fills the Coffee API with demo data through its own HTTP API (so every business rule holds:
// stock, budget, order numbers and price snapshots stay consistent).
//
//   catalog (admin API):  categories, products, toppings, drinks with recipes and allowed toppings
//   tenant data:          purchases -> warehouse stock, orders in all three statuses
//
// It never creates or edits tenants: the tenant must already exist. The only change to the tenant row is its
// budget: the admin top-up that pays for the demo purchases (completed orders then add their income on top).
//
// Safe to re-run: catalog items are matched by name and skipped when present, and the tenant part is skipped
// when the tenant already has purchases / orders.
//
// Usage (API must be running; Node 22+):
//   node tools/seed-demo-data.mjs
// Settings are taken from the environment, falling back to the dev appsettings files in this repo:
//   COFFEE_API_URL      default: CoffeeApi:BaseUrl of src/CoffeeApi.Tenant/appsettings.json
//   COFFEE_ADMIN_KEY    default: Admin:ApiKey of src/CoffeeApi.Api/appsettings.Development.json
//   COFFEE_TENANT_NAME  default: CoffeeApi:TenantName of src/CoffeeApi.Tenant/appsettings.json
//   COFFEE_TENANT_KEY   default: CoffeeApi:TenantKey of src/CoffeeApi.Tenant/appsettings.json

import { readFileSync } from 'node:fs';
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const root = resolve(dirname(fileURLToPath(import.meta.url)), '..');
const readJson = (path) => {
  try {
    return JSON.parse(readFileSync(resolve(root, path), 'utf8'));
  } catch {
    return {};
  }
};
const tenantApp = readJson('src/CoffeeApi.Tenant/appsettings.json').CoffeeApi ?? {};
const apiDev = readJson('src/CoffeeApi.Api/appsettings.Development.json');

const config = {
  url: (process.env.COFFEE_API_URL ?? tenantApp.BaseUrl ?? 'http://localhost:5264').replace(/\/$/, ''),
  adminKey: process.env.COFFEE_ADMIN_KEY ?? apiDev.Admin?.ApiKey,
  tenantName: process.env.COFFEE_TENANT_NAME ?? tenantApp.TenantName,
  tenantKey: process.env.COFFEE_TENANT_KEY ?? tenantApp.TenantKey,
};
for (const [name, value] of Object.entries(config)) {
  if (!value) {
    console.error(`Missing setting "${name}". Set the COFFEE_* environment variable or fill the appsettings files.`);
    process.exit(1);
  }
}

// ---------- tiny HTTP helpers ----------

async function call(method, path, headers, body) {
  const response = await fetch(config.url + path, {
    method,
    headers: { ...(body !== undefined && { 'Content-Type': 'application/json' }), ...headers },
    body: body !== undefined ? JSON.stringify(body) : undefined,
  });
  const text = await response.text();
  const data = text ? JSON.parse(text) : null;
  if (!response.ok) {
    const reason = data?.errors ? JSON.stringify(data.errors) : data?.title ?? text;
    throw new Error(`${method} ${path} -> ${response.status}: ${reason}`);
  }
  return data;
}

const adminHeaders = { 'X-Admin-Key': config.adminKey };
const tenantHeaders = { 'X-Tenant-Name': config.tenantName, 'X-Api-Key': config.tenantKey };
const admin = (method, path, body) => call(method, `/api/admin${path}`, adminHeaders, body);
const tenant = (method, path, body) => call(method, `/api${path}`, tenantHeaders, body);

/** Creates every item whose name is not there yet; returns a name -> entity map of all of them. */
async function ensure(collection, items, toBody) {
  const existing = new Map((await admin('GET', `/${collection}`)).map((e) => [e.name, e]));
  let created = 0;
  for (const item of items) {
    if (existing.has(item.name)) continue;
    existing.set(item.name, await admin('POST', `/${collection}`, toBody(item)));
    created++;
  }
  console.log(`${collection.padEnd(11)} ${items.length} wanted, ${created} created, ${items.length - created} already there`);
  return existing;
}

const money = (cents) => (cents / 100).toFixed(2);

// ---------- demo catalog ----------

const categoryNames = ['Кофе', 'Чай', 'Холодные напитки', 'Другое'];

// name, unit, pack size (in unit), pack price in cents
const productData = [
  ['Кофе в зёрнах', 'г', 1000, 2500],
  ['Молоко', 'мл', 1000, 120],
  ['Овсяное молоко', 'мл', 1000, 280],
  ['Сливки 33%', 'мл', 500, 250],
  ['Сироп ванильный', 'мл', 700, 600],
  ['Сироп карамельный', 'мл', 700, 600],
  ['Шоколадный соус', 'мл', 500, 550],
  ['Какао-порошок', 'г', 500, 700],
  ['Чай чёрный', 'г', 500, 900],
  ['Чай зелёный', 'г', 500, 1000],
  ['Лимон', 'шт', 10, 300],
  ['Лёд', 'г', 2000, 100],
  ['Сахар', 'г', 1000, 90],
  ['Корица молотая', 'г', 100, 250],
  ['Тоник', 'мл', 1000, 180],
  ['Питьевая вода', 'мл', 5000, 100],
].map(([name, unit, packSize, packPriceCents]) => ({ name, unit, packSize, packPriceCents }));

// name, price in cents, available, recipe per portion as [product, amount]
const toppingData = [
  ['Ванильный сироп', 60, true, [['Сироп ванильный', 15]]],
  ['Карамельный сироп', 60, true, [['Сироп карамельный', 15]]],
  ['Шоколадный соус', 70, true, [['Шоколадный соус', 15]]],
  ['Взбитые сливки', 80, true, [['Сливки 33%', 30]]],
  ['Овсяное молоко', 50, true, [['Овсяное молоко', 50]]],
  ['Корица', 20, true, [['Корица молотая', 1]]],
  ['Дополнительный шот', 100, true, [['Кофе в зёрнах', 9]]],
  ['Сахар', 0, true, [['Сахар', 5]]],
  ['Маршмеллоу', 90, false, []], // out of stock in the catalog: shows the "unavailable" state
].map(([name, priceCents, isAvailable, recipe]) => ({ name, priceCents, isAvailable, recipe }));

const sweet = ['Ванильный сироп', 'Карамельный сироп', 'Овсяное молоко', 'Сахар'];
const milky = [...sweet, 'Шоколадный соус', 'Корица', 'Взбитые сливки', 'Дополнительный шот'];

// name, description, category, price in cents, available, recipe per serving, allowed toppings
const drinkData = [
  ['Эспрессо', 'Классический шот эспрессо', 'Кофе', 150, true, [['Кофе в зёрнах', 18]], ['Дополнительный шот', 'Сахар']],
  ['Американо', 'Эспрессо с горячей водой', 'Кофе', 180, true, [['Кофе в зёрнах', 18], ['Питьевая вода', 150]], ['Ванильный сироп', 'Карамельный сироп', 'Дополнительный шот', 'Сахар', 'Корица']],
  ['Капучино', 'Эспрессо, молоко и плотная молочная пена', 'Кофе', 250, true, [['Кофе в зёрнах', 18], ['Молоко', 150]], milky],
  ['Латте', 'Эспрессо с большим количеством нежного молока', 'Кофе', 280, true, [['Кофе в зёрнах', 18], ['Молоко', 220]], milky],
  ['Флэт уайт', 'Двойной эспрессо с бархатистым молоком', 'Кофе', 290, true, [['Кофе в зёрнах', 36], ['Молоко', 120]], sweet],
  ['Мокка', 'Эспрессо с шоколадом и молоком', 'Кофе', 320, true, [['Кофе в зёрнах', 18], ['Молоко', 180], ['Шоколадный соус', 20]], ['Взбитые сливки', 'Корица', 'Дополнительный шот', 'Овсяное молоко', 'Сахар']],
  ['Раф', 'Эспрессо со сливками и ванильным сиропом', 'Кофе', 330, true, [['Кофе в зёрнах', 18], ['Сливки 33%', 100], ['Сироп ванильный', 15]], ['Корица', 'Дополнительный шот', 'Сахар']],
  ['Тыквенный латте', 'Сезонный напиток, пока недоступен', 'Кофе', 350, false, [['Кофе в зёрнах', 18], ['Молоко', 200]], ['Корица']],
  ['Чёрный чай', 'Крепкий чёрный чай', 'Чай', 150, true, [['Чай чёрный', 5], ['Питьевая вода', 250]], ['Сахар', 'Корица']],
  ['Зелёный чай', 'Лёгкий зелёный чай', 'Чай', 150, true, [['Чай зелёный', 5], ['Питьевая вода', 250]], ['Сахар']],
  ['Чай с лимоном', 'Чёрный чай с долькой лимона', 'Чай', 180, true, [['Чай чёрный', 5], ['Лимон', 1], ['Питьевая вода', 250]], ['Сахар']],
  ['Айс-латте', 'Холодный латте со льдом', 'Холодные напитки', 300, true, [['Кофе в зёрнах', 18], ['Молоко', 200], ['Лёд', 100]], sweet],
  ['Эспрессо-тоник', 'Эспрессо и тоник со льдом', 'Холодные напитки', 310, true, [['Кофе в зёрнах', 18], ['Тоник', 150], ['Лёд', 100]], []],
  ['Домашний лимонад', 'Лимонад на свежем лимоне', 'Холодные напитки', 220, true, [['Лимон', 1], ['Сахар', 20], ['Питьевая вода', 250], ['Лёд', 100]], []],
  ['Какао', 'Горячее какао на молоке', 'Другое', 260, true, [['Какао-порошок', 20], ['Молоко', 200], ['Сахар', 10]], ['Взбитые сливки', 'Маршмеллоу', 'Корица']],
  ['Горячий шоколад', 'Густой горячий шоколад', 'Другое', 290, true, [['Шоколадный соус', 40], ['Молоко', 200]], ['Взбитые сливки', 'Маршмеллоу', 'Корица']],
].map(([name, description, category, priceCents, isAvailable, recipe, toppings]) => ({
  name, description, category, priceCents, isAvailable, recipe, toppings,
}));

// ---------- demo tenant data ----------

// product, packs: bought in two rounds so the purchase history has more than one page
const purchaseRounds = [
  [['Кофе в зёрнах', 4], ['Молоко', 8], ['Овсяное молоко', 2], ['Сливки 33%', 2], ['Сироп ванильный', 1],
   ['Сироп карамельный', 1], ['Шоколадный соус', 2], ['Какао-порошок', 1], ['Чай чёрный', 1], ['Чай зелёный', 1],
   ['Лимон', 2], ['Лёд', 3], ['Сахар', 1], ['Корица молотая', 1], ['Тоник', 2], ['Питьевая вода', 2]],
  [['Кофе в зёрнах', 1], ['Молоко', 2]],
];

// final: what happens to the order after it is placed
const orderData = [
  { final: 'completed', items: [['Латте', 1, ['Ванильный сироп']], ['Эспрессо', 1, []]] },
  { final: 'completed', items: [['Капучино', 2, ['Корица', 'Взбитые сливки']]] },
  { final: 'completed', items: [['Американо', 1, ['Сахар']]] },
  { final: 'completed', items: [['Айс-латте', 2, ['Карамельный сироп']]] },
  { final: 'completed', comment: 'Без маршмеллоу', items: [['Какао', 1, ['Взбитые сливки']]] },
  { final: 'completed', items: [['Чай с лимоном', 3, []]] },
  { final: 'completed', items: [['Флэт уайт', 1, ['Овсяное молоко']]] },
  { final: 'cancelled', items: [['Мокка', 1, ['Дополнительный шот']]] },
  { final: 'cancelled', items: [['Эспрессо-тоник', 2, []]] },
  { final: 'inprogress', comment: 'Для столика 4', items: [['Раф', 1, []]] },
  { final: 'inprogress', items: [['Домашний лимонад', 2, []], ['Горячий шоколад', 1, ['Взбитые сливки']]] },
  { final: 'inprogress', comment: 'Подать вместе', items: [['Латте', 3, ['Ванильный сироп', 'Дополнительный шот']]] },
];

// ---------- run ----------

console.log(`API ${config.url}, tenant "${config.tenantName}"\n`);

const me = await tenant('GET', '/me'); // fails fast with 401 if the tenant name/key are wrong

const categories = await ensure('categories', categoryNames.map((name) => ({ name })), (c) => ({ name: c.name }));
const products = await ensure('products', productData, (p) => p);
const idOf = (map, name, kind) => {
  const entity = map.get(name);
  if (!entity) throw new Error(`${kind} "${name}" was not found`);
  return entity.id;
};
const recipeOf = (recipe) => recipe.map(([product, quantity]) => ({ productId: idOf(products, product, 'Product'), quantity }));

const toppings = await ensure('toppings', toppingData, (t) => ({
  name: t.name, priceCents: t.priceCents, isAvailable: t.isAvailable, ingredients: recipeOf(t.recipe),
}));
const drinks = await ensure('drinks', drinkData, (d) => ({
  name: d.name,
  description: d.description,
  categoryId: idOf(categories, d.category, 'Category'),
  priceCents: d.priceCents,
  isAvailable: d.isAvailable,
  toppingIds: d.toppings.map((t) => idOf(toppings, t, 'Topping')),
  ingredients: recipeOf(d.recipe),
}));

// -- purchases: need budget, which the admin tops up by exactly what is missing
const purchaseCount = (await tenant('GET', '/purchases?pageSize=1')).totalCount;
if (purchaseCount > 0) {
  console.log(`\npurchases   skipped: the tenant already has ${purchaseCount}`);
} else {
  const cost = purchaseRounds.flat().reduce((sum, [name, packs]) => sum + packs * products.get(name).packPriceCents, 0);
  const missing = cost - me.budgetCents;
  if (missing > 0) {
    await admin('POST', `/tenants/${me.id}/budget/top-up`, { amountCents: missing });
    console.log(`\nbudget      topped up by ${money(missing)} to pay for the purchases (${money(cost)})`);
  }
  for (const round of purchaseRounds)
    for (const [name, packs] of round) await tenant('POST', '/purchases', { productId: products.get(name).id, packs });
  console.log(`purchases   ${purchaseRounds.flat().length} created`);
}

// -- orders: placed through the API (takes stock), then completed or cancelled
const orderCount = (await tenant('GET', '/orders?pageSize=1')).totalCount;
if (orderCount > 0) {
  console.log(`orders      skipped: the tenant already has ${orderCount}`);
} else {
  for (const order of orderData) {
    const placed = await tenant('POST', '/orders', {
      comment: order.comment ?? null,
      items: order.items.map(([drink, quantity, extras]) => ({
        drinkId: idOf(drinks, drink, 'Drink'),
        quantity,
        toppingIds: extras.map((t) => idOf(toppings, t, 'Topping')),
      })),
    });
    if (order.final === 'completed') await tenant('POST', `/orders/${placed.id}/complete`);
    if (order.final === 'cancelled') await tenant('POST', `/orders/${placed.id}/cancel`);
  }
  console.log(`orders      ${orderData.length} created`);
}

const after = await tenant('GET', '/me');
const stock = await tenant('GET', '/stock');
console.log(`\ndone. Budget now ${money(after.budgetCents)}, ${stock.length} stock lines.`);
