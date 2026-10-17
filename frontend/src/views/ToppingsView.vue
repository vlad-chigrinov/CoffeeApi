<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import Avatar from 'primevue/avatar'
import Button from 'primevue/button'
import Column from 'primevue/column'
import DataTable from 'primevue/datatable'
import Dialog from 'primevue/dialog'
import InputNumber from 'primevue/inputnumber'
import InputText from 'primevue/inputtext'
import Tag from 'primevue/tag'
import ToggleSwitch from 'primevue/toggleswitch'
import { useConfirm } from 'primevue/useconfirm'
import { type Ingredient, type Product, productsApi } from '../api/products'
import { type Topping, toppingsApi } from '../api/toppings'
import IngredientsEditor from '../components/IngredientsEditor.vue'
import { formatMoney, centsToUnits, unitsToCents } from '../utils/money'
import { useNotify } from '../utils/notify'

const notify = useNotify()
const confirm = useConfirm()

const toppings = ref<Topping[]>([])
const products = ref<Product[]>([])
const loading = ref(false)

const formVisible = ref(false)
const editingId = ref<string | null>(null)
const saving = ref(false)
const form = reactive({
  name: '',
  imageUrl: '',
  price: 0 as number | null,
  isAvailable: true,
  ingredients: [] as Ingredient[],
})

async function load() {
  loading.value = true
  try {
    ;[toppings.value, products.value] = await Promise.all([toppingsApi.list(), productsApi.list()])
  } catch (e) {
    notify.error(e)
  } finally {
    loading.value = false
  }
}

function openCreate() {
  editingId.value = null
  Object.assign(form, { name: '', imageUrl: '', price: 0, isAvailable: true, ingredients: [] })
  formVisible.value = true
}

function openEdit(topping: Topping) {
  editingId.value = topping.id
  Object.assign(form, {
    name: topping.name,
    imageUrl: topping.imageUrl ?? '',
    price: centsToUnits(topping.priceCents),
    isAvailable: topping.isAvailable,
    ingredients: topping.ingredients.map((i) => ({ ...i })),
  })
  formVisible.value = true
}

async function save() {
  saving.value = true
  const payload = {
    name: form.name,
    imageUrl: form.imageUrl.trim() || null,
    priceCents: unitsToCents(form.price),
    isAvailable: form.isAvailable,
    ingredients: form.ingredients,
  }
  try {
    if (editingId.value) await toppingsApi.update(editingId.value, payload)
    else await toppingsApi.create(payload)
    notify.success(editingId.value ? 'Топпинг обновлён' : 'Топпинг создан')
    formVisible.value = false
    await load()
  } catch (e) {
    notify.error(e)
  } finally {
    saving.value = false
  }
}

function confirmDelete(topping: Topping) {
  confirm.require({
    header: 'Удалить топпинг',
    message: `Удалить «${topping.name}»? Он также будет убран из всех напитков.`,
    icon: 'pi pi-exclamation-triangle',
    rejectProps: { label: 'Отмена', severity: 'secondary', outlined: true },
    acceptProps: { label: 'Удалить', severity: 'danger' },
    accept: async () => {
      try {
        await toppingsApi.remove(topping.id)
        notify.success('Топпинг удалён')
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
      <h1>Топпинги</h1>
      <Button label="Создать" icon="pi pi-plus" @click="openCreate" />
    </div>

    <DataTable :value="toppings" :loading="loading" data-key="id" striped-rows scrollable>
      <template #empty>Топпингов пока нет</template>
      <Column header="" style="width: 4rem">
        <template #body="{ data }">
          <Avatar :image="data.imageUrl ?? undefined" :label="data.imageUrl ? undefined : data.name[0]" shape="circle" />
        </template>
      </Column>
      <Column field="name" header="Название" />
      <Column header="Цена">
        <template #body="{ data }">{{ formatMoney(data.priceCents) }}</template>
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

    <Dialog v-model:visible="formVisible" :header="editingId ? 'Изменить топпинг' : 'Новый топпинг'" modal :style="{ width: '26rem' }">
      <form class="form" @submit.prevent="save">
        <label class="field">
          <span>Название</span>
          <InputText v-model="form.name" maxlength="200" required autofocus fluid />
        </label>
        <label class="field">
          <span>Цена</span>
          <InputNumber v-model="form.price" :min="0" :min-fraction-digits="2" :max-fraction-digits="2" fluid />
        </label>
        <label class="field">
          <span>URL картинки</span>
          <InputText v-model="form.imageUrl" maxlength="500" placeholder="https://…" fluid />
        </label>
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
