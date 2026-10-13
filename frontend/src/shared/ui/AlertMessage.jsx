export default function AlertMessage({ type = 'danger', message, onClose }) {
  if (!message) return null
  return (
    <div className={`alert alert-${type} d-flex align-items-center gap-2`} role="alert">
      <i className={`bi bi-${type === 'success' ? 'check-circle' : 'exclamation-circle'}-fill`} />
      <div className="flex-grow-1">{message}</div>
      {onClose && <button type="button" className="btn-close" onClick={onClose} aria-label="Đóng" />}
    </div>
  )
}
