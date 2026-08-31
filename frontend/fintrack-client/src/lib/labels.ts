import type { BudgetStatus, RecurrenceFrequency, TransactionType } from '../types'

export const MONTH_NAMES_TR = [
  'Ocak', 'Şubat', 'Mart', 'Nisan', 'Mayıs', 'Haziran',
  'Temmuz', 'Ağustos', 'Eylül', 'Ekim', 'Kasım', 'Aralık',
]

export function typeLabel(type: TransactionType): string {
  return type === 'Income' ? 'Gelir' : 'Gider'
}

export function frequencyLabel(frequency: RecurrenceFrequency): string {
  const map: Record<RecurrenceFrequency, string> = { Weekly: 'Haftalık', Monthly: 'Aylık', Yearly: 'Yıllık' }
  return map[frequency]
}

export function budgetStatusLabel(status: BudgetStatus): string {
  const map: Record<BudgetStatus, string> = { Ok: 'İyi', Warning: 'Uyarı', Exceeded: 'Aşıldı' }
  return map[status]
}
