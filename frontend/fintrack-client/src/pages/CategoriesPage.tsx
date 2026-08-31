import { useState } from 'react'
import type { FormEvent } from 'react'
import { useCategories, useCreateCategory, useDeleteCategory } from '../api/categories'
import { Badge, EmptyState, ErrorState, Field, Modal, PageHeader, Spinner } from '../components/ui'
import { useToast } from '../context/ToastContext'
import { getApiErrorMessage } from '../lib/api'
import type { CategoryType } from '../types'

export function CategoriesPage() {
  const { data, isLoading, isError } = useCategories()
  const createCategory = useCreateCategory()
  const deleteCategory = useDeleteCategory()
  const { notify } = useToast()

  const [open, setOpen] = useState(false)
  const [name, setName] = useState('')
  const [type, setType] = useState<CategoryType>('Expense')

  async function submit(event: FormEvent) {
    event.preventDefault()
    try {
      await createCategory.mutateAsync({ name, type })
      notify('success', 'Category created.')
      setOpen(false)
      setName('')
    } catch (error) {
      notify('error', getApiErrorMessage(error))
    }
  }

  async function remove(id: string) {
    if (!window.confirm('Archive this category? Existing transactions are kept.')) return
    try {
      await deleteCategory.mutateAsync(id)
      notify('success', 'Category archived.')
    } catch (error) {
      notify('error', getApiErrorMessage(error))
    }
  }

  return (
    <div>
      <PageHeader
        title="Categories"
        subtitle="Organize your income and expenses"
        action={<button className="btn-primary" onClick={() => setOpen(true)}>New category</button>}
      />

      {isLoading && <Spinner label="Loading categories…" />}
      {isError && <ErrorState message="Could not load categories." />}

      {data && (data.length === 0 ? (
        <EmptyState title="No categories yet" hint="Create your first category." />
      ) : (
        <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
          {data.map((category) => (
            <div key={category.id} className="card flex items-center justify-between">
              <div>
                <p className="font-medium text-slate-800">{category.name}</p>
                <div className="mt-1 flex gap-2">
                  <Badge tone={category.type === 'Income' ? 'emerald' : 'slate'}>{category.type}</Badge>
                  {category.isDefault && <Badge>Default</Badge>}
                </div>
              </div>
              <button className="text-sm text-rose-600 hover:underline" onClick={() => remove(category.id)}>
                Archive
              </button>
            </div>
          ))}
        </div>
      ))}

      {open && (
        <Modal title="New category" onClose={() => setOpen(false)}>
          <form onSubmit={submit} className="space-y-4">
            <Field label="Name">
              <input className="input" value={name} onChange={(e) => setName(e.target.value)} required maxLength={64} />
            </Field>
            <Field label="Type">
              <select className="input" value={type} onChange={(e) => setType(e.target.value as CategoryType)}>
                <option value="Expense">Expense</option>
                <option value="Income">Income</option>
              </select>
            </Field>
            <div className="flex justify-end gap-2">
              <button type="button" className="btn-secondary" onClick={() => setOpen(false)}>Cancel</button>
              <button type="submit" className="btn-primary" disabled={createCategory.isPending}>Create</button>
            </div>
          </form>
        </Modal>
      )}
    </div>
  )
}
