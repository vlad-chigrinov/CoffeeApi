import { createRouter, createWebHistory } from 'vue-router'

export const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: '/', redirect: '/tenants' },
    { path: '/tenants', component: () => import('./views/TenantsView.vue') },
    { path: '/categories', component: () => import('./views/CategoriesView.vue') },
    { path: '/drinks', component: () => import('./views/DrinksView.vue') },
    { path: '/toppings', component: () => import('./views/ToppingsView.vue') },
    { path: '/products', component: () => import('./views/ProductsView.vue') },
    { path: '/:pathMatch(.*)*', redirect: '/tenants' },
  ],
})
