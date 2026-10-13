import { NavLink } from 'react-router-dom'

const links = [
  ['/admin', 'speedometer2', 'Tổng quan', true],
  ['/admin/campaigns', 'megaphone', 'Campaign'],
  ['/admin/users', 'people', 'Người dùng'],
  ['/admin/issued-vouchers', 'ticket-detailed', 'Voucher đã phát'],
  ['/admin/audit-logs', 'journal-text', 'Audit log'],
]

export default function AdminShell({ title, description, actions, children }) {
  return (
    <div className="container-fluid admin-area">
      <div className="row">
        <aside className="col-lg-2 admin-sidebar p-3">
          <div className="small text-uppercase text-secondary fw-semibold px-3 mb-2">Quản trị hệ thống</div>
          <nav className="nav nav-pills flex-lg-column gap-1">
            {links.map(([to, icon, label, end]) => (
              <NavLink key={to} end={end} className="nav-link" to={to}>
                <i className={`bi bi-${icon} me-2`} />{label}
              </NavLink>
            ))}
          </nav>
        </aside>
        <section className="col-lg-10 p-4 p-xl-5">
          <div className="d-flex flex-column flex-md-row justify-content-between align-items-md-start gap-3 mb-4">
            <div><h1 className="h2 fw-bold mb-1">{title}</h1><p className="text-secondary mb-0">{description}</p></div>
            {actions && <div>{actions}</div>}
          </div>
          {children}
        </section>
      </div>
    </div>
  )
}
