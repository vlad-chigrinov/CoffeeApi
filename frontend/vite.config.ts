import vue from '@vitejs/plugin-vue'
import { defineConfig } from 'vite'

// https://vite.dev/config/
export default defineConfig({
  plugins: [vue()],
  // Read VITE_* values from the repo-root .env, the same file docker compose uses.
  envDir: '..',
  server: {
    // Forward API calls to the ASP.NET Core backend (http profile), so no CORS setup is needed in dev.
    proxy: {
      '/api': 'http://localhost:5264',
    },
  },
})
