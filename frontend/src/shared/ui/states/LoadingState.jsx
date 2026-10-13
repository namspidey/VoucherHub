export default function LoadingState({ fullscreen = false, label = 'Đang tải dữ liệu…' }) {
  return (
    <div className={`d-flex flex-column align-items-center justify-content-center gap-3 ${fullscreen ? 'page-loading' : 'py-5'}`}>
      <div className="spinner-border text-primary" role="status" />
      <span className="text-secondary">{label}</span>
    </div>
  )
}
