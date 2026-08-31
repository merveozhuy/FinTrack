import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '../lib/api'
import type { Paged, Transaction, TransactionType } from '../types'

export interface TransactionFilters {
  from?: string
  to?: string
  categoryId?: string
  type?: TransactionType | ''
  search?: string
  sortBy?: 'Date' | 'Amount'
  sortDir?: 'Asc' | 'Desc'
  page?: number
  pageSize?: number
}

export interface TransactionInput {
  type: TransactionType
  amount: number
  currency: string
  description?: string
  categoryId: string
  transactionDate: string
}

const KEY = 'transactions'

export function useTransactions(filters: TransactionFilters) {
  return useQuery({
    queryKey: [KEY, filters],
    queryFn: async () => {
      const params: Record<string, string | number> = {}
      Object.entries(filters).forEach(([key, value]) => {
        if (value !== undefined && value !== '' && value !== null) {
          params[key] = value as string | number
        }
      })
      return (await api.get<Paged<Transaction>>('/transactions', { params })).data
    },
  })
}

export function useCreateTransaction() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: async (body: TransactionInput) => (await api.post<Transaction>('/transactions', body)).data,
    onSuccess: () => qc.invalidateQueries({ queryKey: [KEY] }),
  })
}

export function useUpdateTransaction() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: async ({ id, body }: { id: string; body: TransactionInput }) =>
      (await api.put<Transaction>(`/transactions/${id}`, body)).data,
    onSuccess: () => qc.invalidateQueries({ queryKey: [KEY] }),
  })
}

export function useDeleteTransaction() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: async (id: string) => {
      await api.delete(`/transactions/${id}`)
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: [KEY] }),
  })
}
