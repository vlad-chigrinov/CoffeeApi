<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import Button from 'primevue/button'
import Column from 'primevue/column'
import DataTable from 'primevue/datatable'
import Dialog from 'primevue/dialog'
import InputNumber from 'primevue/inputnumber'
import InputText from 'primevue/inputtext'
import { useConfirm } from 'primevue/useconfirm'
import { type Product, productsApi } from '../api/products'
import { centsToUnits, formatMoney, unitsToCents } from '../utils/money'
import { useNotify } from '../utils/notify'

const notify = useNotify()
const confirm = useConfirm()

const products = ref<Product[]>([])
const loading = ref(false)

const formVisible = ref(false)
const editingId = ref<string | null>(null)
const saving = ref(false)
const form = reactive({ name: '', unit: '', packSize: 1 as number | null, packPrice: 0 as number | null })

async function load() {
  loading.value = true
  try {
    products.value = await productsApi.list()
  } catch (e) {
    notify.error(e)
  } finally {
    loading.value = false
  }
}

function openCreate() {
  editingId.value = null
  Object.assign(form, { name: '', unit: '', packSize: 1, packPrice: 0 })
  formVisible.value = true
}

function openEdit(product: Product) {
  editingId.value = product.id
  Object.assign(form, {
    name: product.name,
    unit: product.unit,
    packSize: product.packSize,
    packPrice: centsToUnits(product.packPriceCents),
  })
  formVisible.value = true
}

async function save() {
  saving.value = true
  const payload = {
    name: form.name,
    unit: form.unit,
    packSize: form.packSize ?? 1,
    packPriceCents: unitsToCents(form.packPrice),
  }
  try {
    if (editingId.value) await productsApi.update(editingId.value, payload)
    else await productsApi.create(payload)
    notify.success(editingId.value ? 'Продукт обновлён' : 'Продукт создан')
    formVisible.value = false
    await load()
  } catch (e) {
    notify.error(e)
  } finally {
    saving.value = false
  }
}

function confirmDelete(product: Product) {
  confirm.require({
    header: 'Удалить продукт',
    message: `Удалить «${product.name}»? Это возможно, только если он не используется в рецептах и на складах.`,
    icon: 'pi pi-exclamation-triangle',
    rejectProps: { label: 'Отмена', severity: 'secondary', outlined: true },
    acceptProps: { label: 'Удалить', severity: 'danger' },
    accept: async () => {
      try {
        await productsApi.remove(product.id)
        notify.success('Продукт удалён')
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
      <h1>Продукты</h1>
      <Button label="Создать" icon="pi pi-plus" @click="openCreate" />
    </div>

    <DataTable :value="products" :loading="loading" data-key="id" striped-rows scrollable>
      <template #empty>Продуктов пока нет</template>
      <Column field="name" header="Название" />
      <Column header="Упаковка">
        <template #body="{ data }">{{ data.packSize }} {{ data.unit }}</template>
      </Column>
      <Column header="Цена упаковки">
        <template #body="{ data }">{{ formatMoney(data.packPriceCents) }}</template>
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

    <Dialog v-model:visible="formVisible" :header="editingId ? 'Изменить продукт' : 'Новый продукт'" modal :style="{ width: '26rem' }">
      <form class="form" @submit.prevent="save">
        <label class="field">
          <span>Название</span>
          <InputText v-model="form.name" maxlength="200" required autofocus fluid />
        </label>
        <label class="field">
          <span>Единица измерения (г, мл, шт)</span>
          <InputText v-model="form.unit" maxlength="20" required fluid />
        </label>
        <label class="field">
          <span>Размер упаковки (в этих единицах)</span>
          <InputNumber v-model="form.packSize" :min="1" :max="1000000" :use-grouping="false" fluid />
        </label>
        <label class="field">
          <span>Цена упаковки</span>
          <InputNumber v-model="form.packPrice" :min="0" :min-fraction-digits="2" :max-fraction-digits="2" fluid />
        </label>
        <div class="form-actions">
          <Button type="button" label="Отмена" severity="secondary" outlined @click="formVisible = false" />
          <Button type="submit" label="Сохранить" :loading="saving" :disabled="!form.name.trim() || !form.unit.trim()" />
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

.form-actions {
  display: flex;
  justify-content: flex-end;
  gap: 0.5rem;
}
</style>
