import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '../lib/api'
import type { RecurringTransaction, RecurrenceFrequency, TransactionType } from '../types'

export interface RecurringInput {
  type: TransactionType
  amount: number
  currency: string
  categoryId: string
  description?: string
  frequency: RecurrenceFrequency
  startDate: string
  endDate?: string | null
}

const KEY = 'recurring'

export function useRecurring() {
  return useQuery({
    queryKey: [KEY],
    queryFn: async () => (await api.get<RecurringTransaction[]>('/recurring-transactions')).data,
  })
}

export function useCreateRecurring() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: async (body: RecurringInput) =>
      (await api.post<RecurringTransaction>('/recurring-transactions', body)).data,
    onSuccess: () => qc.invalidateQueries({ queryKey: [KEY] }),
  })
}

export function useUpdateRecurringStatus() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: async ({ id, isActive }: { id: string; isActive: boolean }) =>
      (await api.patch<RecurringTransaction>(`/recurring-transactions/${id}/status`, { isActive })).data,
    onSuccess: () => qc.invalidateQueries({ queryKey: [KEY] }),
  })
}

export function useDeleteRecurring() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: async (id: string) => {
      await api.delete(`/recurring-transactions/${id}`)
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: [KEY] }),
  })
}
