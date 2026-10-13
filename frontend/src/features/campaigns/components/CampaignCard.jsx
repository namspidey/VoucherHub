import { Link } from 'react-router-dom'
import { badgeClass, formatDateTime, formatPoints, phaseLabel } from '../../../shared/lib/format.js'

export default function CampaignCard({ campaign }) {
  const available = campaign.offers.reduce((sum, item) => sum + item.remainingQuantity, 0)
  const minPoints = campaign.offers.length
    ? Math.min(...campaign.offers.map((item) => item.pointsCost))
    : 0

  return (
    <article className="card campaign-card h-100 border-0 shadow-sm">
      <div className="campaign-cover p-4">
        <div className="d-flex justify-content-between align-items-start gap-2">
          <span className={`badge ${badgeClass(campaign.phase)}`}>{phaseLabel[campaign.phase] || campaign.phase}</span>
          <i className="bi bi-stars fs-4 text-warning" />
        </div>
        <h2 className="h4 mt-4 mb-2">{campaign.name}</h2>
        <p className="text-secondary line-clamp-2 mb-0">{campaign.description || 'Cơ hội đổi voucher hấp dẫn bằng điểm.'}</p>
      </div>
      <div className="card-body pt-0">
        <div className="row g-3 small">
          <div className="col-6">
            <span className="text-secondary d-block">Từ</span>
            <strong>{formatPoints(minPoints)} điểm</strong>
          </div>
          <div className="col-6">
            <span className="text-secondary d-block">Còn lại</span>
            <strong>{available} voucher</strong>
          </div>
          <div className="col-12 text-secondary">
            <i className="bi bi-clock me-2" />{formatDateTime(campaign.endsAt)}
          </div>
        </div>
      </div>
      <div className="card-footer bg-transparent border-0 p-3 pt-0">
        <Link to={`/campaigns/${campaign.id}`} className="btn btn-primary w-100">
          Xem ưu đãi <i className="bi bi-arrow-right ms-1" />
        </Link>
      </div>
    </article>
  )
}
