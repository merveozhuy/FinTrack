import { useState } from 'react'
import type { FormEvent } from 'react'
import {
  useCreateTransaction, useDeleteTransaction, useTransactions, useUpdateTransaction,
} from '../api/transactions'
import type { TransactionFilters } from '../api/transactions'
import { useCategories } from '../api/categories'
import { Badge, EmptyState, ErrorState, Field, Modal, PageHeader, Spinner } from '../components/ui'
import { useToast } from '../context/ToastContext'
import { api, getApiErrorMessage } from '../lib/api'
import { formatCurrency, formatDate } from '../lib/format'
import { typeLabel } from '../lib/labels'
import { downloadCsv } from '../lib/csv'
import type { Paged, Transaction, TransactionType } from '../types'

const today = new Date().toISOString().slice(0, 10)
const PAGE_SIZE = 10

interface FormState {
  id?: string
  type: TransactionType
  amount: string
  categoryId: string
  transactionDate: string
  description: string
}

const emptyForm: FormState = { type: 'Expense', amount: '', categoryId: '', transactionDate: today, description: '' }

function iso(date: Date): string {
  return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}`
}

function monthOffsetRange(startOffset: number, endOffset: number): { from: string; to: string } {
  const base = new Date()
  const start = new Date(base.getFullYear(), base.getMonth() + startOffset, 1)
  const end = new Date(base.getFullYear(), base.getMonth() + endOffset + 1, 0)
  return { from: iso(start), to: iso(end) }
}

const presets = [
  { key: 'thisMonth', label: 'Bu ay', range: () => monthOffsetRange(0, 0) },
  { key: 'lastMonth', label: 'Geçen ay', range: () => monthOffsetRange(-1, -1) },
  { key: 'last3', label: 'Son 3 ay', range: () => monthOffsetRange(-2, 0) },
  { key: 'all', label: 'Tümü', range: () => ({ from: undefined as string | undefined, to: undefined as string | undefined }) },
]

export function TransactionsPage() {
  const [filters, setFilters] = useState<TransactionFilters>({ sortBy: 'Date', sortDir: 'Desc' })
  const [preset, setPreset] = useState('all')
  const [page, setPage] = useState(1)
  const { data, isLoading, isError } = useTransactions({ ...filters, page, pageSize: PAGE_SIZE })
  const { data: categories } = useCategories()
  const createTransaction = useCreateTransaction()
  const updateTransaction = useUpdateTransaction()
  const deleteTransaction = useDeleteTransaction()
  const { notify } = useToast()

  const [open, setOpen] = useState(false)
  const [form, setForm] = useState<FormState>(emptyForm)

  const formCategories = categories?.filter((c) => c.type === form.type) ?? []

  function updateFilter(patch: Partial<TransactionFilters>) {
    setPage(1)
    setFilters((current) => ({ ...current, ...patch }))
  }

  function applyPreset(key: string) {
    setPreset(key)
    const found = presets.find((p) => p.key === key)
    if (found) {
      const range = found.range()
      updateFilter({ from: range.from, to: range.to })
    }
  }

  async function exportCsv() {
    try {
      const params: Record<string, string | number> = { page: 1, pageSize: 1000 }
      Object.entries(filters).forEach(([key, value]) => {
        if (value !== undefined && value !== '' && value !== null) params[key] = value as string | number
      })
      const result = await api.get<Paged<Transaction>>('/transactions', { params })
      const rows = result.data.items.map((t) => [
        t.transactionDate, typeLabel(t.type), t.categoryName, t.description ?? '', t.amount, t.currency,
      ])
      downloadCsv(`islemler-${today}.csv`, ['Tarih', 'Tür', 'Kategori', 'Açıklama', 'Tutar', 'Para Birimi'], rows)
      notify('success', `${rows.length} işlem dışa aktarıldı.`)
    } catch (error) {
      notify('error', getApiErrorMessage(error))
    }
  }

  function openCreate() {
    setForm(emptyForm)
    setOpen(true)
  }

  function openEdit(t: Transaction) {
    setForm({
      id: t.id, type: t.type, amount: String(t.amount), categoryId: t.categoryId,
      transactionDate: t.transactionDate, description: t.description ?? '',
    })
    setOpen(true)
  }

  async function submit(event: FormEvent) {
    event.preventDefault()
    const body = {
      type: form.type,
      amount: Number(form.amount),
      currency: 'TRY',
      categoryId: form.categoryId,
      description: form.description || undefined,
      transactionDate: form.transactionDate,
    }
    try {
      if (form.id) {
        await updateTransaction.mutateAsync({ id: form.id, body })
        notify('success', 'İşlem güncellendi.')
      } else {
        await createTransaction.mutateAsync(body)
        notify('success', 'İşlem eklendi.')
      }
      setOpen(false)
    } catch (error) {
      notify('error', getApiErrorMessage(error))
    }
  }

  async function remove(id: string) {
    if (!window.confirm('Bu işlem silinsin mi?')) return
    try {
      await deleteTransaction.mutateAsync(id)
      notify('success', 'İşlem silindi.')
    } catch (error) {
      notify('error', getApiErrorMessage(error))
    }
  }

  return (
    <div>
      <PageHeader
        title="İşlemler"
        subtitle="Tüm gelir ve giderleriniz"
        action={
          <div className="flex gap-2">
            <button className="btn-secondary" onClick={exportCsv}>CSV indir</button>
            <button className="btn-primary" onClick={openCreate}>İşlem ekle</button>
          </div>
        }
      />

      <div className="mb-3 flex flex-wrap gap-2">
        {presets.map((p) => (
          <button key={p.key} className={`chip ${preset === p.key ? 'chip-active' : ''}`} onClick={() => applyPreset(p.key)}>
            {p.label}
          </button>
        ))}
      </div>

      <div className="mb-4 grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
        <input className="input" placeholder="Açıklamada ara…" value={filters.search ?? ''} onChange={(e) => updateFilter({ search: e.target.value })} />
        <select className="input" value={filters.type ?? ''} onChange={(e) => updateFilter({ type: e.target.value as TransactionType | '' })}>
          <option value="">Tüm türler</option>
          <option value="Income">Gelir</option>
          <option value="Expense">Gider</option>
        </select>
        <select className="input" value={filters.categoryId ?? ''} onChange={(e) => updateFilter({ categoryId: e.target.value })}>
          <option value="">Tüm kategoriler</option>
          {categories?.map((c) => <option key={c.id} value={c.id}>{c.name}</option>)}
        </select>
        <select
          className="input"
          value={`${filters.sortBy}-${filters.sortDir}`}
          onChange={(e) => {
            const [sortBy, sortDir] = e.target.value.split('-') as ['Date' | 'Amount', 'Asc' | 'Desc']
            updateFilter({ sortBy, sortDir })
          }}
        >
          <option value="Date-Desc">En yeni</option>
          <option value="Date-Asc">En eski</option>
          <option value="Amount-Desc">Tutar: yüksekten düşüğe</option>
          <option value="Amount-Asc">Tutar: düşükten yükseğe</option>
        </select>
      </div>

      {isLoading && <Spinner label="İşlemler yükleniyor…" />}
      {isError && <ErrorState message="İşlemler yüklenemedi." />}

      {data && (data.items.length === 0 ? (
        <EmptyState title="İşlem bulunamadı" hint="Filtreleri değiştirin veya yeni işlem ekleyin." />
      ) : (
        <>
          <div className="overflow-x-auto rounded-2xl border border-slate-200 bg-white">
            <table className="w-full text-sm">
              <thead className="bg-slate-50 text-left text-xs uppercase tracking-wide text-slate-500">
                <tr>
                  <th className="px-4 py-3">Tarih</th>
                  <th className="px-4 py-3">Kategori</th>
                  <th className="px-4 py-3">Açıklama</th>
                  <th className="px-4 py-3 text-right">Tutar</th>
                  <th className="px-4 py-3 text-right">İşlemler</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100">
                {data.items.map((t) => (
                  <tr key={t.id}>
                    <td className="px-4 py-3 text-slate-600">{formatDate(t.transactionDate)}</td>
                    <td className="px-4 py-3">
                      <span className="font-medium text-slate-800">{t.categoryName}</span>{' '}
                      <Badge tone={t.type === 'Income' ? 'emerald' : 'slate'}>{typeLabel(t.type)}</Badge>
                    </td>
                    <td className="px-4 py-3 text-slate-500">{t.description ?? '—'}</td>
                    <td className={`px-4 py-3 text-right font-semibold ${t.type === 'Income' ? 'text-emerald-600' : 'text-slate-800'}`}>
                      {t.type === 'Income' ? '+' : '−'}{formatCurrency(t.amount)}
                    </td>
                    <td className="px-4 py-3 text-right">
                      <button className="mr-3 text-brand-600 hover:underline" onClick={() => openEdit(t)}>Düzenle</button>
                      <button className="text-rose-600 hover:underline" onClick={() => remove(t.id)}>Sil</button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          <div className="mt-4 flex items-center justify-between text-sm text-slate-500">
            <span>{data.totalCount} işlem</span>
            <div className="flex items-center gap-3">
              <button className="btn-secondary" disabled={page <= 1} onClick={() => setPage((p) => p - 1)}>Önceki</button>
              <span>Sayfa {data.page} / {Math.max(1, data.totalPages)}</span>
              <button className="btn-secondary" disabled={page >= data.totalPages} onClick={() => setPage((p) => p + 1)}>Sonraki</button>
            </div>
          </div>
        </>
      ))}

      {open && (
        <Modal title={form.id ? 'İşlem düzenle' : 'İşlem ekle'} onClose={() => setOpen(false)}>
          <form onSubmit={submit} className="space-y-4">
            <div className="grid grid-cols-2 gap-4">
              <Field label="Tür">
                <select className="input" value={form.type} onChange={(e) => setForm((f) => ({ ...f, type: e.target.value as TransactionType, categoryId: '' }))}>
                  <option value="Expense">Gider</option>
                  <option value="Income">Gelir</option>
                </select>
              </Field>
              <Field label="Tutar">
                <input className="input" type="number" min="0.01" step="0.01" value={form.amount} onChange={(e) => setForm((f) => ({ ...f, amount: e.target.value }))} required />
              </Field>
            </div>
            <Field label="Kategori">
              <select className="input" value={form.categoryId} onChange={(e) => setForm((f) => ({ ...f, categoryId: e.target.value }))} required>
                <option value="" disabled>Kategori seçin</option>
                {formCategories.map((c) => <option key={c.id} value={c.id}>{c.name}</option>)}
              </select>
            </Field>
            <Field label="Tarih">
              <input className="input" type="date" value={form.transactionDate} onChange={(e) => setForm((f) => ({ ...f, transactionDate: e.target.value }))} required />
            </Field>
            <Field label="Açıklama (opsiyonel)">
              <input className="input" maxLength={512} value={form.description} onChange={(e) => setForm((f) => ({ ...f, description: e.target.value }))} />
            </Field>
            <div className="flex justify-end gap-2">
              <button type="button" className="btn-secondary" onClick={() => setOpen(false)}>İptal</button>
              <button type="submit" className="btn-primary" disabled={createTransaction.isPending || updateTransaction.isPending}>
                {form.id ? 'Kaydet' : 'Ekle'}
              </button>
            </div>
          </form>
        </Modal>
      )}
    </div>
  )
}
