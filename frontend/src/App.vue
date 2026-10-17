<script setup lang="ts">
import { adminKey } from './api/client'
import AppLayout from './components/AppLayout.vue'
import LoginView from './views/LoginView.vue'
</script>

<template>
  <AppLayout v-if="adminKey" />
  <LoginView v-else />
</template>
