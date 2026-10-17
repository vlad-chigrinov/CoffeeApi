<script setup lang="ts">
import { ref } from 'vue'
import Button from 'primevue/button'
import Card from 'primevue/card'
import Message from 'primevue/message'
import Password from 'primevue/password'
import { setAdminKey } from '../api/client'
import { tenantsApi } from '../api/tenants'

const key = ref('')
const loading = ref(false)
const error = ref<string | null>(null)

async function login() {
  if (!key.value) return
  loading.value = true
  error.value = null
  try {
    // The key is checked by calling a protected endpoint with it.
    await tenantsApi.list(key.value)
    setAdminKey(key.value)
  } catch (e) {
    error.value = e instanceof Error ? e.message : 'Не удалось войти'
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <div class="login">
    <Card class="login-card">
      <template #title>Coffee Admin</template>
      <template #subtitle>Введите admin API-ключ</template>
      <template #content>
        <form class="login-form" @submit.prevent="login">
          <Password v-model="key" placeholder="Admin API key" :feedback="false" toggle-mask fluid autofocus />
          <Message v-if="error" severity="error" size="small" variant="simple">{{ error }}</Message>
          <Button type="submit" label="Войти" icon="pi pi-sign-in" :loading="loading" :disabled="!key" />
        </form>
      </template>
    </Card>
  </div>
</template>

<style scoped>
.login {
  min-height: 100vh;
  display: grid;
  place-items: center;
  padding: 1rem;
}

.login-card {
  width: 100%;
  max-width: 24rem;
}

.login-form {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}
</style>
