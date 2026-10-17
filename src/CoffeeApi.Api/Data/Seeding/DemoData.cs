using CoffeeApi.Api.Entities;

namespace CoffeeApi.Api.Data.Seeding;

/// <summary>
/// The demo data as plain values, referring to each other by name. <see cref="DatabaseSeeder"/> turns it into entities.
/// </summary>
internal static class DemoData
{
    // Pictures: Unsplash photos (free to use), requested as 600x600 crops so they share one size and aspect ratio.
    private static string Photo(string id) => $"https://images.unsplash.com/photo-{id}?w=600&h=600&fit=crop&q=80";

    public static readonly string[] Categories = ["Кофе", "Чай", "Холодные напитки", "Другое"];

    public record ProductSeed(string Name, string Unit, int PackSize, long PackPriceCents);

    public record IngredientSeed(string Product, int Quantity);

    public record ToppingSeed(string Name, string ImageUrl, long PriceCents, bool IsAvailable, IngredientSeed[] Recipe);

    public record DrinkSeed(
        string Name, string Description, string ImageUrl, string Category, long PriceCents, bool IsAvailable,
        IngredientSeed[] Recipe, string[] Toppings);

    public record PurchaseSeed(string Product, int Packs);

    public record OrderLineSeed(string Drink, int Quantity, string[] Toppings);

    public record OrderSeed(string Customer, string? Comment, OrderStatus Status, OrderLineSeed[] Lines);

    private static IngredientSeed I(string product, int quantity) => new(product, quantity);

    public static readonly ProductSeed[] Products =
    [
        new("Кофе в зёрнах", "г", 1000, 2500),
        new("Молоко", "мл", 1000, 120),
        new("Овсяное молоко", "мл", 1000, 280),
        new("Сливки 33%", "мл", 500, 250),
        new("Сироп ванильный", "мл", 700, 600),
        new("Сироп карамельный", "мл", 700, 600),
        new("Шоколадный соус", "мл", 500, 550),
        new("Какао-порошок", "г", 500, 700),
        new("Чай чёрный", "г", 500, 900),
        new("Чай зелёный", "г", 500, 1000),
        new("Лимон", "шт", 10, 300),
        new("Лёд", "г", 2000, 100),
        new("Сахар", "г", 1000, 90),
        new("Корица молотая", "г", 100, 250),
        new("Тоник", "мл", 1000, 180),
        new("Питьевая вода", "мл", 5000, 100),
    ];

    public static readonly ToppingSeed[] Toppings =
    [
        new("Ванильный сироп", Photo("1752495079922-3d96fa527112"), 60, true, [I("Сироп ванильный", 15)]),
        new("Карамельный сироп", Photo("1568657704598-602700bd9694"), 60, true, [I("Сироп карамельный", 15)]),
        new("Шоколадный соус", Photo("1760445528687-3528e6e5ea73"), 70, true, [I("Шоколадный соус", 15)]),
        new("Взбитые сливки", Photo("1786695144564-d1cd05367032"), 80, true, [I("Сливки 33%", 30)]),
        new("Овсяное молоко", Photo("1517448931760-9bf4414148c5"), 50, true, [I("Овсяное молоко", 50)]),
        new("Корица", Photo("1622798337764-259682f03741"), 20, true, [I("Корица молотая", 1)]),
        new("Дополнительный шот", Photo("1772134141698-c47b78663c55"), 100, true, [I("Кофе в зёрнах", 9)]),
        new("Сахар", Photo("1709651808265-977ed7ef78c6"), 0, true, [I("Сахар", 5)]),
        // Not available in the catalog: shows the "unavailable" state.
        new("Маршмеллоу", Photo("1518646455738-c17d10184f5e"), 90, false, []),
    ];

    private static readonly string[] Sweet = ["Ванильный сироп", "Карамельный сироп", "Овсяное молоко", "Сахар"];

