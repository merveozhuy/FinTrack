import { useState } from 'react'
import type { FormEvent } from 'react'
import { useCategories, useCreateCategory, useDeleteCategory } from '../api/categories'
import { Badge, EmptyState, ErrorState, Field, Modal, PageHeader, Spinner } from '../components/ui'
import { useToast } from '../context/ToastContext'
import { getApiErrorMessage } from '../lib/api'
import { typeLabel } from '../lib/labels'
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
      notify('success', 'Kategori oluşturuldu.')
      setOpen(false)
      setName('')
    } catch (error) {
      notify('error', getApiErrorMessage(error))
    }
  }

  async function remove(id: string) {
    if (!window.confirm('Bu kategori arşivlensin mi? Mevcut işlemler korunur.')) return
    try {
      await deleteCategory.mutateAsync(id)
      notify('success', 'Kategori arşivlendi.')
    } catch (error) {
      notify('error', getApiErrorMessage(error))
    }
  }

  return (
    <div>
      <PageHeader
        title="Kategoriler"
        subtitle="Gelir ve giderlerinizi düzenleyin"
        action={<button className="btn-primary" onClick={() => setOpen(true)}>Yeni kategori</button>}
      />

      {isLoading && <Spinner label="Kategoriler yükleniyor…" />}
      {isError && <ErrorState message="Kategoriler yüklenemedi." />}

      {data && (data.length === 0 ? (
        <EmptyState title="Henüz kategori yok" hint="İlk kategorinizi oluşturun." />
      ) : (
        <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
          {data.map((category) => (
            <div key={category.id} className="card flex items-center justify-between">
              <div>
                <p className="font-medium text-slate-800">{category.name}</p>
                <div className="mt-1 flex gap-2">
                  <Badge tone={category.type === 'Income' ? 'emerald' : 'slate'}>{typeLabel(category.type)}</Badge>
                  {category.isDefault && <Badge>Varsayılan</Badge>}
                </div>
              </div>
              <button className="text-sm text-rose-600 hover:underline" onClick={() => remove(category.id)}>Arşivle</button>
            </div>
          ))}
        </div>
      ))}

      {open && (
        <Modal title="Yeni kategori" onClose={() => setOpen(false)}>
          <form onSubmit={submit} className="space-y-4">
            <Field label="Ad">
              <input className="input" value={name} onChange={(e) => setName(e.target.value)} required maxLength={64} />
            </Field>
            <Field label="Tür">
              <select className="input" value={type} onChange={(e) => setType(e.target.value as CategoryType)}>
                <option value="Expense">Gider</option>
                <option value="Income">Gelir</option>
              </select>
            </Field>
            <div className="flex justify-end gap-2">
              <button type="button" className="btn-secondary" onClick={() => setOpen(false)}>İptal</button>
              <button type="submit" className="btn-primary" disabled={createCategory.isPending}>Oluştur</button>
            </div>
          </form>
        </Modal>
      )}
    </div>
  )
}
