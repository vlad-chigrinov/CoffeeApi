import { request } from './client'

export interface Product {
  id: string
  name: string
  unit: string
  packSize: number
  packPriceCents: number
}

export interface ProductInput {
  name: string
  unit: string
  packSize: number
  packPriceCents: number
}

/** One recipe line: a product and how much of it (in the product's base units) one serving uses. */
export interface Ingredient {
  productId: string
  quantity: number
}

const base = '/api/admin/products'

export const productsApi = {
  list: () => request<Product[]>('GET', base),
  create: (data: ProductInput) => request<Product>('POST', base, data),
  update: (id: string, data: ProductInput) => request<Product>('PUT', `${base}/${id}`, data),
  remove: (id: string) => request<void>('DELETE', `${base}/${id}`),
}