    private static readonly string[] Milky =
        [.. Sweet, "Шоколадный соус", "Корица", "Взбитые сливки", "Дополнительный шот"];

    public static readonly DrinkSeed[] Drinks =
    [
        new("Эспрессо", "Классический шот эспрессо", Photo("1579992357154-faf4bde95b3d"), "Кофе", 150, true,
            [I("Кофе в зёрнах", 18)], ["Дополнительный шот", "Сахар"]),
        new("Американо", "Эспрессо с горячей водой", Photo("1771736385651-c071580a9d78"), "Кофе", 180, true,
            [I("Кофе в зёрнах", 18), I("Питьевая вода", 150)],
            ["Ванильный сироп", "Карамельный сироп", "Дополнительный шот", "Сахар", "Корица"]),
        new("Капучино", "Эспрессо, молоко и плотная молочная пена", Photo("1506372023823-741c83b836fe"), "Кофе", 250, true,
            [I("Кофе в зёрнах", 18), I("Молоко", 150)], Milky),
        new("Латте", "Эспрессо с большим количеством нежного молока", Photo("1761783768352-e7f42bfa9988"), "Кофе", 280, true,
            [I("Кофе в зёрнах", 18), I("Молоко", 220)], Milky),
        new("Флэт уайт", "Двойной эспрессо с бархатистым молоком", Photo("1502462041640-b3d7e50d0662"), "Кофе", 290, true,
            [I("Кофе в зёрнах", 36), I("Молоко", 120)], Sweet),
        new("Мокка", "Эспрессо с шоколадом и молоком", Photo("1618576230663-9714aecfb99a"), "Кофе", 320, true,
            [I("Кофе в зёрнах", 18), I("Молоко", 180), I("Шоколадный соус", 20)],
            ["Взбитые сливки", "Корица", "Дополнительный шот", "Овсяное молоко", "Сахар"]),
        new("Раф", "Эспрессо со сливками и ванильным сиропом", Photo("1561882468-9110e03e0f78"), "Кофе", 330, true,
            [I("Кофе в зёрнах", 18), I("Сливки 33%", 100), I("Сироп ванильный", 15)],
            ["Корица", "Дополнительный шот", "Сахар"]),
        new("Тыквенный латте", "Сезонный напиток, пока недоступен", Photo("1724198217375-f298dc745f55"), "Кофе", 350, false,
            [I("Кофе в зёрнах", 18), I("Молоко", 200)], ["Корица"]),
        new("Чёрный чай", "Крепкий чёрный чай", Photo("1617543414010-212c4537604d"), "Чай", 150, true,
            [I("Чай чёрный", 5), I("Питьевая вода", 250)], ["Сахар", "Корица"]),
        new("Зелёный чай", "Лёгкий зелёный чай", Photo("1639241181542-b20bc1d0a1dc"), "Чай", 150, true,
            [I("Чай зелёный", 5), I("Питьевая вода", 250)], ["Сахар"]),
        new("Чай с лимоном", "Чёрный чай с долькой лимона", Photo("1637216789852-d2c6d5de6f68"), "Чай", 180, true,
            [I("Чай чёрный", 5), I("Лимон", 1), I("Питьевая вода", 250)], ["Сахар"]),
        new("Айс-латте", "Холодный латте со льдом", Photo("1461023058943-07fcbe16d735"), "Холодные напитки", 300, true,
            [I("Кофе в зёрнах", 18), I("Молоко", 200), I("Лёд", 100)], Sweet),
        new("Эспрессо-тоник", "Эспрессо и тоник со льдом", Photo("1753700281029-b3d15df20ec4"), "Холодные напитки", 310, true,
            [I("Кофе в зёрнах", 18), I("Тоник", 150), I("Лёд", 100)], []),
        new("Домашний лимонад", "Лимонад на свежем лимоне", Photo("1623084921164-4a8c5c37a912"), "Холодные напитки", 220, true,
            [I("Лимон", 1), I("Сахар", 20), I("Питьевая вода", 250), I("Лёд", 100)], []),
        new("Какао", "Горячее какао на молоке", Photo("1637572815755-c4b80092dce1"), "Другое", 260, true,
            [I("Какао-порошок", 20), I("Молоко", 200), I("Сахар", 10)], ["Взбитые сливки", "Маршмеллоу", "Корица"]),
        new("Горячий шоколад", "Густой горячий шоколад", Photo("1517578239113-b03992dcdd25"), "Другое", 290, true,
            [I("Шоколадный соус", 40), I("Молоко", 200)], ["Взбитые сливки", "Маршмеллоу", "Корица"]),
    ];

