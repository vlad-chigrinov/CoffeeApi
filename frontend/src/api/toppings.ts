import { request } from './client'
import type { Ingredient } from './products'

export interface Topping {
  id: string
  name: string
  imageUrl: string | null
  priceCents: number
  isAvailable: boolean
  ingredients: Ingredient[]
}

export interface ToppingInput {
  name: string
  imageUrl: string | null
  priceCents: number
  isAvailable: boolean
  ingredients: Ingredient[]
}

const base = '/api/admin/toppings'

export const toppingsApi = {
  list: () => request<Topping[]>('GET', base),
  create: (data: ToppingInput) => request<Topping>('POST', base, data),
  update: (id: string, data: ToppingInput) => request<Topping>('PUT', `${base}/${id}`, data),
  remove: (id: string) => request<void>('DELETE', `${base}/${id}`),
}
