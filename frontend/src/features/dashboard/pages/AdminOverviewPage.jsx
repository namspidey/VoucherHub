import { Link } from 'react-router-dom'
import AdminShell from '../../../shared/ui/AdminShell.jsx'
import AlertMessage from '../../../shared/ui/AlertMessage.jsx'
import LoadingState from '../../../shared/ui/states/LoadingState.jsx'
import { useOverview } from '../queries.js'
import { formatDateTime } from '../../../shared/lib/format.js'

export default function AdminOverviewPage() {
  const { data: state, error } = useOverview()

  return (
    <AdminShell title="Tổng quan" description="Theo dõi nhanh hoạt động của VoucherHub.">
      <AlertMessage message={error} />
      {!state && !error ? <LoadingState /> : state && (
        <>
          <div className="row g-3 mb-4">
            <Stat icon="megaphone" label="Campaign" value={state.campaigns.totalCount} color="primary" />
            <Stat icon="people" label="Người dùng" value={state.users.totalCount} color="success" />
            <Stat icon="ticket-perforated" label="Voucher đã phát" value={state.vouchers.totalCount} color="warning" />
            <Stat icon="journal-check" label="Audit events" value={state.audits.totalCount} color="info" />
          </div>
          <div className="row g-4">
            <div className="col-xl-7">
              <div className="card border-0 shadow-sm h-100">
                <div className="card-header bg-white d-flex justify-content-between align-items-center"><strong>Campaign gần đây</strong><Link to="/admin/campaigns">Xem tất cả</Link></div>
                <div className="list-group list-group-flush">{state.campaigns.items.map((item) => (
                  <Link to={`/admin/campaigns/${item.id}`} className="list-group-item list-group-item-action d-flex justify-content-between align-items-center py-3" key={item.id}>
                    <div><strong>{item.name}</strong><div className="small text-secondary">{formatDateTime(item.startsAt)}</div></div>
                    <span className="badge text-bg-light">{item.status}</span>
                  </Link>
                ))}</div>
              </div>
            </div>
            <div className="col-xl-5">
              <div className="card border-0 shadow-sm h-100">
                <div className="card-header bg-white"><strong>Audit mới nhất</strong></div>
                <div className="card-body">
                  {state.audits.items.map((item) => <div className="d-flex gap-3 pb-3 mb-3 border-bottom" key={item.id}><i className="bi bi-activity text-primary" /><div><strong className="small">{item.action}</strong><div className="small text-secondary">{item.entityType} · {formatDateTime(item.createdAt)}</div></div></div>)}
                </div>
              </div>
            </div>
          </div>
        </>
      )}
    </AdminShell>
  )
}

function Stat({ icon, label, value, color }) {
  return <div className="col-sm-6 col-xl-3"><div className="card border-0 shadow-sm h-100"><div className="card-body d-flex align-items-center gap-3"><span className={`stat-icon bg-${color}-subtle text-${color}`}><i className={`bi bi-${icon}`} /></span><div><span className="text-secondary small">{label}</span><div className="h3 fw-bold mb-0">{value}</div></div></div></div></div>
}