    // Bought in two rounds, so the purchase history has more than one page.
    public static readonly PurchaseSeed[][] PurchaseRounds =
    [
        [
            new("Кофе в зёрнах", 4), new("Молоко", 8), new("Овсяное молоко", 2), new("Сливки 33%", 2),
            new("Сироп ванильный", 1), new("Сироп карамельный", 1), new("Шоколадный соус", 2), new("Какао-порошок", 1),
            new("Чай чёрный", 1), new("Чай зелёный", 1), new("Лимон", 2), new("Лёд", 3), new("Сахар", 1),
            new("Корица молотая", 1), new("Тоник", 2), new("Питьевая вода", 2)
        ],
        [new("Кофе в зёрнах", 1), new("Молоко", 2)]
    ];

    // Oldest first. The name is the client's (2-10 characters, as the API requires), the comment is an optional note.
    public static readonly OrderSeed[] Orders =
    [
        new("Анна", "Без сахара", OrderStatus.Completed,
            [new("Латте", 1, ["Ванильный сироп"]), new("Эспрессо", 1, [])]),
        new("Игорь", "С собой", OrderStatus.Completed, [new("Капучино", 2, ["Корица", "Взбитые сливки"])]),
        new("Мария", null, OrderStatus.Completed, [new("Американо", 1, ["Сахар"])]),
        new("Дмитрий", "Побольше льда", OrderStatus.Completed, [new("Айс-латте", 2, ["Карамельный сироп"])]),
        new("Елена", "Без маршмеллоу", OrderStatus.Completed, [new("Какао", 1, ["Взбитые сливки"])]),
        new("Сергей", "Погорячее, пожалуйста", OrderStatus.Completed, [new("Чай с лимоном", 3, [])]),
        new("Ольга", "Заберёт в 18:30", OrderStatus.Completed, [new("Флэт уайт", 1, ["Овсяное молоко"])]),
        new("Макс", null, OrderStatus.Completed,
            [new("Эспрессо", 2, ["Дополнительный шот"]), new("Зелёный чай", 1, [])]),
        new("Ксения", "Аллергия на корицу", OrderStatus.Completed,
            [new("Капучино", 1, ["Взбитые сливки"]), new("Горячий шоколад", 1, ["Маршмеллоу"])]),
        new("Павел", "Передумал, отмена", OrderStatus.Cancelled, [new("Мокка", 1, ["Дополнительный шот"])]),
        new("Наталья", "Ушла, не дождалась", OrderStatus.Cancelled, [new("Эспрессо-тоник", 2, [])]),
        new("Вика", "Ошибка в заказе", OrderStatus.Cancelled, [new("Чёрный чай", 1, ["Сахар"])]),
        new("Алексей", "Для столика 4", OrderStatus.InProgress, [new("Раф", 1, [])]),
        new("Татьяна", "Одна порция без льда", OrderStatus.InProgress,
            [new("Домашний лимонад", 2, []), new("Горячий шоколад", 1, ["Взбитые сливки"])]),
        new("Андрей", "Подать вместе", OrderStatus.InProgress,
            [new("Латте", 3, ["Ванильный сироп", "Дополнительный шот"])]),
        new("Полина", null, OrderStatus.InProgress, [new("Флэт уайт", 2, ["Карамельный сироп"])]),
    ];
}
