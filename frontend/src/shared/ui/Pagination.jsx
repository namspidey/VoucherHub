export default function Pagination({ page, pageSize, totalCount, onChange }) {
  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize))
  if (totalPages <= 1) return null
  return (
    <nav className="d-flex align-items-center justify-content-between mt-4" aria-label="Phân trang">
      <span className="small text-secondary">Trang {page}/{totalPages} · {totalCount} kết quả</span>
      <div className="btn-group">
        <button className="btn btn-outline-secondary" disabled={page <= 1} onClick={() => onChange(page - 1)}>
          <i className="bi bi-chevron-left" /> Trước
        </button>
        <button className="btn btn-outline-secondary" disabled={page >= totalPages} onClick={() => onChange(page + 1)}>
          Sau <i className="bi bi-chevron-right" />
        </button>
      </div>
    </nav>
  )
}
