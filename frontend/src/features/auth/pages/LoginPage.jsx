import AuthShell from '../components/AuthShell.jsx'
import FormField from '../../../shared/ui/FormField.jsx'
import { useState } from 'react'
import { Link, Navigate, useLocation, useNavigate } from 'react-router-dom'
import useAuth from '../useAuth.js'
import AlertMessage from '../../../shared/ui/AlertMessage.jsx'

export default function LoginPage() {
  const { user, login } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const [form, setForm] = useState({ email: '', password: '' })
  const [error, setError] = useState('')
  const [busy, setBusy] = useState(false)
  if (user) return <Navigate to={user.role === 'ADMIN' ? '/admin' : '/'} replace />

  const submit = async (event) => {
    event.preventDefault()
    setBusy(true)
    setError('')
    try {
      const profile = await login(form.email, form.password)
      navigate(location.state?.from?.pathname || (profile?.role === 'ADMIN' ? '/admin' : '/'), { replace: true })
    } catch (err) {
      setError(err.message)
    } finally {
      setBusy(false)
    }
  }

  return (
    <AuthShell title="Chào mừng trở lại" subtitle="Đăng nhập để tiếp tục săn voucher.">
      <AlertMessage message={error} />
      <form onSubmit={submit}>
        <FormField icon="envelope" label="Email" type="email" value={form.email} onChange={(value) => setForm({ ...form, email: value })} autoComplete="email" />
        <FormField icon="lock" label="Mật khẩu" type="password" value={form.password} onChange={(value) => setForm({ ...form, password: value })} autoComplete="current-password" />
        <button className="btn btn-primary btn-lg w-100" disabled={busy}>{busy ? 'Đang đăng nhập…' : 'Đăng nhập'}</button>
      </form>
      <p className="text-center text-secondary mt-4 mb-0">Chưa có tài khoản? <Link to="/register">Đăng ký ngay</Link></p>
    </AuthShell>
  )
}
