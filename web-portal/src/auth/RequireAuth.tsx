import { Navigate, Outlet } from 'react-router-dom'
import { useAuth } from './AuthContext'

/** Bảo vệ route theo JWT + role. */
export function RequireAuth({ roles }: { roles?: string[] }) {
  const { user, token } = useAuth()

  if (!token || !user) {
    return <Navigate to="/login" replace />
  }

  if (roles && !roles.includes(String(user.role))) {
    // Sai role → đưa về khu vực đúng
    if (user.role === 'Admin') return <Navigate to="/admin" replace />
    if (user.role === 'Teacher') return <Navigate to="/teacher" replace />
    return <Navigate to="/login" replace />
  }

  return <Outlet />
}
