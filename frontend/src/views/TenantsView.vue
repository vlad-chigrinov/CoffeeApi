<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import Button from 'primevue/button'
import Column from 'primevue/column'
import DataTable from 'primevue/datatable'
import Dialog from 'primevue/dialog'
import InputNumber from 'primevue/inputnumber'
import InputText from 'primevue/inputtext'
import Tag from 'primevue/tag'
import ToggleSwitch from 'primevue/toggleswitch'
import { useConfirm } from 'primevue/useconfirm'
import { type Tenant, tenantsApi } from '../api/tenants'
import { formatMoney, unitsToCents } from '../utils/money'
import { useNotify } from '../utils/notify'

const notify = useNotify()
const confirm = useConfirm()

const tenants = ref<Tenant[]>([])
const loading = ref(false)

const formVisible = ref(false)
const editingId = ref<string | null>(null)
const saving = ref(false)
const form = reactive({ name: '', isActive: true })

const issuedKey = ref<{ tenantName: string; key: string } | null>(null)
const keyDialogVisible = ref(false)

async function load() {
  loading.value = true
  try {
    tenants.value = await tenantsApi.list()
  } catch (e) {
    notify.error(e)
  } finally {
    loading.value = false
  }
}

function openCreate() {
  editingId.value = null
  Object.assign(form, { name: '', isActive: true })
  formVisible.value = true
}

function openEdit(tenant: Tenant) {
  editingId.value = tenant.id
  Object.assign(form, { name: tenant.name, isActive: tenant.isActive })
  formVisible.value = true
}

const topUpTarget = ref<Tenant | null>(null)
const topUpVisible = ref(false)
const topUpAmount = ref<number | null>(null)
const topUpInput = ref<{ $el: HTMLElement } | null>(null)
const toppingUp = ref(false)

// InputNumber's `autofocus` does not reach the inner <input>, so focus it once the dialog is shown.
function focusTopUp() {
  topUpInput.value?.$el.querySelector('input')?.focus()
}

function openTopUp(tenant: Tenant) {
  topUpTarget.value = tenant
  topUpAmount.value = null
  topUpVisible.value = true
}

async function topUp() {
  if (!topUpTarget.value || !topUpAmount.value) return
  toppingUp.value = true
  try {
    await tenantsApi.topUpBudget(topUpTarget.value.id, unitsToCents(topUpAmount.value))
    notify.success('Бюджет пополнен')
    topUpVisible.value = false
    await load()
  } catch (e) {
    notify.error(e)
  } finally {
    toppingUp.value = false
  }
}

function showKey(tenantName: string, key: string) {
  issuedKey.value = { tenantName, key }
  keyDialogVisible.value = true
}

async function save() {
  saving.value = true
  try {
    if (editingId.value) {
      await tenantsApi.update(editingId.value, { name: form.name, isActive: form.isActive })
      notify.success('Тенант обновлён')
    } else {
      const created = await tenantsApi.create({ name: form.name })
      showKey(created.tenant.name, created.apiKey)
    }
    formVisible.value = false
    await load()
  } catch (e) {
    notify.error(e)
  } finally {
    saving.value = false
  }
}

function confirmRegenerate(tenant: Tenant) {
  confirm.require({
    header: 'Перегенерировать ключ',
    message: `Старый ключ тенанта «${tenant.name}» перестанет работать. Продолжить?`,
    icon: 'pi pi-key',
    rejectProps: { label: 'Отмена', severity: 'secondary', outlined: true },
    acceptProps: { label: 'Перегенерировать', severity: 'warn' },
    accept: async () => {
      try {
        const result = await tenantsApi.regenerateKey(tenant.id)
        showKey(result.tenant.name, result.apiKey)
        await load()
      } catch (e) {
        notify.error(e)
      }
    },
  })
}

function confirmDelete(tenant: Tenant) {
  confirm.require({
    header: 'Удалить тенанта',
    message: `Удалить «${tenant.name}»? Действие необратимо.`,
    icon: 'pi pi-exclamation-triangle',
    rejectProps: { label: 'Отмена', severity: 'secondary', outlined: true },
    acceptProps: { label: 'Удалить', severity: 'danger' },
    accept: async () => {
      try {
        await tenantsApi.remove(tenant.id)
        notify.success('Тенант удалён')
        await load()
      } catch (e) {
        notify.error(e)
      }
    },
  })
}

