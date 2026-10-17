import { useToast } from 'primevue/usetoast'

export function useNotify() {
  const toast = useToast()

  return {
    error(e: unknown) {
      toast.add({ severity: 'error', summary: 'Ошибка', detail: e instanceof Error ? e.message : String(e), life: 5000 })
    },
    success(summary: string) {
      toast.add({ severity: 'success', summary, life: 3000 })
    },
    info(summary: string) {
      toast.add({ severity: 'info', summary, life: 2000 })
    },
  }
}
