import { ref } from 'vue'

const STORAGE_KEY = 'coffee.adminKey'

function readStoredKey(): string | null {
  try {
    return sessionStorage.getItem(STORAGE_KEY)
  } catch {
    return null
  }
}

/** Admin API key, kept in sessionStorage so it is dropped when the tab is closed. */
export const adminKey = ref<string | null>(readStoredKey())

export function setAdminKey(key: string | null) {
  adminKey.value = key
  try {
    if (key) sessionStorage.setItem(STORAGE_KEY, key)
    else sessionStorage.removeItem(STORAGE_KEY)
  } catch {
    // storage unavailable: the key just lives in memory
  }
}

export class ApiError extends Error {
  status: number

  constructor(status: number, message: string) {
    super(message)
    this.status = status
  }
}

async function errorMessage(response: Response): Promise<string> {
  try {
    const problem = await response.json()
    if (problem?.errors) return Object.values(problem.errors as Record<string, string[]>).flat().join(' ')
    if (problem?.title) return problem.title
  } catch {
    // body is not JSON
  }
  return `Ошибка ${response.status}`
}

export async function request<T>(method: string, path: string, body?: unknown, key = adminKey.value): Promise<T> {
  const response = await fetch(path, {
    method,
    headers: {
      ...(body !== undefined && { 'Content-Type': 'application/json' }),
      ...(key && { 'X-Admin-Key': key }),
    },
    body: body !== undefined ? JSON.stringify(body) : undefined,
  })

  if (response.status === 401) {
    setAdminKey(null)
    throw new ApiError(401, 'Неверный или отсутствующий admin-ключ')
  }
  if (!response.ok) throw new ApiError(response.status, await errorMessage(response))

  return response.status === 204 ? (undefined as T) : ((await response.json()) as T)
}
