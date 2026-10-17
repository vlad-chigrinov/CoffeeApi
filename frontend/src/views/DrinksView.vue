<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import Avatar from 'primevue/avatar'
import Button from 'primevue/button'
import Column from 'primevue/column'
import DataTable from 'primevue/datatable'
import Dialog from 'primevue/dialog'
import InputNumber from 'primevue/inputnumber'
import InputText from 'primevue/inputtext'
import MultiSelect from 'primevue/multiselect'
import Select from 'primevue/select'
import Tag from 'primevue/tag'
import Textarea from 'primevue/textarea'
import ToggleSwitch from 'primevue/toggleswitch'
import { useConfirm } from 'primevue/useconfirm'
import { type Category, categoriesApi } from '../api/categories'
import { type Drink, drinksApi } from '../api/drinks'
import { type Ingredient, type Product, productsApi } from '../api/products'
import IngredientsEditor from '../components/IngredientsEditor.vue'
import { type Topping, toppingsApi } from '../api/toppings'
import { centsToUnits, formatMoney, unitsToCents } from '../utils/money'
import { useNotify } from '../utils/notify'

const notify = useNotify()
const confirm = useConfirm()

const drinks = ref<Drink[]>([])
const toppings = ref<Topping[]>([])
const categories = ref<Category[]>([])
const products = ref<Product[]>([])
const loading = ref(false)

const toppingNames = computed(() => new Map(toppings.value.map((t) => [t.id, t.name])))
const categoryNames = computed(() => new Map(categories.value.map((c) => [c.id, c.name])))

const formVisible = ref(false)
const editingId = ref<string | null>(null)
const saving = ref(false)
const form = reactive({
  name: '',
  description: '',
  imageUrl: '',
  categoryId: null as string | null,
  price: 0 as number | null,
  isAvailable: true,
  toppingIds: [] as string[],
  ingredients: [] as Ingredient[],
})

async function load() {
  loading.value = true
  try {
    ;[drinks.value, toppings.value, categories.value, products.value] = await Promise.all([
      drinksApi.list(),
      toppingsApi.list(),
      categoriesApi.list(),
      productsApi.list(),
    ])
  } catch (e) {
    notify.error(e)
  } finally {
    loading.value = false
  }
}

function openCreate() {
  editingId.value = null
  Object.assign(form, {
    name: '',
    description: '',
    imageUrl: '',
    categoryId: null,
    price: 0,
    isAvailable: true,
    toppingIds: [],
    ingredients: [],
  })
  formVisible.value = true
}

function openEdit(drink: Drink) {
  editingId.value = drink.id
  Object.assign(form, {
    name: drink.name,
    description: drink.description ?? '',
    imageUrl: drink.imageUrl ?? '',
    categoryId: drink.categoryId,
    price: centsToUnits(drink.priceCents),
    isAvailable: drink.isAvailable,
    toppingIds: [...drink.toppingIds],
    ingredients: drink.ingredients.map((i) => ({ ...i })),
  })
  formVisible.value = true
}

async function save() {
  saving.value = true
  const payload = {
    name: form.name,
    description: form.description.trim() || null,
    imageUrl: form.imageUrl.trim() || null,
    categoryId: form.categoryId,
    priceCents: unitsToCents(form.price),
    isAvailable: form.isAvailable,
    toppingIds: form.toppingIds,
    ingredients: form.ingredients,
  }
  try {
    if (editingId.value) await drinksApi.update(editingId.value, payload)
    else await drinksApi.create(payload)
    notify.success(editingId.value ? 'Напиток обновлён' : 'Напиток создан')
    formVisible.value = false
    await load()
  } catch (e) {
    notify.error(e)
  } finally {
    saving.value = false
  }
}

function confirmDelete(drink: Drink) {
  confirm.require({
    header: 'Удалить напиток',
    message: `Удалить «${drink.name}»? Действие необратимо.`,
    icon: 'pi pi-exclamation-triangle',
    rejectProps: { label: 'Отмена', severity: 'secondary', outlined: true },
    acceptProps: { label: 'Удалить', severity: 'danger' },
    accept: async () => {
      try {
        await drinksApi.remove(drink.id)
        notify.success('Напиток удалён')
        await load()
      } catch (e) {
        notify.error(e)
      }
    },
  })
}

onMounted(load)
</script>

