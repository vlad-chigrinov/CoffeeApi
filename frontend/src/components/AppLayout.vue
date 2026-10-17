<script setup lang="ts">
import { useRoute, useRouter } from 'vue-router'
import Button from 'primevue/button'
import ConfirmDialog from 'primevue/confirmdialog'
import Tab from 'primevue/tab'
import TabList from 'primevue/tablist'
import Tabs from 'primevue/tabs'
import Toast from 'primevue/toast'
import { setAdminKey } from '../api/client'

const route = useRoute()
const router = useRouter()

const tabs = [
  { path: '/tenants', label: 'Тенанты', icon: 'pi pi-users' },
  { path: '/categories', label: 'Категории', icon: 'pi pi-folder' },
  { path: '/drinks', label: 'Напитки', icon: 'pi pi-shopping-bag' },
  { path: '/toppings', label: 'Топпинги', icon: 'pi pi-tags' },
  { path: '/products', label: 'Продукты', icon: 'pi pi-box' },
]
</script>

<template>
  <div class="layout">
    <Toast />
    <ConfirmDialog />

    <header class="layout-header">
      <div class="brand"><i class="pi pi-coffee" /> Coffee Admin</div>
      <Button label="Выйти" icon="pi pi-sign-out" severity="secondary" outlined @click="setAdminKey(null)" />
    </header>

    <Tabs :value="route.path" @update:value="router.push(String($event))">
      <TabList>
        <Tab v-for="tab in tabs" :key="tab.path" :value="tab.path">
          <span class="tab-label"><i :class="tab.icon" /> {{ tab.label }}</span>
        </Tab>
      </TabList>
    </Tabs>

    <main class="layout-content">
      <RouterView />
    </main>
  </div>
</template>

<style scoped>
.layout {
  max-width: 72rem;
  margin: 0 auto;
  padding: 1rem;
}

.layout-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 1rem;
  margin-bottom: 1rem;
}

.brand {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  font-size: 1.25rem;
  font-weight: 600;
}

.tab-label {
  display: inline-flex;
  align-items: center;
  gap: 0.5rem;
}

.layout-content {
  padding-top: 1rem;
}
</style>
