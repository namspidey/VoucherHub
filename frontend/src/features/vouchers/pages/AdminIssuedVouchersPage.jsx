import { useUrlState } from '../../../shared/lib/useUrlState.js'
import { useState } from 'react'
import AdminShell from '../../../shared/ui/AdminShell.jsx'
import AlertMessage from '../../../shared/ui/AlertMessage.jsx'
import LoadingState from '../../../shared/ui/states/LoadingState.jsx'
import Pagination from '../../../shared/ui/Pagination.jsx'
import { useIssued } from '../queries.js'
import { formatDateTime, formatPoints, shortId } from '../../../shared/lib/format.js'

export default function AdminIssuedVouchersPage() {
  const [filters, setFilters] = useState({ userId: '', campaignId: '', offerId: '' })
  const [query, setQuery] = useState(filters)
  const [page, setPage] = useUrlState('page', 1)
  const { data, error } = useIssued({ page, pageSize: 20, ...query })

  return <AdminShell title="Voucher đã phát" description="Tra cứu lịch sử phát voucher thành công.">
    <form className="card border-0 shadow-sm p-3 mb-4" onSubmit={(e) => { e.preventDefault(); setPage(1); setQuery({ ...filters }) }}>
      <div className="row g-2">
        <div className="col-lg"><input className="form-control" placeholder="User ID" value={filters.userId} onChange={(e) => setFilters({ ...filters, userId: e.target.value })} /></div>
        <div className="col-lg"><input className="form-control" placeholder="Campaign ID" value={filters.campaignId} onChange={(e) => setFilters({ ...filters, campaignId: e.target.value })} /></div>
        <div className="col-lg"><input className="form-control" placeholder="Offer ID" value={filters.offerId} onChange={(e) => setFilters({ ...filters, offerId: e.target.value })} /></div>
        <div className="col-auto"><button className="btn btn-dark"><i className="bi bi-funnel me-2" />Lọc</button></div>
      </div>
    </form>
    <AlertMessage message={error} />
    {!data && !error ? <LoadingState /> : data && <div className="card border-0 shadow-sm"><div className="table-responsive"><table className="table align-middle mb-0"><thead><tr><th>Voucher ID</th><th>User</th><th>Campaign / Offer</th><th>Điểm</th><th>Thời gian</th></tr></thead><tbody>{data.items.map((item) => <tr key={item.id}><td><code title={item.id}>{shortId(item.id)}</code></td><td><code title={item.userId}>{shortId(item.userId)}</code></td><td><small>Campaign: {shortId(item.campaignId)}<br />Offer: {shortId(item.voucherOfferId)}</small></td><td>{formatPoints(item.pointsSpent)}</td><td><small>{formatDateTime(item.claimedAt)}<br />Hạn: {formatDateTime(item.expiresAt)}</small></td></tr>)}</tbody></table></div><div className="card-body pt-0"><Pagination page={data.pageNumber} pageSize={data.pageSize} totalCount={data.totalCount} onChange={setPage} /></div></div>}
  </AdminShell>
}
