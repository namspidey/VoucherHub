export default function EmptyState({ icon = 'inbox', title, message, action }) {
  return (
    <div className="empty-state text-center p-5">
      <i className={`bi bi-${icon} display-5 text-secondary`} />
      <h3 className="h5 mt-3">{title}</h3>
      <p className="text-secondary mb-3">{message}</p>
      {action}
    </div>
  )
}
