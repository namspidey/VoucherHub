import { useState } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { authApi } from '../api.js'
import AuthShell from '../components/AuthShell.jsx'
import AlertMessage from '../../../shared/ui/AlertMessage.jsx'
export default function VerifyEmailPage() {
  const [params, setParams] = useSearchParams()
  const [token] = useState(() => params.get('token') || '')
  const [state, setState] = useState({ busy: false, ok: false, message: '' })
  const [email, setEmail] = useState('')
  const [resendMessage, setResendMessage] = useState('')
  const verify = async () => {
    setState({ busy: true, ok: false, message: '' })
    try {
      await authApi.verify(token)
      setState({ busy: false, ok: true, message: 'Email đã xác thực thành công.' })
      setParams({}, { replace: true })
    } catch (err) { setState({ busy: false, ok: false, message: err.message }) }
  }
  const resend = async (event) => {
    event.preventDefault()
    try { await authApi.resend(email); setResendMessage('Đã tiếp nhận yêu cầu. Hãy kiểm tra email mới nhất.') }
    catch (err) { setResendMessage(err.message) }
  }
  return <AuthShell title="Xác thực email" subtitle="Xác nhận để hoàn tất đăng ký tài khoản.">
    {state.message && <AlertMessage type={state.ok ? 'success' : 'danger'} message={state.message} />}
    {state.ok ? <Link to="/login" className="btn btn-primary w-100">Đăng nhập</Link> : <>
      <button className="btn btn-primary w-100" type="button" disabled={!token || state.busy} onClick={verify}>
        {state.busy ? 'Đang xác thực…' : 'Xác nhận email'}
      </button>
      {!token && <p className="text-danger mt-2">Link không có token xác thực.</p>}
      <hr />
      <form onSubmit={resend}>
        <label htmlFor="resend-email" className="form-label">Gửi lại email xác thực</label>
        <input id="resend-email" className="form-control mb-2" type="email" required value={email} onChange={e => setEmail(e.target.value)} />
        <button className="btn btn-outline-primary w-100">Gửi lại</button>
        {resendMessage && <p className="small mt-2" role="status">{resendMessage}</p>}
      </form>
    </>}
  </AuthShell>
}
