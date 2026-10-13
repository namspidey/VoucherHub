import { useUrlState } from '../../../shared/lib/useUrlState.js'
import AlertMessage from '../../../shared/ui/AlertMessage.jsx'
import EmptyState from '../../../shared/ui/states/EmptyState.jsx'
import LoadingState from '../../../shared/ui/states/LoadingState.jsx'
import Pagination from '../../../shared/ui/Pagination.jsx'
import { useMine } from '../queries.js'
import { formatDateTime, formatPoints } from '../../../shared/lib/format.js'

export default function MyVouchersPage() {
  const [expired, setExpired] = useUrlState('expired', '')
  const [page, setPage] = useUrlState('page', 1)
  const { data, error } = useMine({ page, pageSize: 8, expired })


  return (
    <div className="container py-5">
      <div className="d-flex flex-column flex-sm-row justify-content-between gap-3 mb-4">
        <div><span className="text-primary fw-semibold">VÍ VOUCHER</span><h1 className="h2 fw-bold">Voucher của tôi</h1></div>
        <select className="form-select w-auto" value={expired} onChange={(event) => { setExpired(event.target.value); setPage(1) }}>
          <option value="">Tất cả</option><option value="false">Còn hạn</option><option value="true">Hết hạn</option>
        </select>
      </div>
      <AlertMessage message={error} />
      {!data && !error ? <LoadingState /> : data?.items?.length ? (
        <>
          <div className="row g-4">
            {data.items.map((voucher) => <VoucherCard key={voucher.id} voucher={voucher} />)}
          </div>
          <Pagination page={data.pageNumber} pageSize={data.pageSize} totalCount={data.totalCount} onChange={setPage} />
        </>
      ) : !error && <EmptyState icon="ticket" title="Chưa có voucher" message="Voucher đổi thành công sẽ xuất hiện tại đây." />}
    </div>
  )
}

function VoucherCard({ voucher }) {
  const copy = () => navigator.clipboard.writeText(voucher.code)
  return (
    <div className="col-md-6">
      <article className={`voucher-card card border-0 shadow-sm ${voucher.isExpired ? 'opacity-75' : ''}`}>
        <div className="card-body p-4">
          <div className="d-flex justify-content-between">
            <span className={`badge ${voucher.isExpired ? 'text-bg-secondary' : 'text-bg-success'}`}>{voucher.isExpired ? 'Hết hạn' : 'Còn hiệu lực'}</span>
            <i className="bi bi-ticket-perforated fs-3 text-primary" />
          </div>
          <h2 className="h4 mt-3 mb-1">{voucher.offerName}</h2>
          <p className="text-secondary">{voucher.campaignName}</p>
          <div className="voucher-code d-flex align-items-center justify-content-between">
            <code>{voucher.code}</code>
            <button className="btn btn-sm btn-light" onClick={copy} title="Sao chép"><i className="bi bi-copy" /></button>
          </div>
          <div className="row small mt-3 text-secondary">
            <div className="col-6">Đã dùng <strong>{formatPoints(voucher.pointsSpent)}</strong> điểm</div>
            <div className="col-6 text-end">Hạn {formatDateTime(voucher.expiresAt)}</div>
          </div>
        </div>
      </article>
    </div>
  )
}
