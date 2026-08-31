import { useState } from 'react'
import type { FormEvent } from 'react'
import { useCreateRecurring, useDeleteRecurring, useRecurring, useUpdateRecurringStatus } from '../api/recurring'
import { useCategories } from '../api/categories'
import { Badge, EmptyState, ErrorState, Field, Modal, PageHeader, Spinner } from '../components/ui'
import { useToast } from '../context/ToastContext'
import { getApiErrorMessage } from '../lib/api'
import { formatCurrency, formatDate } from '../lib/format'
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
      await createRule.mutateAsync({
        type, amount: Number(amount), currency: 'TRY', categoryId, frequency, startDate,
      })
      notify('success', 'Recurring payment created.')
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
    if (!window.confirm('Delete this recurring payment?')) return
    try {
      await deleteRule.mutateAsync(id)
      notify('success', 'Recurring payment deleted.')
    } catch (error) {
      notify('error', getApiErrorMessage(error))
    }
  }

  return (
    <div>
      <PageHeader
        title="Recurring payments"
        subtitle="Rules automatically generate transactions on schedule"
        action={<button className="btn-primary" onClick={() => setOpen(true)}>New recurring</button>}
      />

      {isLoading && <Spinner label="Loading…" />}
      {isError && <ErrorState message="Could not load recurring payments." />}

      {data && (data.length === 0 ? (
        <EmptyState title="No recurring payments" hint="Create one to automate regular income or bills." />
      ) : (
        <div className="overflow-x-auto rounded-xl border border-slate-200 bg-white">
          <table className="w-full text-sm">
            <thead className="bg-slate-50 text-left text-xs uppercase tracking-wide text-slate-500">
              <tr>
                <th className="px-4 py-3">Category</th>
                <th className="px-4 py-3">Amount</th>
                <th className="px-4 py-3">Frequency</th>
                <th className="px-4 py-3">Next</th>
                <th className="px-4 py-3">Status</th>
                <th className="px-4 py-3 text-right">Actions</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {data.map((rule) => (
                <tr key={rule.id}>
                  <td className="px-4 py-3">
                    <span className="font-medium text-slate-800">{rule.categoryName}</span>
                    {rule.description && <span className="ml-1 text-slate-400">· {rule.description}</span>}
                  </td>
                  <td className={`px-4 py-3 font-medium ${rule.type === 'Income' ? 'text-emerald-600' : 'text-slate-800'}`}>
                    {rule.type === 'Income' ? '+' : '−'}{formatCurrency(rule.amount)}
                  </td>
                  <td className="px-4 py-3 text-slate-600">{rule.frequency}</td>
                  <td className="px-4 py-3 text-slate-600">{formatDate(rule.nextExecutionDate)}</td>
                  <td className="px-4 py-3">
                    <Badge tone={rule.isActive ? 'emerald' : 'slate'}>{rule.isActive ? 'Active' : 'Paused'}</Badge>
                  </td>
                  <td className="px-4 py-3 text-right">
                    <button className="mr-3 text-brand-600 hover:underline" onClick={() => toggle(rule.id, rule.isActive)}>
                      {rule.isActive ? 'Pause' : 'Resume'}
                    </button>
                    <button className="text-rose-600 hover:underline" onClick={() => remove(rule.id)}>Delete</button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      ))}

      {open && (
        <Modal title="New recurring payment" onClose={() => setOpen(false)}>
          <form onSubmit={submit} className="space-y-4">
            <div className="grid grid-cols-2 gap-4">
              <Field label="Type">
                <select className="input" value={type} onChange={(e) => { setType(e.target.value as TransactionType); setCategoryId('') }}>
                  <option value="Expense">Expense</option>
                  <option value="Income">Income</option>
                </select>
              </Field>
              <Field label="Amount">
                <input className="input" type="number" min="0.01" step="0.01" value={amount} onChange={(e) => setAmount(e.target.value)} required />
              </Field>
            </div>
            <Field label="Category">
              <select className="input" value={categoryId} onChange={(e) => setCategoryId(e.target.value)} required>
                <option value="" disabled>Select a category</option>
                {categoryOptions.map((c) => <option key={c.id} value={c.id}>{c.name}</option>)}
              </select>
            </Field>
            <div className="grid grid-cols-2 gap-4">
              <Field label="Frequency">
                <select className="input" value={frequency} onChange={(e) => setFrequency(e.target.value as RecurrenceFrequency)}>
                  <option value="Weekly">Weekly</option>
                  <option value="Monthly">Monthly</option>
                  <option value="Yearly">Yearly</option>
                </select>
              </Field>
              <Field label="Start date">
                <input className="input" type="date" value={startDate} onChange={(e) => setStartDate(e.target.value)} required />
              </Field>
            </div>
            <div className="flex justify-end gap-2">
              <button type="button" className="btn-secondary" onClick={() => setOpen(false)}>Cancel</button>
              <button type="submit" className="btn-primary" disabled={createRule.isPending}>Create</button>
            </div>
          </form>
        </Modal>
      )}
    </div>
  )
}
