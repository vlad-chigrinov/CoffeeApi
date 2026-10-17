import { request } from './client'
import type { Ingredient } from './products'

export interface Drink {
  id: string
  name: string
  description: string | null
  imageUrl: string | null
  categoryId: string | null
  priceCents: number
  isAvailable: boolean
  createdAt: string
  toppingIds: string[]
  ingredients: Ingredient[]
}

export interface DrinkInput {
  name: string
  description: string | null
  imageUrl: string | null
  categoryId: string | null
  priceCents: number
  isAvailable: boolean
  toppingIds: string[]
  ingredients: Ingredient[]
}

const base = '/api/admin/drinks'

export const drinksApi = {
  list: () => request<Drink[]>('GET', base),
  create: (data: DrinkInput) => request<Drink>('POST', base, data),
  update: (id: string, data: DrinkInput) => request<Drink>('PUT', `${base}/${id}`, data),
  remove: (id: string) => request<void>('DELETE', `${base}/${id}`),
}
