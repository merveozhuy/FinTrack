import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '../lib/api'
import type { Budget } from '../types'

export interface BudgetInput {
  categoryId: string
  year: number
  month: number
  monthlyLimit: number
}

const KEY = 'budgets'

export function useBudgets(year: number, month: number) {
  return useQuery({
    queryKey: [KEY, year, month],
    queryFn: async () => (await api.get<Budget[]>(`/budgets/${year}/${month}`)).data,
  })
}

export function useCreateBudget() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: async (body: BudgetInput) => (await api.post<Budget>('/budgets', body)).data,
    onSuccess: () => qc.invalidateQueries({ queryKey: [KEY] }),
  })
}

export function useDeleteBudget() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: async (id: string) => {
      await api.delete(`/budgets/${id}`)
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: [KEY] }),
  })
}
