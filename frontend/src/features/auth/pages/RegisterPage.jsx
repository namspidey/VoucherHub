import { useState } from 'react'
import { Link } from 'react-router-dom'
import AlertMessage from '../../../shared/ui/AlertMessage.jsx'
import { authApi } from '../api.js'
import AuthShell from '../components/AuthShell.jsx'
import FormField from '../../../shared/ui/FormField.jsx'

export default function RegisterPage() {
  const [form, setForm] = useState({ displayName: '', email: '', password: '' })
  const [error, setError] = useState('')
  const [success, setSuccess] = useState('')
  const [busy, setBusy] = useState(false)

  const submit = async (event) => {
    event.preventDefault()
    setBusy(true)
    setError('')
    try {
      await authApi.register(form)
      setSuccess('Yêu cầu đã được tiếp nhận. Hãy mở email xác thực (Mailpit khi chạy local).')
    } catch (err) { setError(err.message) } finally { setBusy(false) }
  }

  const resend = async () => {
    setBusy(true)
    setError('')
    try {
      await authApi.resend(form.email)
      setSuccess('Đã tiếp nhận yêu cầu gửi lại. Hãy kiểm tra email mới nhất sau thời gian cooldown.')
    } catch (err) { setError(err.message) } finally { setBusy(false) }
  }

  return (
    <AuthShell title="Tạo tài khoản" subtitle="Bắt đầu với số dư 0 điểm và nhận ưu đãi từ VoucherHub.">
      <AlertMessage message={error} />
      <AlertMessage type="success" message={success} />
      {success && <button className="btn btn-outline-primary w-100" type="button" disabled={busy} onClick={resend}><i className="bi bi-envelope-arrow-up me-2" />Gửi lại email xác thực</button>}
      {!success && (
        <form onSubmit={submit}>
          <FormField icon="person" label="Tên hiển thị" value={form.displayName} onChange={(value) => setForm({ ...form, displayName: value })} maxLength="100" />
          <FormField icon="envelope" label="Email" type="email" value={form.email} onChange={(value) => setForm({ ...form, email: value })} maxLength="255" />
          <FormField icon="lock" label="Mật khẩu" type="password" value={form.password} onChange={(value) => setForm({ ...form, password: value })} minLength="8" maxLength="128" />
          <div className="form-text mb-3">Tối thiểu 8 ký tự. Mật khẩu chỉ được gửi qua HTTPS khi deploy.</div>
          <button className="btn btn-primary btn-lg w-100" disabled={busy}>{busy ? 'Đang gửi…' : 'Đăng ký'}</button>
        </form>
      )}
      <p className="text-center text-secondary mt-4 mb-0">Đã có tài khoản? <Link to="/login">Đăng nhập</Link></p>
    </AuthShell>
  )
}
