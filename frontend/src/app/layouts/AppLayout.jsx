import { useState } from 'react'
import { NavLink, Outlet } from 'react-router-dom'
import useAuth from '../../features/auth/useAuth.js'

const navClass = ({ isActive }) => `nav-link px-3 ${isActive ? 'active fw-semibold' : ''}`

export default function AppLayout() {
  const { user, loading, logout } = useAuth()
  const [menuOpen, setMenuOpen] = useState(false)

  return (
    <div className="app-shell">
      <nav className="navbar navbar-expand-lg bg-white border-bottom sticky-top">
        <div className="container">
          <NavLink className="navbar-brand d-flex align-items-center gap-2 fw-bold" to="/">
            <span className="brand-mark"><i className="bi bi-ticket-perforated-fill" /></span>
            VoucherHub
          </NavLink>
          <button className="navbar-toggler" type="button" onClick={() => setMenuOpen(!menuOpen)} aria-expanded={menuOpen} aria-controls="mainNav" aria-label="Mở menu">
            <span className="navbar-toggler-icon" />
          </button>
          <div className={`collapse navbar-collapse ${menuOpen ? 'show' : ''}`} id="mainNav">
            <div className="navbar-nav me-auto ms-lg-4">
              <NavLink className={navClass} to="/">Khám phá</NavLink>
              {user && <NavLink className={navClass} to="/my-vouchers">Voucher của tôi</NavLink>}
              {user && <NavLink className={navClass} to="/point-history">Lịch sử điểm</NavLink>}
              {user?.role === 'ADMIN' && <NavLink className={navClass} to="/admin">Quản trị</NavLink>}
            </div>
            <div className="d-flex align-items-center gap-2 py-2 py-lg-0">
              {loading ? (
                <span className="spinner-border spinner-border-sm text-primary" />
              ) : user ? (
                <>
                  <NavLink className="btn btn-light d-flex align-items-center gap-2" to="/account">
                    <i className="bi bi-person-circle" />
                    <span className="d-none d-md-inline">{user.displayName}</span>
                    <span className="badge text-bg-warning">{user.balance} điểm</span>
                  </NavLink>
                  <button className="btn btn-outline-secondary" type="button" onClick={logout} title="Đăng xuất">
                    <i className="bi bi-box-arrow-right" />
                  </button>
                </>
              ) : (
                <>
                  <NavLink className="btn btn-link text-decoration-none" to="/login">Đăng nhập</NavLink>
                  <NavLink className="btn btn-primary" to="/register">Tạo tài khoản</NavLink>
                </>
              )}
            </div>
          </div>
        </div>
      </nav>

      <main><Outlet /></main>

      <footer className="border-top bg-white mt-auto">
        <div className="container py-4 d-flex flex-column flex-md-row justify-content-between gap-2 text-secondary small">
          <span>© 2026 VoucherHub — Đổi điểm, nhận ưu đãi.</span>
          <span><i className="bi bi-shield-check me-1" />Giao dịch được bảo vệ bởi database transaction</span>
        </div>
      </footer>
    </div>
  )
}
