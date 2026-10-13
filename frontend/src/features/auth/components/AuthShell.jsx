export default function AuthShell({ title, subtitle, children }) {
  return (
    <div className="auth-page py-5">
      <div className="container">
        <div className="auth-card card border-0 shadow-lg mx-auto">
          <div className="card-body p-4 p-md-5">
            <div className="brand-mark mb-4"><i className="bi bi-ticket-perforated-fill" /></div>
            <h1 className="h2 fw-bold">{title}</h1>
            <p className="text-secondary mb-4">{subtitle}</p>
            {children}
          </div>
        </div>
      </div>
    </div>
  )
}
