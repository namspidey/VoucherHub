import { useEffect, useId, useRef } from 'react'
export default function Dialog({ open, title, onClose, children }) {
  const ref = useRef(null)
  const titleId = useId()
  useEffect(() => {
    const dialog = ref.current
    if (open && !dialog.open) dialog.showModal()
    if (!open && dialog.open) dialog.close()
    return () => { if (dialog.open) dialog.close() }
  }, [open])
  return <dialog ref={ref} className="vh-dialog" aria-labelledby={titleId} onCancel={event => { event.preventDefault(); onClose() }}>
    <div className="d-flex justify-content-between align-items-center mb-3">
      <h2 id={titleId} className="h5 mb-0">{title}</h2>
      <button className="btn-close" type="button" aria-label="Đóng" onClick={onClose} />
    </div>
    {children}
  </dialog>
}
