import { request } from './client'

export interface Category {
  id: string
  name: string
}

const base = '/api/admin/categories'

export const categoriesApi = {
  list: () => request<Category[]>('GET', base),
  create: (data: { name: string }) => request<Category>('POST', base, data),
  update: (id: string, data: { name: string }) => request<Category>('PUT', `${base}/${id}`, data),
  remove: (id: string) => request<void>('DELETE', `${base}/${id}`),
}
