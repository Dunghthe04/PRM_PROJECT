import {
  createContext,
  useCallback,
  useContext,
  useMemo,
  useState,
  type ReactNode,
} from 'react'
import { api } from '../api/client'
import type { LoginResponse, User, UserRole } from '../types'

interface AuthState {
  user: User | null
  token: string | null
  login: (phone: string, password: string) => Promise<User>
  logout: () => void
  isAdmin: boolean
  isTeacher: boolean
}

const AuthContext = createContext<AuthState | null>(null)

function loadUser(): User | null {
  try {
    const raw = localStorage.getItem('fschool_user')
    return raw ? (JSON.parse(raw) as User) : null
  } catch {
    return null
  }
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [token, setToken] = useState<string | null>(
    () => localStorage.getItem('fschool_token'),
  )
  const [user, setUser] = useState<User | null>(() => loadUser())

  const login = useCallback(async (phone: string, password: string) => {
    const { data } = await api.post<LoginResponse>('/User/login', {
      phone,
      password,
    })
    localStorage.setItem('fschool_token', data.token)
    localStorage.setItem('fschool_user', JSON.stringify(data.user))
    setToken(data.token)
    setUser(data.user)
    return data.user
  }, [])

  const logout = useCallback(() => {
    localStorage.removeItem('fschool_token')
    localStorage.removeItem('fschool_user')
    setToken(null)
    setUser(null)
  }, [])

  const role = (user?.role ?? '') as UserRole | string
  const value = useMemo(
    () => ({
      user,
      token,
      login,
      logout,
      isAdmin: role === 'Admin',
      isTeacher: role === 'Teacher',
    }),
    [user, token, login, logout, role],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth() {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth phải dùng trong AuthProvider')
  return ctx
}