<template>
  <section>
    <div class="section-header">
      <h1>Напитки</h1>
      <Button label="Создать" icon="pi pi-plus" @click="openCreate" />
    </div>

    <DataTable :value="drinks" :loading="loading" data-key="id" striped-rows scrollable>
      <template #empty>Напитков пока нет</template>
      <Column header="" style="width: 4rem">
        <template #body="{ data }">
          <Avatar :image="data.imageUrl ?? undefined" :label="data.imageUrl ? undefined : data.name[0]" shape="circle" />
        </template>
      </Column>
      <Column field="name" header="Название" />
      <Column header="Категория">
        <template #body="{ data }">
          <span v-if="data.categoryId">{{ categoryNames.get(data.categoryId) ?? '?' }}</span>
          <span v-else class="muted">—</span>
        </template>
      </Column>
      <Column header="Цена">
        <template #body="{ data }">{{ formatMoney(data.priceCents) }}</template>
      </Column>
      <Column header="Топпинги">
        <template #body="{ data }">
          <div class="chips">
            <Tag v-for="id in data.toppingIds" :key="id" :value="toppingNames.get(id) ?? '?'" severity="secondary" />
            <span v-if="!data.toppingIds.length" class="muted">—</span>
          </div>
        </template>
      </Column>
      <Column header="Статус">
        <template #body="{ data }">
          <Tag :value="data.isAvailable ? 'Доступен' : 'Недоступен'" :severity="data.isAvailable ? 'success' : 'secondary'" />
        </template>
      </Column>
      <Column header="" style="width: 8rem">
        <template #body="{ data }">
          <div class="row-actions">
            <Button icon="pi pi-pencil" severity="secondary" text rounded aria-label="Изменить" @click="openEdit(data)" />
            <Button icon="pi pi-trash" severity="danger" text rounded aria-label="Удалить" @click="confirmDelete(data)" />
          </div>
        </template>
      </Column>
    </DataTable>

    <Dialog v-model:visible="formVisible" :header="editingId ? 'Изменить напиток' : 'Новый напиток'" modal :style="{ width: '30rem' }">
      <form class="form" @submit.prevent="save">
        <label class="field">
          <span>Название</span>
          <InputText v-model="form.name" maxlength="200" required autofocus fluid />
        </label>
        <label class="field">
          <span>Описание</span>
          <Textarea v-model="form.description" maxlength="1000" rows="3" auto-resize fluid />
        </label>
        <div class="field">
          <span>Категория</span>
          <Select
            v-model="form.categoryId"
            :options="categories"
            option-label="name"
            option-value="id"
            placeholder="Без категории"
            show-clear
            fluid
          />
        </div>
        <label class="field">
          <span>Цена</span>
          <InputNumber v-model="form.price" :min="0" :min-fraction-digits="2" :max-fraction-digits="2" fluid />
        </label>
        <label class="field">
          <span>URL картинки</span>
          <InputText v-model="form.imageUrl" maxlength="500" placeholder="https://…" fluid />
        </label>
        <div class="field">
          <span>Допустимые топпинги</span>
          <MultiSelect
            v-model="form.toppingIds"
            :options="toppings"
            option-label="name"
            option-value="id"
            placeholder="Не выбраны"
            display="chip"
            filter
            fluid
          />
        </div>
        <div class="field">
          <span>Рецепт: расход на одну порцию</span>
          <IngredientsEditor v-model="form.ingredients" :products="products" />
        </div>
        <label class="field inline">
          <ToggleSwitch v-model="form.isAvailable" />
          <span>Доступен для заказа</span>
        </label>
        <div class="form-actions">
          <Button type="button" label="Отмена" severity="secondary" outlined @click="formVisible = false" />
          <Button type="submit" label="Сохранить" :loading="saving" :disabled="!form.name.trim()" />
        </div>
      </form>
    </Dialog>
  </section>
</template>

<style scoped>
.section-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 1rem;
  margin-bottom: 1rem;
}

.section-header h1 {
  margin: 0;
  font-size: 1.5rem;
}

.row-actions {
  display: flex;
  gap: 0.5rem;
}

.chips {
  display: flex;
  flex-wrap: wrap;
  gap: 0.25rem;
}

.muted {
  color: var(--p-text-muted-color);
}

.form {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.field {
  display: flex;
  flex-direction: column;
  gap: 0.375rem;
}

.field.inline {
  flex-direction: row;
  align-items: center;
  gap: 0.75rem;
}

.form-actions {
  display: flex;
  justify-content: flex-end;
  gap: 0.5rem;
}
</style>
