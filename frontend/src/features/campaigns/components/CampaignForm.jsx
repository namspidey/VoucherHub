export default function CampaignForm({ form, setForm, draft, busy, editing, onSubmit }) {
  return (
          <form onSubmit={onSubmit}>
            <div className="row g-3">
              <div className="col-12"><label className="form-label">Tên campaign *</label><input className="form-control" required maxLength="200" disabled={!draft} value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} /></div>
              <div className="col-12"><label className="form-label">Mô tả</label><textarea className="form-control" rows="3" maxLength="2000" disabled={!draft} value={form.description} onChange={(e) => setForm({ ...form, description: e.target.value })} /></div>
              <div className="col-md-5"><label className="form-label">Bắt đầu *</label><input className="form-control" type="datetime-local" required disabled={!draft} value={form.startsAt} onChange={(e) => setForm({ ...form, startsAt: e.target.value })} /></div>
              <div className="col-md-5"><label className="form-label">Kết thúc *</label><input className="form-control" type="datetime-local" required disabled={!draft} value={form.endsAt} onChange={(e) => setForm({ ...form, endsAt: e.target.value })} /></div>
              <div className="col-md-2"><label className="form-label">Quota/user *</label><input className="form-control" type="number" min="1" max="9007199254740991" required disabled={!draft} value={form.maxRedemptionsPerUser} onChange={(e) => setForm({ ...form, maxRedemptionsPerUser: e.target.value })} /></div>
            </div>
            {draft && <button className="btn btn-primary mt-4" disabled={busy}><i className="bi bi-floppy me-2" />{editing ? 'Lưu thay đổi' : 'Tạo bản nháp'}</button>}
          </form>
  )
}