async function copyKey() {
  if (!issuedKey.value) return
  try {
    await navigator.clipboard.writeText(issuedKey.value.key)
    notify.info('Ключ скопирован')
  } catch {
    notify.error(new Error('Не удалось скопировать, выделите ключ вручную'))
  }
}

onMounted(load)
</script>

<template>
  <section>
    <div class="section-header">
      <h1>Тенанты</h1>
      <Button label="Создать" icon="pi pi-plus" @click="openCreate" />
    </div>

    <DataTable :value="tenants" :loading="loading" data-key="id" striped-rows scrollable>
      <template #empty>Тенантов пока нет</template>
      <Column field="name" header="Название" />
      <Column field="apiKeyPrefix" header="Ключ">
        <template #body="{ data }"><code>{{ data.apiKeyPrefix }}…</code></template>
      </Column>
      <Column header="Бюджет">
        <template #body="{ data }">{{ formatMoney(data.budgetCents) }}</template>
      </Column>
      <Column header="Статус">
        <template #body="{ data }">
          <Tag :value="data.isActive ? 'Активен' : 'Отключён'" :severity="data.isActive ? 'success' : 'secondary'" />
        </template>
      </Column>
      <Column header="Создан">
        <template #body="{ data }">{{ new Date(data.createdAt).toLocaleString() }}</template>
      </Column>
      <Column header="" style="width: 14rem">
        <template #body="{ data }">
          <div class="row-actions">
            <Button icon="pi pi-wallet" severity="success" text rounded aria-label="Пополнить бюджет" @click="openTopUp(data)" />
            <Button icon="pi pi-pencil" severity="secondary" text rounded aria-label="Изменить" @click="openEdit(data)" />
            <Button icon="pi pi-key" severity="warn" text rounded aria-label="Перегенерировать ключ" @click="confirmRegenerate(data)" />
            <Button icon="pi pi-trash" severity="danger" text rounded aria-label="Удалить" @click="confirmDelete(data)" />
          </div>
        </template>
      </Column>
    </DataTable>

    <Dialog v-model:visible="formVisible" :header="editingId ? 'Изменить тенанта' : 'Новый тенант'" modal :style="{ width: '26rem' }">
      <form class="form" @submit.prevent="save">
        <label class="field">
          <span>Название</span>
          <InputText v-model="form.name" maxlength="200" required autofocus fluid />
        </label>
        <label v-if="editingId" class="field inline">
          <ToggleSwitch v-model="form.isActive" />
          <span>Активен</span>
        </label>
        <div class="form-actions">
          <Button type="button" label="Отмена" severity="secondary" outlined @click="formVisible = false" />
          <Button type="submit" label="Сохранить" :loading="saving" :disabled="!form.name.trim()" />
        </div>
      </form>
    </Dialog>

    <Dialog v-model:visible="topUpVisible" header="Пополнить бюджет" modal :style="{ width: '24rem' }" @show="focusTopUp">
      <form class="form" @submit.prevent="topUp">
        <p class="topup-info">
          Тенант «{{ topUpTarget?.name }}», сейчас в бюджете {{ topUpTarget ? formatMoney(topUpTarget.budgetCents) : '' }}.
        </p>
        <label class="field">
          <span>Сумма пополнения</span>
          <InputNumber ref="topUpInput" v-model="topUpAmount" :min="0.01" :min-fraction-digits="2" :max-fraction-digits="2" fluid />
        </label>
        <div class="form-actions">
          <Button type="button" label="Отмена" severity="secondary" outlined @click="topUpVisible = false" />
          <Button type="submit" label="Пополнить" :loading="toppingUp" :disabled="!topUpAmount" />
        </div>
      </form>
    </Dialog>

    <Dialog v-model:visible="keyDialogVisible" header="API-ключ тенанта" modal :style="{ width: '32rem' }">
      <p>
        Ключ тенанта «{{ issuedKey?.tenantName }}». <strong>Он показывается только сейчас</strong> — сохраните его,
        восстановить ключ нельзя, только перегенерировать.
      </p>
      <div class="key-row">
        <InputText :model-value="issuedKey?.key" readonly fluid />
        <Button icon="pi pi-copy" aria-label="Копировать" @click="copyKey" />
      </div>
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

.topup-info {
  margin: 0;
}

.key-row {
  display: flex;
  gap: 0.5rem;
}
</style>
