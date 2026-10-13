import { Link } from 'react-router-dom'

export default function NotFoundPage() {
  return (
    <div className="container text-center py-5 my-5">
      <div className="display-1 fw-bold text-primary">404</div>
      <h1 className="h3">Không tìm thấy trang</h1>
      <p className="text-secondary">Đường dẫn có thể đã thay đổi hoặc không tồn tại.</p>
      <Link className="btn btn-primary" to="/"><i className="bi bi-house me-2" />Về trang chủ</Link>
    </div>
  )
}
