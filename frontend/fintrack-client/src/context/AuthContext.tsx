import { createContext, useContext, useMemo, useState } from 'react'
import type { ReactNode } from 'react'
import { api } from '../lib/api'
import { authStorage } from '../lib/auth'
import type { AuthResponse, UserDto } from '../types'

interface AuthContextValue {
  user: UserDto | null
  isAuthenticated: boolean
  login: (email: string, password: string) => Promise<void>
  register: (email: string, password: string, displayName: string) => Promise<void>
  logout: () => void
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined)

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<UserDto | null>(() => authStorage.getUser())

  async function login(email: string, password: string) {
    const { data } = await api.post<AuthResponse>('/auth/login', { email, password })
    authStorage.set(data.accessToken, data.user)
    setUser(data.user)
  }

  async function register(email: string, password: string, displayName: string) {
    const { data } = await api.post<AuthResponse>('/auth/register', { email, password, displayName })
    authStorage.set(data.accessToken, data.user)
    setUser(data.user)
  }

  function logout() {
    authStorage.clear()
    setUser(null)
  }

  const value = useMemo<AuthContextValue>(
    () => ({ user, isAuthenticated: user !== null, login, register, logout }),
    [user],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

// eslint-disable-next-line react-refresh/only-export-components
export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth must be used within an AuthProvider')
  return ctx
}
