import { useUrlState } from '../../../shared/lib/useUrlState.js'
import { useState } from 'react'
import AdminShell from '../../../shared/ui/AdminShell.jsx'
import AlertMessage from '../../../shared/ui/AlertMessage.jsx'
import LoadingState from '../../../shared/ui/states/LoadingState.jsx'
import Pagination from '../../../shared/ui/Pagination.jsx'
import { useList } from '../queries.js'
import { usersApi } from '../api.js'
import { useAdminHistory } from '../../wallet/queries.js'
import { walletApi } from '../../wallet/api.js'
import { useQueryClient } from '@tanstack/react-query'
import Dialog from '../../../shared/ui/Dialog.jsx'
import { badgeClass, formatDateTime, formatPoints } from '../../../shared/lib/format.js'

export default function AdminUsersPage() {
  const [filters, setFilters] = useState({ email: '', status: '' })
  const [query, setQuery] = useState({ email: '', status: '' })
  const [page, setPage] = useUrlState('page', 1)
  const { data, refetch, error: queryError } = useList({ page, pageSize: 15, ...query })
  const [selectedUserId, setSelectedUserId] = useState(null)
  const selected = data?.items.find(item => item.id === selectedUserId) || null
  const { data: history } = useAdminHistory(selectedUserId)
  const client = useQueryClient()
  const [blockOpen, setBlockOpen] = useState(false)
  const [blockReason, setBlockReason] = useState('')
  const [grant, setGrant] = useState({ amount: 100, reason: '' })
  const [error, setError] = useState('')
  const [success, setSuccess] = useState('')
  const [busy, setBusy] = useState(false)

  const selectUser = (user) => { setSelectedUserId(user.id); setError('') }

  const grantPoints = async (event) => {
    event.preventDefault(); setBusy(true); setError('')
    try {
      await walletApi.grant(selected.id, { amount: Number(grant.amount), reason: grant.reason })
      setSuccess('Đã cộng điểm và ghi point transaction.')
      setGrant({ amount: 100, reason: '' })
      await refetch()
      client.invalidateQueries({ queryKey: ['wallet'] })
    } catch (err) { setError(err.message) } finally { setBusy(false) }
  }

  const blockUser = async () => {
    const reason = blockReason.trim()
    if (!reason) return
    setBusy(true); setError('')
    try {
      await usersApi.block(selected.id, { reason })
      setSuccess('Tài khoản đã bị khóa.')
      setBlockOpen(false)
      setSelectedUserId(null)
      await refetch()
    } catch (err) { setError(err.message) } finally { setBusy(false) }
  }

  return (
    <AdminShell title="Người dùng" description="Tìm kiếm, cấp điểm, xem lịch sử và khóa tài khoản.">
      <Dialog open={blockOpen} title="Khóa tài khoản" onClose={() => setBlockOpen(false)}>
        <form onSubmit={e => { e.preventDefault(); blockUser() }}>
          <label htmlFor="block-reason" className="form-label">Lý do</label>
          <textarea id="block-reason" className="form-control mb-3" required maxLength="500" value={blockReason} onChange={e => setBlockReason(e.target.value)} />
          <button className="btn btn-danger w-100" disabled={busy}>Xác nhận khóa</button>
        </form>
      </Dialog>
      <AlertMessage message={error || queryError} onClose={() => setError('')} />
      <AlertMessage type="success" message={success} onClose={() => setSuccess('')} />
      <form className="card border-0 shadow-sm p-3 mb-4" onSubmit={(e) => { e.preventDefault(); setPage(1); setQuery(filters) }}>
        <div className="row g-2">
          <div className="col"><input type="search" aria-label="Tìm email" className="form-control" placeholder="Tìm theo email…" value={filters.email} onChange={(e) => setFilters({ ...filters, email: e.target.value })} /></div>
          <div className="col-md-3"><select className="form-select" value={filters.status} onChange={(e) => setFilters({ ...filters, status: e.target.value })}><option value="">Mọi trạng thái</option><option>ACTIVE</option><option>BLOCKED</option></select></div>
          <div className="col-auto"><button className="btn btn-dark"><i className="bi bi-search" /></button></div>
        </div>
      </form>
      {!data && !error && !queryError ? <LoadingState /> : data && (
        <div className="row g-4">
          <div className={selected ? 'col-xl-7' : 'col-12'}>
            <div className="card border-0 shadow-sm">
              <div className="table-responsive"><table className="table table-hover align-middle mb-0">
                <thead><tr><th>Người dùng</th><th>Vai trò</th><th>Trạng thái</th><th className="text-end">Số dư</th></tr></thead>
                <tbody>{data.items.map((user) => <tr key={user.id} role="button" tabIndex={0} onKeyDown={e => { if (e.key === 'Enter' || e.key === ' ') { e.preventDefault(); selectUser(user) } }} className={selected?.id === user.id ? 'table-primary' : ''} onClick={() => selectUser(user)}>
                  <td><strong>{user.displayName}</strong><div className="small text-secondary">{user.email}</div></td>
                  <td><span className="badge text-bg-light">{user.role}</span></td>
                  <td><span className={`badge ${badgeClass(user.status)}`}>{user.status}</span></td>
                  <td className="text-end fw-semibold">{formatPoints(user.balance)}</td>
                </tr>)}</tbody>
              </table></div>
              <div className="card-body pt-0"><Pagination page={data.pageNumber} pageSize={data.pageSize} totalCount={data.totalCount} onChange={setPage} /></div>
            </div>
          </div>
          {selected && <div className="col-xl-5"><UserPanel user={selected} history={history} grant={grant} setGrant={setGrant} grantPoints={grantPoints} blockUser={() => setBlockOpen(true)} busy={busy} close={() => setSelectedUserId(null)} /></div>}
        </div>
      )}
    </AdminShell>
  )
}

