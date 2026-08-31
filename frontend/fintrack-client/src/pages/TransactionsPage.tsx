import { useState } from 'react'
import type { FormEvent } from 'react'
import {
  useCreateTransaction, useDeleteTransaction, useTransactions, useUpdateTransaction,
} from '../api/transactions'
import type { TransactionFilters } from '../api/transactions'
import { useCategories } from '../api/categories'
import { Badge, EmptyState, ErrorState, Field, Modal, PageHeader, Spinner } from '../components/ui'
import { useToast } from '../context/ToastContext'
import { getApiErrorMessage } from '../lib/api'
import { formatCurrency, formatDate } from '../lib/format'
import type { Transaction, TransactionType } from '../types'

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

export function TransactionsPage() {
  const [filters, setFilters] = useState<TransactionFilters>({ sortBy: 'Date', sortDir: 'Desc' })
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

  function openCreate() {
    setForm(emptyForm)
    setOpen(true)
  }

  function openEdit(t: Transaction) {
    setForm({
      id: t.id,
      type: t.type,
      amount: String(t.amount),
      categoryId: t.categoryId,
      transactionDate: t.transactionDate,
      description: t.description ?? '',
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
        notify('success', 'Transaction updated.')
      } else {
        await createTransaction.mutateAsync(body)
        notify('success', 'Transaction added.')
      }
      setOpen(false)
    } catch (error) {
      notify('error', getApiErrorMessage(error))
    }
  }

  async function remove(id: string) {
    if (!window.confirm('Delete this transaction?')) return
    try {
      await deleteTransaction.mutateAsync(id)
      notify('success', 'Transaction deleted.')
    } catch (error) {
      notify('error', getApiErrorMessage(error))
    }
  }

  return (
    <div>
      <PageHeader
        title="Transactions"
        subtitle="All your income and expenses"
        action={<button className="btn-primary" onClick={openCreate}>Add transaction</button>}
      />

      <div className="mb-4 grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
        <input
          className="input"
          placeholder="Search description…"
          value={filters.search ?? ''}
          onChange={(e) => updateFilter({ search: e.target.value })}
        />
        <select className="input" value={filters.type ?? ''} onChange={(e) => updateFilter({ type: e.target.value as TransactionType | '' })}>
          <option value="">All types</option>
          <option value="Income">Income</option>
          <option value="Expense">Expense</option>
        </select>
        <select className="input" value={filters.categoryId ?? ''} onChange={(e) => updateFilter({ categoryId: e.target.value })}>
          <option value="">All categories</option>
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
          <option value="Date-Desc">Newest first</option>
          <option value="Date-Asc">Oldest first</option>
          <option value="Amount-Desc">Amount: high to low</option>
          <option value="Amount-Asc">Amount: low to high</option>
        </select>
      </div>

      {isLoading && <Spinner label="Loading transactions…" />}
      {isError && <ErrorState message="Could not load transactions." />}

      {data && (data.items.length === 0 ? (
        <EmptyState title="No transactions found" hint="Adjust filters or add a new transaction." />
      ) : (
        <>
          <div className="overflow-x-auto rounded-xl border border-slate-200 bg-white">
            <table className="w-full text-sm">
              <thead className="bg-slate-50 text-left text-xs uppercase tracking-wide text-slate-500">
                <tr>
                  <th className="px-4 py-3">Date</th>
                  <th className="px-4 py-3">Category</th>
                  <th className="px-4 py-3">Description</th>
                  <th className="px-4 py-3 text-right">Amount</th>
                  <th className="px-4 py-3 text-right">Actions</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100">
                {data.items.map((t) => (
                  <tr key={t.id}>
                    <td className="px-4 py-3 text-slate-600">{formatDate(t.transactionDate)}</td>
                    <td className="px-4 py-3">
                      <span className="font-medium text-slate-800">{t.categoryName}</span>{' '}
                      <Badge tone={t.type === 'Income' ? 'emerald' : 'slate'}>{t.type}</Badge>
                    </td>
                    <td className="px-4 py-3 text-slate-500">{t.description ?? '—'}</td>
                    <td className={`px-4 py-3 text-right font-semibold ${t.type === 'Income' ? 'text-emerald-600' : 'text-slate-800'}`}>
                      {t.type === 'Income' ? '+' : '−'}{formatCurrency(t.amount)}
                    </td>
                    <td className="px-4 py-3 text-right">
                      <button className="mr-3 text-brand-600 hover:underline" onClick={() => openEdit(t)}>Edit</button>
                      <button className="text-rose-600 hover:underline" onClick={() => remove(t.id)}>Delete</button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          <div className="mt-4 flex items-center justify-between text-sm text-slate-500">
            <span>{data.totalCount} transactions</span>
            <div className="flex items-center gap-3">
              <button className="btn-secondary" disabled={page <= 1} onClick={() => setPage((p) => p - 1)}>Previous</button>
              <span>Page {data.page} of {Math.max(1, data.totalPages)}</span>
              <button className="btn-secondary" disabled={page >= data.totalPages} onClick={() => setPage((p) => p + 1)}>Next</button>
            </div>
          </div>
        </>
      ))}

      {open && (
        <Modal title={form.id ? 'Edit transaction' : 'Add transaction'} onClose={() => setOpen(false)}>
          <form onSubmit={submit} className="space-y-4">
            <div className="grid grid-cols-2 gap-4">
              <Field label="Type">
                <select
                  className="input"
                  value={form.type}
                  onChange={(e) => setForm((f) => ({ ...f, type: e.target.value as TransactionType, categoryId: '' }))}
                >
                  <option value="Expense">Expense</option>
                  <option value="Income">Income</option>
                </select>
              </Field>
              <Field label="Amount">
                <input className="input" type="number" min="0.01" step="0.01" value={form.amount} onChange={(e) => setForm((f) => ({ ...f, amount: e.target.value }))} required />
              </Field>
            </div>
            <Field label="Category">
              <select className="input" value={form.categoryId} onChange={(e) => setForm((f) => ({ ...f, categoryId: e.target.value }))} required>
                <option value="" disabled>Select a category</option>
                {formCategories.map((c) => <option key={c.id} value={c.id}>{c.name}</option>)}
              </select>
            </Field>
            <Field label="Date">
              <input className="input" type="date" value={form.transactionDate} onChange={(e) => setForm((f) => ({ ...f, transactionDate: e.target.value }))} required />
            </Field>
            <Field label="Description (optional)">
              <input className="input" maxLength={512} value={form.description} onChange={(e) => setForm((f) => ({ ...f, description: e.target.value }))} />
            </Field>
            <div className="flex justify-end gap-2">
              <button type="button" className="btn-secondary" onClick={() => setOpen(false)}>Cancel</button>
              <button type="submit" className="btn-primary" disabled={createTransaction.isPending || updateTransaction.isPending}>
                {form.id ? 'Save' : 'Add'}
              </button>
            </div>
          </form>
        </Modal>
      )}
    </div>
  )
}
