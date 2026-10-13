import { useUrlState } from '../../../shared/lib/useUrlState.js'
import CampaignCard from '../components/CampaignCard.jsx'
import EmptyState from '../../../shared/ui/states/EmptyState.jsx'
import LoadingState from '../../../shared/ui/states/LoadingState.jsx'
import AlertMessage from '../../../shared/ui/AlertMessage.jsx'
import Pagination from '../../../shared/ui/Pagination.jsx'
import { useCatalog } from '../queries.js'

export default function HomePage() {
  const [phase, setPhase] = useUrlState('phase', '')
  const [page, setPage] = useUrlState('page', 1)
  const { data, setData, error } = useCatalog({ page, pageSize: 9, phase })


  const changePhase = (value) => {
    setPhase(value)
    setPage(1)
    setData(null)
  }

  return (
    <>
      <section className="hero-section">
        <div className="container py-5">
          <div className="row align-items-center g-5 py-lg-5">
            <div className="col-lg-7">
              <span className="eyebrow"><i className="bi bi-lightning-charge-fill" /> Flash sale voucher</span>
              <h1 className="display-4 fw-bold mt-3">Biến điểm tích lũy<br />thành ưu đãi thật.</h1>
              <p className="lead text-secondary mt-3 mb-4">Khám phá campaign mới, đổi voucher trong vài giây và quản lý mọi phần thưởng tại một nơi.</p>
              <a className="btn btn-primary btn-lg" href="#campaigns">Khám phá ngay <i className="bi bi-arrow-down ms-2" /></a>
            </div>
            <div className="col-lg-5">
              <div className="hero-ticket shadow-lg">
                <div>
                  <span className="small opacity-75">VOUCHERHUB</span>
                  <div className="display-6 fw-bold mt-2">ĐỔI ĐIỂM</div>
                  <p className="mb-0 opacity-75">Ưu đãi giới hạn · Đổi bằng điểm</p>
                </div>
                <i className="bi bi-ticket-perforated display-3" />
              </div>
            </div>
          </div>
        </div>
      </section>

      <section id="campaigns" className="container py-5">
        <div className="d-flex flex-column flex-md-row justify-content-between align-items-md-end gap-3 mb-4">
          <div>
            <span className="text-primary fw-semibold">ƯU ĐÃI NỔI BẬT</span>
            <h2 className="fw-bold mt-1 mb-0">Chọn campaign dành cho bạn</h2>
          </div>
          <div className="btn-group filter-pills" role="group">
            {[
              ['', 'Tất cả'],
              ['ONGOING', 'Đang diễn ra'],
              ['UPCOMING', 'Sắp diễn ra'],
              ['ENDED', 'Đã kết thúc'],
            ].map(([value, label]) => (
              <button key={value} className={`btn ${phase === value ? 'btn-primary' : 'btn-outline-secondary'}`} onClick={() => changePhase(value)}>
                {label}
              </button>
            ))}
          </div>
        </div>
        <AlertMessage message={error} />
        {!data && !error ? <LoadingState /> : data?.items?.length ? (
          <>
            <div className="row g-4">
              {data.items.map((campaign) => (
                <div className="col-md-6 col-xl-4" key={campaign.id}>
                  <CampaignCard campaign={campaign} />
                </div>
              ))}
            </div>
            <Pagination page={data.pageNumber} pageSize={data.pageSize} totalCount={data.totalCount} onChange={(value) => { setPage(value); setData(null) }} />
          </>
        ) : !error && <EmptyState icon="ticket-perforated" title="Chưa có campaign" message="Các ưu đãi mới sẽ sớm xuất hiện tại đây." />}
      </section>
    </>
  )
}
