import { createApp } from 'vue'
import PrimeVue from 'primevue/config'
import ConfirmationService from 'primevue/confirmationservice'
import ToastService from 'primevue/toastservice'
import Aura from '@primeuix/themes/aura'
import 'primeicons/primeicons.css'
import './style.css'
import App from './App.vue'
import { router } from './router'

createApp(App)
  .use(router)
  .use(PrimeVue, {
    // Container: /config.js is written on start from PRIMEUI_LICENSE. Dev: VITE_PRIMEUI_LICENSE from the root .env.
    license: window.__CONFIG__?.primeuiLicense || import.meta.env.VITE_PRIMEUI_LICENSE,
    theme: {
      preset: Aura,
      options: { darkModeSelector: '.app-dark' },
    },
  })
  .use(ToastService)
  .use(ConfirmationService)
  .mount('#app')
