import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '../lib/api'
import type { CreditCard } from '../types'

export interface CreditCardInput {
  name: string
  last4?: string
  creditLimit?: number | null
}

export interface CardPaymentInput {
  amount: number
  paymentDate: string
}

const KEY = 'credit-cards'

export function useCreditCards() {
  return useQuery({
    queryKey: [KEY],
    queryFn: async () => (await api.get<CreditCard[]>('/credit-cards')).data,
  })
}

export function useCreateCreditCard() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: async (body: CreditCardInput) => (await api.post<CreditCard>('/credit-cards', body)).data,
    onSuccess: () => qc.invalidateQueries({ queryKey: [KEY] }),
  })
}

export function useAddCardPayment() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: async ({ id, body }: { id: string; body: CardPaymentInput }) =>
      (await api.post<CreditCard>(`/credit-cards/${id}/payments`, body)).data,
    onSuccess: () => qc.invalidateQueries({ queryKey: [KEY] }),
  })
}

export function useDeleteCreditCard() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: async (id: string) => {
      await api.delete(`/credit-cards/${id}`)
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: [KEY] }),
  })
}
