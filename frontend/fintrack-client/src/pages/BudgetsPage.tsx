import { useState } from 'react'
import type { FormEvent } from 'react'
import { useBudgets, useCreateBudget, useDeleteBudget } from '../api/budgets'
import { useCategories } from '../api/categories'
import { Badge, EmptyState, ErrorState, Field, Modal, PageHeader, Spinner } from '../components/ui'
import { useToast } from '../context/ToastContext'
import { getApiErrorMessage } from '../lib/api'
import { formatCurrency } from '../lib/format'
import { budgetStatusLabel, MONTH_NAMES_TR } from '../lib/labels'
import type { BudgetStatus } from '../types'

const now = new Date()
const tone: Record<BudgetStatus, 'emerald' | 'amber' | 'rose'> = { Ok: 'emerald', Warning: 'amber', Exceeded: 'rose' }

export function BudgetsPage() {
  const [year, setYear] = useState(now.getFullYear())
  const [month, setMonth] = useState(now.getMonth() + 1)
  const { data, isLoading, isError } = useBudgets(year, month)
  const { data: categories } = useCategories()
  const createBudget = useCreateBudget()
  const deleteBudget = useDeleteBudget()
  const { notify } = useToast()

  const [open, setOpen] = useState(false)
  const [categoryId, setCategoryId] = useState('')
  const [limit, setLimit] = useState('')

  const expenseCategories = categories?.filter((c) => c.type === 'Expense') ?? []

  async function submit(event: FormEvent) {
    event.preventDefault()
    try {
      await createBudget.mutateAsync({ categoryId, year, month, monthlyLimit: Number(limit) })
      notify('success', 'Bütçe oluşturuldu.')
      setOpen(false)
      setCategoryId('')
      setLimit('')
    } catch (error) {
      notify('error', getApiErrorMessage(error))
    }
  }

  async function remove(id: string) {
    if (!window.confirm('Bu bütçe silinsin mi?')) return
    try {
      await deleteBudget.mutateAsync(id)
      notify('success', 'Bütçe silindi.')
    } catch (error) {
      notify('error', getApiErrorMessage(error))
    }
  }

  return (
    <div>
      <PageHeader
        title="Bütçeler"
        subtitle="Kategori bazında aylık limitler"
        action={
          <div className="flex gap-2">
            <select className="input w-36" value={month} onChange={(e) => setMonth(Number(e.target.value))}>
              {MONTH_NAMES_TR.map((name, index) => <option key={name} value={index + 1}>{name}</option>)}
            </select>
            <select className="input w-28" value={year} onChange={(e) => setYear(Number(e.target.value))}>
              {[now.getFullYear(), now.getFullYear() - 1].map((y) => <option key={y} value={y}>{y}</option>)}
            </select>
            <button className="btn-primary" onClick={() => setOpen(true)}>Yeni bütçe</button>
          </div>
        }
      />

      {isLoading && <Spinner label="Bütçeler yükleniyor…" />}
      {isError && <ErrorState message="Bütçeler yüklenemedi." />}

      {data && (data.length === 0 ? (
        <EmptyState title="Bu ay bütçe yok" hint="Harcamayı takip etmek için bütçe oluşturun." />
      ) : (
        <div className="grid gap-4 sm:grid-cols-2">
          {data.map((b) => (
            <div key={b.id} className="card">
              <div className="mb-2 flex items-center justify-between">
                <div className="flex items-center gap-2">
                  <span className="font-medium text-slate-800">{b.categoryName}</span>
                  <Badge tone={tone[b.status]}>{budgetStatusLabel(b.status)}</Badge>
                </div>
                <button className="text-sm text-rose-600 hover:underline" onClick={() => remove(b.id)}>Sil</button>
              </div>
              <div className="h-2.5 overflow-hidden rounded-full bg-slate-100">
                <div
                  className={`h-full ${b.status === 'Exceeded' ? 'bg-rose-500' : b.status === 'Warning' ? 'bg-amber-500' : 'bg-emerald-500'}`}
                  style={{ width: `${Math.min(100, b.usagePercentage)}%` }}
                />
              </div>
              <div className="mt-2 flex justify-between text-sm text-slate-500">
                <span>{formatCurrency(b.spent)} harcandı</span>
                <span>{formatCurrency(b.remaining)} kaldı · %{b.usagePercentage}</span>
              </div>
              <p className="mt-1 text-xs text-slate-400">Limit {formatCurrency(b.monthlyLimit)}</p>
            </div>
          ))}
        </div>
      ))}

      {open && (
        <Modal title="Yeni bütçe" onClose={() => setOpen(false)}>
          <form onSubmit={submit} className="space-y-4">
            <Field label="Kategori (gider)">
              <select className="input" value={categoryId} onChange={(e) => setCategoryId(e.target.value)} required>
                <option value="" disabled>Kategori seçin</option>
                {expenseCategories.map((c) => <option key={c.id} value={c.id}>{c.name}</option>)}
              </select>
            </Field>
            <Field label="Aylık limit">
              <input className="input" type="number" min="0.01" step="0.01" value={limit} onChange={(e) => setLimit(e.target.value)} required />
            </Field>
            <p className="text-xs text-slate-400">Bütçe {MONTH_NAMES_TR[month - 1]} {year} için geçerlidir.</p>
            <div className="flex justify-end gap-2">
              <button type="button" className="btn-secondary" onClick={() => setOpen(false)}>İptal</button>
              <button type="submit" className="btn-primary" disabled={createBudget.isPending}>Oluştur</button>
            </div>
          </form>
        </Modal>
      )}
    </div>
  )
}
