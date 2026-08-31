import { useState } from 'react'
import type { FormEvent } from 'react'
import { useAddCardPayment, useCreateCreditCard, useCreditCards, useDeleteCreditCard } from '../api/creditCards'
import { EmptyState, ErrorState, Field, Modal, PageHeader, Spinner } from '../components/ui'
import { useToast } from '../context/ToastContext'
import { getApiErrorMessage } from '../lib/api'
import { formatCurrency } from '../lib/format'

const today = new Date().toISOString().slice(0, 10)

export function CreditCardsPage() {
  const { data, isLoading, isError } = useCreditCards()
  const createCard = useCreateCreditCard()
  const addPayment = useAddCardPayment()
  const deleteCard = useDeleteCreditCard()
  const { notify } = useToast()

  const [addOpen, setAddOpen] = useState(false)
  const [name, setName] = useState('')
  const [last4, setLast4] = useState('')
  const [limit, setLimit] = useState('')

  const [payFor, setPayFor] = useState<string | null>(null)
  const [payAmount, setPayAmount] = useState('')
  const [payDate, setPayDate] = useState(today)

  async function submitCard(event: FormEvent) {
    event.preventDefault()
    try {
      await createCard.mutateAsync({
        name,
        last4: last4 || undefined,
        creditLimit: limit ? Number(limit) : null,
      })
      notify('success', 'Kart eklendi.')
      setAddOpen(false)
      setName(''); setLast4(''); setLimit('')
    } catch (error) {
      notify('error', getApiErrorMessage(error))
    }
  }

  async function submitPayment(event: FormEvent) {
    event.preventDefault()
    if (!payFor) return
    try {
      await addPayment.mutateAsync({ id: payFor, body: { amount: Number(payAmount), paymentDate: payDate } })
      notify('success', 'Ödeme kaydedildi.')
      setPayFor(null); setPayAmount(''); setPayDate(today)
    } catch (error) {
      notify('error', getApiErrorMessage(error))
    }
  }

  async function remove(id: string) {
    if (!window.confirm('Bu kart silinsin mi? İşlemleri karttan çözülür, ödemeleri silinir.')) return
    try {
      await deleteCard.mutateAsync(id)
      notify('success', 'Kart silindi.')
    } catch (error) {
      notify('error', getApiErrorMessage(error))
    }
  }

  return (
    <div>
      <PageHeader
        title="Kredi Kartları"
        subtitle="Kartlarınızı ve borçlarınızı takip edin"
        action={<button className="btn-primary" onClick={() => setAddOpen(true)}>Yeni kart</button>}
      />

      {isLoading && <Spinner label="Kartlar yükleniyor…" />}
      {isError && <ErrorState message="Kartlar yüklenemedi." />}

      {data && (data.length === 0 ? (
        <EmptyState title="Henüz kart yok" hint="Kart ekleyip harcamalarınızı ona bağlayın." />
      ) : (
        <div className="grid gap-5 sm:grid-cols-2 lg:grid-cols-3">
          {data.map((card) => (
            <div key={card.id}>
              <div className="rounded-2xl bg-gradient-to-br from-slate-800 to-slate-950 p-5 text-white shadow-soft">
                <div className="flex items-start justify-between">
                  <span className="text-sm font-medium text-slate-200">{card.name}</span>
                  <span className="h-6 w-9 rounded bg-gradient-to-br from-amber-300 to-amber-500" />
                </div>
                <p className="mt-6 font-mono tracking-[0.2em] text-slate-300">•••• •••• •••• {card.last4 ?? '••••'}</p>
                <div className="mt-4 flex items-end justify-between">
                  <div>
                    <p className="text-[11px] uppercase tracking-wide text-slate-400">Borç</p>
                    <p className="text-lg font-semibold text-rose-300">{formatCurrency(card.currentDebt)}</p>
                  </div>
                  <div className="text-right">
                    <p className="text-[11px] uppercase tracking-wide text-slate-400">
                      {card.availableLimit !== null && card.availableLimit !== undefined ? 'Kalan limit' : 'Limit'}
                    </p>
                    <p className="text-sm text-slate-200">
                      {card.availableLimit !== null && card.availableLimit !== undefined
                        ? formatCurrency(card.availableLimit)
                        : card.creditLimit ? formatCurrency(card.creditLimit) : '—'}
                    </p>
                  </div>
                </div>
                {card.usagePercentage !== null && card.usagePercentage !== undefined && (
                  <div className="mt-3 h-1.5 overflow-hidden rounded-full bg-white/15">
                    <div
                      className={`h-full ${card.usagePercentage >= 100 ? 'bg-rose-400' : card.usagePercentage >= 80 ? 'bg-amber-400' : 'bg-emerald-400'}`}
                      style={{ width: `${Math.min(100, card.usagePercentage)}%` }}
                    />
                  </div>
                )}
              </div>
              <div className="mt-2 flex justify-end gap-3 px-1 text-sm">
                <button className="text-brand-600 hover:underline" onClick={() => { setPayFor(card.id); setPayAmount(''); setPayDate(today) }}>
                  Ödeme ekle
                </button>
                <button className="text-rose-600 hover:underline" onClick={() => remove(card.id)}>Sil</button>
              </div>
            </div>
          ))}
        </div>
      ))}

      {addOpen && (
        <Modal title="Yeni kart" onClose={() => setAddOpen(false)}>
          <form onSubmit={submitCard} className="space-y-4">
            <Field label="Kart adı">
              <input className="input" value={name} onChange={(e) => setName(e.target.value)} required maxLength={64} placeholder="Örn. Garanti Bonus" />
            </Field>
            <div className="grid grid-cols-2 gap-4">
              <Field label="Son 4 hane (opsiyonel)">
                <input className="input" value={last4} onChange={(e) => setLast4(e.target.value)} maxLength={4} inputMode="numeric" placeholder="1234" />
              </Field>
              <Field label="Limit (opsiyonel)">
                <input className="input" type="number" min="0.01" step="0.01" value={limit} onChange={(e) => setLimit(e.target.value)} />
              </Field>
            </div>
            <div className="flex justify-end gap-2">
              <button type="button" className="btn-secondary" onClick={() => setAddOpen(false)}>İptal</button>
              <button type="submit" className="btn-primary" disabled={createCard.isPending}>Ekle</button>
            </div>
          </form>
        </Modal>
      )}

      {payFor && (
        <Modal title="Ödeme ekle" onClose={() => setPayFor(null)}>
          <form onSubmit={submitPayment} className="space-y-4">
            <Field label="Tutar">
              <input className="input" type="number" min="0.01" step="0.01" value={payAmount} onChange={(e) => setPayAmount(e.target.value)} required />
            </Field>
            <Field label="Tarih">
              <input className="input" type="date" value={payDate} onChange={(e) => setPayDate(e.target.value)} required />
            </Field>
            <div className="flex justify-end gap-2">
              <button type="button" className="btn-secondary" onClick={() => setPayFor(null)}>İptal</button>
              <button type="submit" className="btn-primary" disabled={addPayment.isPending}>Kaydet</button>
            </div>
          </form>
        </Modal>
      )}
    </div>
  )
}
