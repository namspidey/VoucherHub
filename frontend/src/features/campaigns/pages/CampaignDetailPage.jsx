import { useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import useAuth from '../../auth/useAuth.js'
import { getScope } from '../../auth/session.js'
import AlertMessage from '../../../shared/ui/AlertMessage.jsx'
import EmptyState from '../../../shared/ui/states/EmptyState.jsx'
import LoadingState from '../../../shared/ui/states/LoadingState.jsx'
import { useCampaign } from '../queries.js'
import { useRedeemVoucher } from '../../redemption/useRedeemVoucher.js'
import Dialog from '../../../shared/ui/Dialog.jsx'
import { badgeClass, formatDateTime, formatPoints, phaseLabel } from '../../../shared/lib/format.js'

export default function CampaignDetailPage() {
  const { id } = useParams()
  useAuth() // Re-render on session changes; discard private dialog/result state.
  return <CampaignDetailContent key={`${id}:${getScope()}`} />
}

function CampaignDetailContent() {
  const { id } = useParams()
  const { user } = useAuth()
  const redemption = useRedeemVoucher(user?.id)
  const [selectedOffer, setSelectedOffer] = useState(null)
  const navigate = useNavigate()
  const { data: campaign, error, setError } = useCampaign(id)
  const [message, setMessage] = useState('')
  const [busy, setBusy] = useState('')

  const redeem = async (offer) => {
    if (!user) {
      navigate('/login', { state: { from: { pathname: `/campaigns/${id}` } } })
      return
    }
    setBusy(offer.id)
    setError('')
    setMessage('')
    try {
      const voucher = await redemption.mutateAsync(offer.id)
      setMessage(`Đổi thành công! Mã voucher của bạn: ${voucher.code}`)
      setSelectedOffer(null)
    } catch (err) {
      setError(err.message)
    } finally {
      setBusy('')
    }
  }

  if (!campaign && !error) return <LoadingState fullscreen />

  return (
    <div className="container py-5">
      <Dialog open={Boolean(selectedOffer)} title="Xác nhận đổi voucher" onClose={() => { if (!busy) setSelectedOffer(null) }}>
        {selectedOffer && <><p>Đổi “{selectedOffer.name}” với {formatPoints(selectedOffer.pointsCost)} điểm?</p>
          {redemption.hasPending(selectedOffer.id) && <p className="text-warning">Lượt trước chưa rõ kết quả. Thử lại sẽ giữ cùng mã yêu cầu.</p>}
          <AlertMessage message={redemption.error?.message} />
          <button className="btn btn-primary w-100" disabled={Boolean(busy)} onClick={() => redeem(selectedOffer)}>{busy ? 'Đang xử lý…' : 'Xác nhận'}</button>
        </>}
      </Dialog>
      <Link to="/" className="text-decoration-none small"><i className="bi bi-arrow-left me-1" />Quay lại danh sách</Link>
      <AlertMessage message={error} />
      {campaign && (
        <>
          <section className="detail-hero rounded-4 p-4 p-lg-5 my-4">
            <div className="row align-items-center g-4">
              <div className="col-lg-8">
                <span className={`badge ${badgeClass(campaign.phase)}`}>{phaseLabel[campaign.phase]}</span>
                <h1 className="display-5 fw-bold mt-3">{campaign.name}</h1>
                <p className="lead mb-0 opacity-75">{campaign.description || 'Chọn voucher phù hợp và đổi bằng điểm của bạn.'}</p>
              </div>
              <div className="col-lg-4">
                <div className="bg-white bg-opacity-10 rounded-4 p-4">
                  <div className="mb-3"><i className="bi bi-calendar-event me-2" />{formatDateTime(campaign.startsAt)}</div>
                  <div className="mb-3"><i className="bi bi-calendar-check me-2" />{formatDateTime(campaign.endsAt)}</div>
                  <div><i className="bi bi-person-check me-2" />Tối đa {campaign.maxRedemptionsPerUser} lượt/người</div>
                </div>
              </div>
            </div>
          </section>
          <AlertMessage type="success" message={message} onClose={() => setMessage('')} />
          <div className="d-flex justify-content-between align-items-end mb-3">
            <div><span className="text-primary fw-semibold">VOUCHER OFFERS</span><h2 className="h3 fw-bold mb-0">Ưu đãi trong campaign</h2></div>
            {user && <span className="badge rounded-pill text-bg-light fs-6">{formatPoints(user.balance)} điểm</span>}
          </div>
          {campaign.offers.length ? (
            <div className="row g-4">
              {campaign.offers.map((offer) => {
                const canRedeem = redemption.hasPending(offer.id) || (campaign.phase === 'ONGOING' && offer.remainingQuantity > 0)
                return (
                  <div className="col-md-6" key={offer.id}>
                    <article className="card border-0 shadow-sm h-100 voucher-offer-card">
                      <div className="card-body p-4">
                        <div className="d-flex justify-content-between gap-3">
                          <div><span className="badge text-bg-light mb-2">Còn {offer.remainingQuantity}/{offer.totalQuantity}</span><h3 className="h4">{offer.name}</h3></div>
                          <span className="points-chip">{formatPoints(offer.pointsCost)}<small> điểm</small></span>
                        </div>
                        <p className="text-secondary">{offer.description || 'Voucher ưu đãi dành riêng cho thành viên VoucherHub.'}</p>
                        <div className="progress mb-3" role="progressbar">
                          <div className="progress-bar" style={{ width: `${(offer.remainingQuantity / offer.totalQuantity) * 100}%` }} />
                        </div>
                        <div className="small text-secondary mb-4"><i className="bi bi-hourglass-split me-2" />Hạn dùng: {formatDateTime(offer.expiresAt)}</div>
                        <button className="btn btn-primary w-100" disabled={!canRedeem || busy === offer.id} onClick={() => setSelectedOffer(offer)}>
                          {busy === offer.id ? <><span className="spinner-border spinner-border-sm me-2" />Đang xử lý</> : canRedeem ? <><i className="bi bi-lightning-charge-fill me-2" />Đổi / thử lại</> : 'Chưa thể đổi'}
                        </button>
                      </div>
                    </article>
                  </div>
                )
              })}
            </div>
          ) : <EmptyState title="Chưa có voucher" message="Campaign này chưa có offer khả dụng." />}
        </>
      )}
    </div>
  )
}
