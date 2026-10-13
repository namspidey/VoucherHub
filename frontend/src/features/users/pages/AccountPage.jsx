import { Link } from 'react-router-dom'
import useAuth from '../../auth/useAuth.js'
import { formatDateTime, formatPoints } from '../../../shared/lib/format.js'

export default function AccountPage() {
  const { user } = useAuth()
  return (
    <div className="container py-5">
      <div className="row g-4">
        <div className="col-lg-4">
          <div className="card border-0 shadow-sm text-center p-4 h-100">
            <div className="avatar-circle mx-auto">{user.displayName?.charAt(0).toUpperCase()}</div>
            <h1 className="h4 mt-3 mb-1">{user.displayName}</h1>
            <p className="text-secondary">{user.email}</p>
            <span className="badge text-bg-primary align-self-center">{user.role}</span>
          </div>
        </div>
        <div className="col-lg-8">
          <div className="balance-card p-4 p-md-5 mb-4">
            <span className="opacity-75">Số dư khả dụng</span>
            <div className="display-4 fw-bold my-2">{formatPoints(user.balance)} <small className="fs-4">điểm</small></div>
            <Link to="/point-history" className="btn btn-light">Xem lịch sử <i className="bi bi-arrow-right ms-1" /></Link>
          </div>
          <div className="card border-0 shadow-sm">
            <div className="card-body p-4">
              <h2 className="h5 mb-4">Thông tin tài khoản</h2>
              <InfoRow icon="person-badge" label="Mã người dùng" value={user.id} />
              <InfoRow icon="shield-check" label="Trạng thái" value={user.status} />
              <InfoRow icon="envelope-check" label="Xác thực email" value={formatDateTime(user.emailVerifiedAt)} />
            </div>
          </div>
        </div>
      </div>
    </div>
  )
}

function InfoRow({ icon, label, value }) {
  return <div className="d-flex gap-3 py-3 border-bottom"><i className={`bi bi-${icon} text-primary fs-5`} /><div><span className="text-secondary small d-block">{label}</span><strong className="text-break">{value}</strong></div></div>
}
