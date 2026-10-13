import { useUrlState } from '../../../shared/lib/useUrlState.js'
import { useState } from 'react'
import { Link } from 'react-router-dom'
import AdminShell from '../../../shared/ui/AdminShell.jsx'
import AlertMessage from '../../../shared/ui/AlertMessage.jsx'
import EmptyState from '../../../shared/ui/states/EmptyState.jsx'
import LoadingState from '../../../shared/ui/states/LoadingState.jsx'
import Pagination from '../../../shared/ui/Pagination.jsx'
import { useAdminCampaigns } from '../queries.js'
import { badgeClass, formatDateTime, statusLabel } from '../../../shared/lib/format.js'

export default function AdminCampaignsPage() {
  const [filters, setFilters] = useState({ search: '', status: '' })
  const [query, setQuery] = useState({ search: '', status: '' })
  const [page, setPage] = useUrlState('page', 1)
  const { data, error } = useAdminCampaigns({ page, pageSize: 15, ...query })


  const submit = (event) => { event.preventDefault(); setPage(1); setQuery(filters) }
  return (
    <AdminShell title="Campaign" description="Tạo, cấu hình offer và quản lý trạng thái campaign." actions={<Link className="btn btn-primary" to="/admin/campaigns/new"><i className="bi bi-plus-lg me-2" />Tạo campaign</Link>}>
      <form className="card border-0 shadow-sm p-3 mb-4" onSubmit={submit}>
        <div className="row g-2">
          <div className="col-md"><input className="form-control" placeholder="Tìm theo tên…" value={filters.search} onChange={(e) => setFilters({ ...filters, search: e.target.value })} /></div>
          <div className="col-md-3"><select className="form-select" value={filters.status} onChange={(e) => setFilters({ ...filters, status: e.target.value })}><option value="">Mọi trạng thái</option><option>DRAFT</option><option>PUBLISHED</option><option>CANCELLED</option></select></div>
          <div className="col-md-auto"><button className="btn btn-dark w-100"><i className="bi bi-search me-2" />Lọc</button></div>
        </div>
      </form>
      <AlertMessage message={error} />
      {!data && !error ? <LoadingState /> : data?.items?.length ? (
        <div className="card border-0 shadow-sm">
          <div className="table-responsive"><table className="table align-middle mb-0">
            <thead><tr><th>Tên campaign</th><th>Thời gian</th><th>Quota</th><th>Trạng thái</th><th /></tr></thead>
            <tbody>{data.items.map((item) => <tr key={item.id}><td><strong>{item.name}</strong><div className="small text-secondary text-truncate table-description">{item.description}</div></td><td><small>{formatDateTime(item.startsAt)}<br />→ {formatDateTime(item.endsAt)}</small></td><td>{item.maxRedemptionsPerUser}/user</td><td><span className={`badge ${badgeClass(item.status)}`}>{statusLabel[item.status]}</span></td><td className="text-end"><Link className="btn btn-sm btn-outline-primary" to={`/admin/campaigns/${item.id}`}><i className="bi bi-pencil-square me-1" />Quản lý</Link></td></tr>)}</tbody>
          </table></div>
          <div className="card-body pt-0"><Pagination page={data.pageNumber} pageSize={data.pageSize} totalCount={data.totalCount} onChange={setPage} /></div>
        </div>
      ) : !error && <EmptyState icon="megaphone" title="Chưa có campaign" message="Tạo campaign đầu tiên để bắt đầu." />}
    </AdminShell>
  )
}
