import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '../lib/api'
import type { ChatResponse, ConversationDetail, ConversationSummary } from '../types'

const KEY = 'conversations'

export function useConversations() {
  return useQuery({
    queryKey: [KEY],
    queryFn: async () => (await api.get<ConversationSummary[]>('/assistant/conversations')).data,
  })
}

export function useConversation(id: string | null) {
  return useQuery({
    queryKey: [KEY, id],
    queryFn: async () => (await api.get<ConversationDetail>(`/assistant/conversations/${id}`)).data,
    enabled: id !== null,
  })
}

export function useSendMessage() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: async (body: { message: string; conversationId: string | null }) =>
      (await api.post<ChatResponse>('/assistant/chat', body)).data,
    onSuccess: (data) => {
      qc.invalidateQueries({ queryKey: [KEY] })
      qc.invalidateQueries({ queryKey: [KEY, data.conversationId] })
    },
  })
}

export function useDeleteConversation() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: async (id: string) => {
      await api.delete(`/assistant/conversations/${id}`)
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: [KEY] }),
  })
}
