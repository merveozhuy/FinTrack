import { useState } from 'react'
import type { FormEvent } from 'react'
import { useCreateRecurring, useDeleteRecurring, useRecurring, useUpdateRecurringStatus } from '../api/recurring'
import { useCategories } from '../api/categories'
import { Badge, EmptyState, ErrorState, Field, Modal, PageHeader, Spinner } from '../components/ui'
import { useToast } from '../context/ToastContext'
import { getApiErrorMessage } from '../lib/api'
import { formatCurrency, formatDate } from '../lib/format'
import { frequencyLabel, typeLabel } from '../lib/labels'
import type { RecurrenceFrequency, TransactionType } from '../types'

const today = new Date().toISOString().slice(0, 10)

export function RecurringPage() {
  const { data, isLoading, isError } = useRecurring()
  const { data: categories } = useCategories()
  const createRule = useCreateRecurring()
  const updateStatus = useUpdateRecurringStatus()
  const deleteRule = useDeleteRecurring()
  const { notify } = useToast()

  const [open, setOpen] = useState(false)
  const [type, setType] = useState<TransactionType>('Expense')
  const [amount, setAmount] = useState('')
  const [categoryId, setCategoryId] = useState('')
  const [frequency, setFrequency] = useState<RecurrenceFrequency>('Monthly')
  const [startDate, setStartDate] = useState(today)

  const categoryOptions = categories?.filter((c) => c.type === type) ?? []

  async function submit(event: FormEvent) {
    event.preventDefault()
    try {
      await createRule.mutateAsync({ type, amount: Number(amount), currency: 'TRY', categoryId, frequency, startDate })
      notify('success', 'Yinelenen ödeme oluşturuldu.')
      setOpen(false)
      setAmount('')
      setCategoryId('')
    } catch (error) {
      notify('error', getApiErrorMessage(error))
    }
  }

  async function toggle(id: string, isActive: boolean) {
    try {
      await updateStatus.mutateAsync({ id, isActive: !isActive })
    } catch (error) {
      notify('error', getApiErrorMessage(error))
    }
  }

  async function remove(id: string) {
    if (!window.confirm('Bu yinelenen ödeme silinsin mi?')) return
    try {
      await deleteRule.mutateAsync(id)
      notify('success', 'Yinelenen ödeme silindi.')
    } catch (error) {
      notify('error', getApiErrorMessage(error))
    }
  }

  return (
    <div>
      <PageHeader
        title="Yinelenen ödemeler"
        subtitle="Kurallar zamanı gelince otomatik işlem oluşturur"
        action={<button className="btn-primary" onClick={() => setOpen(true)}>Yeni yinelenen</button>}
      />

      {isLoading && <Spinner label="Yükleniyor…" />}
      {isError && <ErrorState message="Yinelenen ödemeler yüklenemedi." />}

      {data && (data.length === 0 ? (
        <EmptyState title="Yinelenen ödeme yok" hint="Düzenli gelir veya faturaları otomatikleştirin." />
      ) : (
        <div className="overflow-x-auto rounded-2xl border border-slate-200 bg-white">
          <table className="w-full text-sm">
            <thead className="bg-slate-50 text-left text-xs uppercase tracking-wide text-slate-500">
              <tr>
                <th className="px-4 py-3">Kategori</th>
                <th className="px-4 py-3">Tutar</th>
                <th className="px-4 py-3">Sıklık</th>
                <th className="px-4 py-3">Sonraki</th>
                <th className="px-4 py-3">Durum</th>
                <th className="px-4 py-3 text-right">İşlemler</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {data.map((rule) => (
                <tr key={rule.id}>
                  <td className="px-4 py-3">
                    <span className="font-medium text-slate-800">{rule.categoryName}</span>
                    {rule.description && <span className="ml-1 text-slate-400">· {rule.description}</span>}
                    <span className="ml-2"><Badge tone={rule.type === 'Income' ? 'emerald' : 'slate'}>{typeLabel(rule.type)}</Badge></span>
                  </td>
                  <td className={`px-4 py-3 font-medium ${rule.type === 'Income' ? 'text-emerald-600' : 'text-slate-800'}`}>
                    {rule.type === 'Income' ? '+' : '−'}{formatCurrency(rule.amount)}
                  </td>
                  <td className="px-4 py-3 text-slate-600">{frequencyLabel(rule.frequency)}</td>
                  <td className="px-4 py-3 text-slate-600">{formatDate(rule.nextExecutionDate)}</td>
                  <td className="px-4 py-3">
                    <Badge tone={rule.isActive ? 'emerald' : 'slate'}>{rule.isActive ? 'Aktif' : 'Duraklatıldı'}</Badge>
                  </td>
                  <td className="px-4 py-3 text-right">
                    <button className="mr-3 text-brand-600 hover:underline" onClick={() => toggle(rule.id, rule.isActive)}>
                      {rule.isActive ? 'Duraklat' : 'Devam et'}
                    </button>
                    <button className="text-rose-600 hover:underline" onClick={() => remove(rule.id)}>Sil</button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      ))}

      {open && (
        <Modal title="Yeni yinelenen ödeme" onClose={() => setOpen(false)}>
          <form onSubmit={submit} className="space-y-4">
            <div className="grid grid-cols-2 gap-4">
              <Field label="Tür">
                <select className="input" value={type} onChange={(e) => { setType(e.target.value as TransactionType); setCategoryId('') }}>
                  <option value="Expense">Gider</option>
                  <option value="Income">Gelir</option>
                </select>
              </Field>
              <Field label="Tutar">
                <input className="input" type="number" min="0.01" step="0.01" value={amount} onChange={(e) => setAmount(e.target.value)} required />
              </Field>
            </div>
            <Field label="Kategori">
              <select className="input" value={categoryId} onChange={(e) => setCategoryId(e.target.value)} required>
                <option value="" disabled>Kategori seçin</option>
                {categoryOptions.map((c) => <option key={c.id} value={c.id}>{c.name}</option>)}
              </select>
            </Field>
            <div className="grid grid-cols-2 gap-4">
              <Field label="Sıklık">
                <select className="input" value={frequency} onChange={(e) => setFrequency(e.target.value as RecurrenceFrequency)}>
                  <option value="Weekly">Haftalık</option>
                  <option value="Monthly">Aylık</option>
                  <option value="Yearly">Yıllık</option>
                </select>
              </Field>
              <Field label="Başlangıç tarihi">
                <input className="input" type="date" value={startDate} onChange={(e) => setStartDate(e.target.value)} required />
              </Field>
            </div>
            <div className="flex justify-end gap-2">
              <button type="button" className="btn-secondary" onClick={() => setOpen(false)}>İptal</button>
              <button type="submit" className="btn-primary" disabled={createRule.isPending}>Oluştur</button>
            </div>
          </form>
        </Modal>
      )}
    </div>
  )
}
