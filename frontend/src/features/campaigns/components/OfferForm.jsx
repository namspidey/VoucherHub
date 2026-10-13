export default function OfferForm({ offerForm, setOfferForm, editingOfferId, busy, onSubmit }) {
  return (
              <form onSubmit={onSubmit}>
                <h3 className="h6 mb-3">{editingOfferId ? 'Sửa offer' : 'Offer mới'}</h3>
                <div className="row g-3">
                  <div className="col-md-6"><label className="form-label">Tên offer *</label><input className="form-control" required maxLength="200" value={offerForm.name} onChange={(e) => setOfferForm({ ...offerForm, name: e.target.value })} /></div>
                  <div className="col-md-3"><label className="form-label">Giá điểm *</label><input className="form-control" type="number" min="1" max="9007199254740991" required value={offerForm.pointsCost} onChange={(e) => setOfferForm({ ...offerForm, pointsCost: e.target.value })} /></div>
                  <div className="col-md-3"><label className="form-label">Số lượng *</label><input className="form-control" type="number" min="1" max="9007199254740991" required value={offerForm.totalQuantity} onChange={(e) => setOfferForm({ ...offerForm, totalQuantity: e.target.value })} /></div>
                  <div className="col-md-8"><label className="form-label">Mô tả</label><input className="form-control" maxLength="2000" value={offerForm.description} onChange={(e) => setOfferForm({ ...offerForm, description: e.target.value })} /></div>
                  <div className="col-md-4"><label className="form-label">Hạn voucher *</label><input className="form-control" type="datetime-local" required value={offerForm.expiresAt} onChange={(e) => setOfferForm({ ...offerForm, expiresAt: e.target.value })} /></div>
                </div>
                <div className="mt-3 d-flex gap-2"><button className="btn btn-primary" disabled={busy}>Lưu offer</button><button type="button" className="btn btn-light" onClick={() => setOfferForm(null)}>Đóng</button></div>
              </form>
  )
}
