import { useQuery } from '@tanstack/react-query'
import { api } from '../lib/api'
import type { Dashboard } from '../types'

export function useDashboard(year: number, month: number) {
  return useQuery({
    queryKey: ['dashboard', year, month],
    queryFn: async () =>
      (await api.get<Dashboard>('/dashboard', { params: { year, month } })).data,
  })
}