function UserPanel({ user, history, grant, setGrant, grantPoints, blockUser, busy, close }) {
  return <div className="card border-0 shadow-sm sticky-xl-top user-panel">
    <div className="card-header bg-white p-3 d-flex justify-content-between"><div><strong>{user.displayName}</strong><div className="small text-secondary">{user.email}</div></div><button className="btn-close" onClick={close} /></div>
    <div className="card-body">
      <div className="balance-mini mb-4"><span>Số dư hiện tại</span><strong>{formatPoints(user.balance)} điểm</strong></div>
      {user.status === 'ACTIVE' && <form onSubmit={grantPoints} className="mb-4"><h3 className="h6">Cấp điểm</h3><div className="input-group mb-2"><input className="form-control" type="number" min="1" max="9007199254740991" required value={grant.amount} onChange={(e) => setGrant({ ...grant, amount: e.target.value })} /><span className="input-group-text">điểm</span></div><textarea className="form-control mb-2" required maxLength="500" placeholder="Lý do bắt buộc" value={grant.reason} onChange={(e) => setGrant({ ...grant, reason: e.target.value })} /><button className="btn btn-primary w-100" disabled={busy}>Xác nhận cấp điểm</button></form>}
      <h3 className="h6">Giao dịch gần đây</h3>
      {!history ? <div className="spinner-border spinner-border-sm" /> : history.items.length ? history.items.map((item) => <div className="d-flex justify-content-between border-bottom py-2 small" key={item.id}><div>{item.reason}<div className="text-secondary">{formatDateTime(item.createdAt)}</div></div><strong className={item.amount > 0 ? 'text-success' : 'text-danger'}>{item.amount > 0 ? '+' : ''}{formatPoints(item.amount)}</strong></div>) : <p className="text-secondary small">Chưa có giao dịch.</p>}
      {user.status === 'ACTIVE' && user.role !== 'ADMIN' && <button className="btn btn-outline-danger w-100 mt-4" disabled={busy} onClick={blockUser}><i className="bi bi-person-lock me-2" />Khóa tài khoản</button>}
    </div>
  </div>
}
