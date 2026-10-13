import { Navigate, Outlet, useLocation } from 'react-router-dom'
import useAuth from '../features/auth/useAuth.js'
import LoadingState from '../shared/ui/states/LoadingState.jsx'

export default function ProtectedRoute({ role }) {
  const { user, loading, status, error, refreshUser } = useAuth()
  const location = useLocation()
  if (loading) return <LoadingState fullscreen />
  if (status === 'unavailable') return <div className="container py-5"><div className="alert alert-warning">{error}</div><button className="btn btn-primary" onClick={() => refreshUser().catch(() => {})}>Thử tải lại tài khoản</button></div>
  if (!user) return <Navigate to="/login" state={{ from: location }} replace />
  if (role && user.role !== role) return <Navigate to="/" replace />
  return <Outlet />
}
