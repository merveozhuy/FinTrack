import { useState } from 'react'
import type { FormEvent } from 'react'
import { useConversation, useConversations, useDeleteConversation, useSendMessage } from '../api/assistant'
import { EmptyState, PageHeader, Spinner } from '../components/ui'
import { useToast } from '../context/ToastContext'
import { getApiErrorMessage } from '../lib/api'

const suggestions = [
  'How much did I spend this month?',
  'Which categories am I over budget on?',
  'Summarize my finances and where I should be careful',
  'What are my upcoming payments?',
]

export function AssistantPage() {
  const [selectedId, setSelectedId] = useState<string | null>(null)
  const [input, setInput] = useState('')
  const { data: conversations } = useConversations()
  const { data: conversation, isLoading: loadingConversation } = useConversation(selectedId)
  const sendMessage = useSendMessage()
  const deleteConversation = useDeleteConversation()
  const { notify } = useToast()

  async function send(message: string) {
    if (!message.trim() || sendMessage.isPending) return
    try {
      const result = await sendMessage.mutateAsync({ message: message.trim(), conversationId: selectedId })
      setSelectedId(result.conversationId)
      setInput('')
    } catch (error) {
      notify('error', getApiErrorMessage(error))
    }
  }

  function onSubmit(event: FormEvent) {
    event.preventDefault()
    void send(input)
  }

  async function remove(id: string) {
    if (!window.confirm('Delete this conversation?')) return
    try {
      await deleteConversation.mutateAsync(id)
      if (selectedId === id) setSelectedId(null)
    } catch (error) {
      notify('error', getApiErrorMessage(error))
    }
  }

  const messages = conversation?.messages ?? []

  return (
    <div>
      <PageHeader title="AI Assistant" subtitle="Ask about your own finances — answers are grounded in your data" />

      <div className="grid gap-4 lg:grid-cols-[240px_1fr]">
        <aside className="card h-fit">
          <button
            className="btn-primary mb-3 w-full"
            onClick={() => { setSelectedId(null); setInput('') }}
          >
            New chat
          </button>
          <ul className="space-y-1">
            {conversations?.map((c) => (
              <li key={c.id} className="group flex items-center justify-between gap-1">
                <button
                  onClick={() => setSelectedId(c.id)}
                  className={`flex-1 truncate rounded-lg px-2 py-1.5 text-left text-sm ${
                    selectedId === c.id ? 'bg-brand-50 text-brand-700' : 'text-slate-600 hover:bg-slate-100'
                  }`}
                  title={c.title}
                >
                  {c.title}
                </button>
                <button
                  onClick={() => remove(c.id)}
                  className="text-slate-300 hover:text-rose-600"
                  aria-label="Delete conversation"
                >
                  ✕
                </button>
              </li>
            ))}
            {conversations?.length === 0 && <li className="px-2 py-1 text-xs text-slate-400">No conversations yet</li>}
          </ul>
        </aside>

        <section className="card flex h-[70vh] flex-col">
          <div className="flex-1 space-y-4 overflow-y-auto pr-1">
            {selectedId === null && messages.length === 0 && (
              <div className="space-y-4">
                <EmptyState title="Ask me anything about your finances" hint="I only use your own data to answer." />
                <div className="flex flex-wrap gap-2">
                  {suggestions.map((s) => (
                    <button key={s} onClick={() => void send(s)} className="btn-secondary text-xs">
                      {s}
                    </button>
                  ))}
                </div>
              </div>
            )}

            {loadingConversation && selectedId !== null && <Spinner label="Loading conversation…" />}

            {messages.map((m, index) => (
              <div key={index} className={`flex ${m.role === 'User' ? 'justify-end' : 'justify-start'}`}>
                <div
                  className={`max-w-[80%] whitespace-pre-wrap rounded-xl px-4 py-2.5 text-sm ${
                    m.role === 'User' ? 'bg-brand-600 text-white' : 'border border-slate-200 bg-slate-50 text-slate-800'
                  }`}
                >
                  {m.content}
                </div>
              </div>
            ))}

            {sendMessage.isPending && (
              <div className="flex justify-start">
                <div className="rounded-xl border border-slate-200 bg-slate-50 px-4 py-2.5 text-sm text-slate-400">
                  Thinking…
                </div>
              </div>
            )}
          </div>

          <form onSubmit={onSubmit} className="mt-4 flex gap-2 border-t border-slate-100 pt-4">
            <input
              className="input"
              placeholder="Ask about your spending, budgets, upcoming payments…"
              value={input}
              onChange={(e) => setInput(e.target.value)}
              disabled={sendMessage.isPending}
            />
            <button type="submit" className="btn-primary" disabled={sendMessage.isPending || !input.trim()}>
              Send
            </button>
          </form>
        </section>
      </div>
    </div>
  )
}
