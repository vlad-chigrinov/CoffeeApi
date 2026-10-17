import { request } from './client'

export interface Tenant {
  id: string
  name: string
  apiKeyPrefix: string
  budgetCents: number
  isActive: boolean
  createdAt: string
}

export interface TenantWithKey {
  tenant: Tenant
  apiKey: string
}

const base = '/api/admin/tenants'

export const tenantsApi = {
  list: (key?: string) => request<Tenant[]>('GET', base, undefined, key),
  create: (data: { name: string }) => request<TenantWithKey>('POST', base, data),
  update: (id: string, data: { name: string; isActive: boolean }) =>
    request<Tenant>('PUT', `${base}/${id}`, data),
  topUpBudget: (id: string, amountCents: number) =>
    request<Tenant>('POST', `${base}/${id}/budget/top-up`, { amountCents }),
  regenerateKey: (id: string) => request<TenantWithKey>('POST', `${base}/${id}/regenerate-key`),
  remove: (id: string) => request<void>('DELETE', `${base}/${id}`),
}
