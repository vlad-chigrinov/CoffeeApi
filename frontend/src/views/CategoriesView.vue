<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import Button from 'primevue/button'
import Column from 'primevue/column'
import DataTable from 'primevue/datatable'
import Dialog from 'primevue/dialog'
import InputText from 'primevue/inputtext'
import { useConfirm } from 'primevue/useconfirm'
import { type Category, categoriesApi } from '../api/categories'
import { useNotify } from '../utils/notify'

const notify = useNotify()
const confirm = useConfirm()

const categories = ref<Category[]>([])
const loading = ref(false)

const formVisible = ref(false)
const editingId = ref<string | null>(null)
const saving = ref(false)
const form = reactive({ name: '' })

async function load() {
  loading.value = true
  try {
    categories.value = await categoriesApi.list()
  } catch (e) {
    notify.error(e)
  } finally {
    loading.value = false
  }
}

function openCreate() {
  editingId.value = null
  form.name = ''
  formVisible.value = true
}

function openEdit(category: Category) {
  editingId.value = category.id
  form.name = category.name
  formVisible.value = true
}

async function save() {
  saving.value = true
  try {
    if (editingId.value) await categoriesApi.update(editingId.value, { name: form.name })
    else await categoriesApi.create({ name: form.name })
    notify.success(editingId.value ? 'Категория обновлена' : 'Категория создана')
    formVisible.value = false
    await load()
  } catch (e) {
    notify.error(e)
  } finally {
    saving.value = false
  }
}

function confirmDelete(category: Category) {
  confirm.require({
    header: 'Удалить категорию',
    message: `Удалить «${category.name}»? Это возможно только если в ней нет напитков.`,
    icon: 'pi pi-exclamation-triangle',
    rejectProps: { label: 'Отмена', severity: 'secondary', outlined: true },
    acceptProps: { label: 'Удалить', severity: 'danger' },
    accept: async () => {
      try {
        await categoriesApi.remove(category.id)
        notify.success('Категория удалена')
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
      <h1>Категории</h1>
      <Button label="Создать" icon="pi pi-plus" @click="openCreate" />
    </div>

    <DataTable :value="categories" :loading="loading" data-key="id" striped-rows scrollable>
      <template #empty>Категорий пока нет</template>
      <Column field="name" header="Название" />
      <Column header="" style="width: 8rem">
        <template #body="{ data }">
          <div class="row-actions">
            <Button icon="pi pi-pencil" severity="secondary" text rounded aria-label="Изменить" @click="openEdit(data)" />
            <Button icon="pi pi-trash" severity="danger" text rounded aria-label="Удалить" @click="confirmDelete(data)" />
          </div>
        </template>
      </Column>
    </DataTable>

    <Dialog v-model:visible="formVisible" :header="editingId ? 'Изменить категорию' : 'Новая категория'" modal :style="{ width: '24rem' }">
      <form class="form" @submit.prevent="save">
        <label class="field">
          <span>Название</span>
          <InputText v-model="form.name" maxlength="100" required autofocus fluid />
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

.form-actions {
  display: flex;
  justify-content: flex-end;
  gap: 0.5rem;
}
</style>
