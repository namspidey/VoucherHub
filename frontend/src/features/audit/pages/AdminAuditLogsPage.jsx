import { useUrlState } from '../../../shared/lib/useUrlState.js'
import { useState } from 'react'
import AdminShell from '../../../shared/ui/AdminShell.jsx'
import AlertMessage from '../../../shared/ui/AlertMessage.jsx'
import LoadingState from '../../../shared/ui/states/LoadingState.jsx'
import Pagination from '../../../shared/ui/Pagination.jsx'
import { useList } from '../queries.js'
import { formatDateTime, shortId } from '../../../shared/lib/format.js'

export default function AdminAuditLogsPage() {
  const [filters, setFilters] = useState({ action: '', entityType: '', actorUserId: '' })
  const [query, setQuery] = useState(filters)
  const [page, setPage] = useUrlState('page', 1)
  const { data, error } = useList({ page, pageSize: 20, ...query })

  return <AdminShell title="Audit log" description="Theo dõi ai đã thực hiện hành động gì trên hệ thống.">
    <form className="card border-0 shadow-sm p-3 mb-4" onSubmit={(e) => { e.preventDefault(); setPage(1); setQuery({ ...filters }) }}>
      <div className="row g-2">
        <div className="col-lg"><input className="form-control" placeholder="Action, ví dụ CAMPAIGN_CREATED" maxLength="100" value={filters.action} onChange={(e) => setFilters({ ...filters, action: e.target.value })} /></div>
        <div className="col-lg"><input className="form-control" placeholder="Entity type" maxLength="100" value={filters.entityType} onChange={(e) => setFilters({ ...filters, entityType: e.target.value })} /></div>
        <div className="col-lg"><input className="form-control" placeholder="Actor user ID" value={filters.actorUserId} onChange={(e) => setFilters({ ...filters, actorUserId: e.target.value })} /></div>
        <div className="col-auto"><button className="btn btn-dark"><i className="bi bi-search" /></button></div>
      </div>
    </form>
    <AlertMessage message={error} />
    {!data && !error ? <LoadingState /> : data && <div className="card border-0 shadow-sm"><div className="table-responsive"><table className="table align-middle mb-0"><thead><tr><th>Hành động</th><th>Đối tượng</th><th>Actor</th><th>Chi tiết</th><th>Thời gian</th></tr></thead><tbody>{data.items.map((item) => <tr key={item.id}><td><span className="badge text-bg-light">{item.action}</span></td><td>{item.entityType}<div><code title={item.entityId}>{shortId(item.entityId)}</code></div></td><td><code title={item.actorUserId}>{shortId(item.actorUserId)}</code></td><td><small className="audit-details">{item.details || '—'}</small></td><td><small>{formatDateTime(item.createdAt)}</small></td></tr>)}</tbody></table></div><div className="card-body pt-0"><Pagination page={data.pageNumber} pageSize={data.pageSize} totalCount={data.totalCount} onChange={setPage} /></div></div>}
  </AdminShell>
}
