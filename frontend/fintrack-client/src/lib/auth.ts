import type { UserDto } from '../types'

const TOKEN_KEY = 'fintrack.accessToken'
const USER_KEY = 'fintrack.user'

export const authStorage = {
  getToken(): string | null {
    try {
      return localStorage.getItem(TOKEN_KEY)
    } catch {
      return null
    }
  },
  getUser(): UserDto | null {
    try {
      const raw = localStorage.getItem(USER_KEY)
      return raw ? (JSON.parse(raw) as UserDto) : null
    } catch {
      return null
    }
  },
  set(token: string, user: UserDto): void {
    try {
      localStorage.setItem(TOKEN_KEY, token)
      localStorage.setItem(USER_KEY, JSON.stringify(user))
    } catch {
      /* ignore storage errors (e.g. private mode) */
    }
  },
  clear(): void {
    try {
      localStorage.removeItem(TOKEN_KEY)
      localStorage.removeItem(USER_KEY)
    } catch {
      /* ignore */
    }
  },
}
