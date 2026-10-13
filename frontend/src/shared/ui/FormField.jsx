import { useId } from 'react'

export default function FormField({ icon, label, value, onChange, ...props }) {
  const id = useId()
  return (
    <div className="mb-3">
      <label htmlFor={id} className="form-label fw-semibold">{label}</label>
      <div className="input-group input-group-lg">
        <span className="input-group-text bg-white"><i className={`bi bi-${icon}`} /></span>
        <input id={id} className="form-control" value={value} onChange={(event) => onChange(event.target.value)} required {...props} />
      </div>
    </div>
  )
}
