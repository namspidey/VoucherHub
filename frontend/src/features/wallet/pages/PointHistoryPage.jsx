import { useUrlState } from '../../../shared/lib/useUrlState.js'
import AlertMessage from '../../../shared/ui/AlertMessage.jsx'
import EmptyState from '../../../shared/ui/states/EmptyState.jsx'
import LoadingState from '../../../shared/ui/states/LoadingState.jsx'
import Pagination from '../../../shared/ui/Pagination.jsx'
import { useHistory } from '../queries.js'
import { formatDateTime, formatPoints } from '../../../shared/lib/format.js'

export default function PointHistoryPage() {
  const [type, setType] = useUrlState('type', '')
  const [page, setPage] = useUrlState('page', 1)
  const { data, error } = useHistory({ page, pageSize: 15, type })

  return (
    <div className="container py-5">
      <div className="d-flex justify-content-between align-items-end gap-3 mb-4">
        <div><span className="text-primary fw-semibold">SỔ ĐIỂM</span><h1 className="h2 fw-bold mb-0">Lịch sử điểm</h1></div>
        <select className="form-select w-auto" value={type} onChange={(event) => { setType(event.target.value); setPage(1) }}>
          <option value="">Tất cả giao dịch</option><option value="ADMIN_GRANT">Được cấp điểm</option><option value="VOUCHER_REDEMPTION">Đổi voucher</option>
        </select>
      </div>
      <AlertMessage message={error} />
      {!data && !error ? <LoadingState /> : data?.items?.length ? (
        <div className="card border-0 shadow-sm">
          <div className="table-responsive">
            <table className="table align-middle mb-0">
              <thead><tr><th>Giao dịch</th><th>Lý do</th><th>Thời gian</th><th className="text-end">Thay đổi</th><th className="text-end">Số dư sau</th></tr></thead>
              <tbody>{data.items.map((item) => (
                <tr key={item.id}>
                  <td><span className={`transaction-icon ${item.amount > 0 ? 'positive' : 'negative'}`}><i className={`bi bi-arrow-${item.amount > 0 ? 'down-left' : 'up-right'}`} /></span>{item.type === 'ADMIN_GRANT' ? 'Cấp điểm' : 'Đổi voucher'}</td>
                  <td className="text-secondary">{item.reason}</td>
                  <td className="text-secondary">{formatDateTime(item.createdAt)}</td>
                  <td className={`text-end fw-bold ${item.amount > 0 ? 'text-success' : 'text-danger'}`}>{item.amount > 0 ? '+' : ''}{formatPoints(item.amount)}</td>
                  <td className="text-end">{formatPoints(item.balanceAfter)}</td>
                </tr>
              ))}</tbody>
            </table>
          </div>
          <div className="card-body pt-0"><Pagination page={data.pageNumber} pageSize={data.pageSize} totalCount={data.totalCount} onChange={setPage} /></div>
        </div>
      ) : !error && <EmptyState icon="clock-history" title="Chưa có giao dịch" message="Lịch sử cộng và sử dụng điểm sẽ xuất hiện tại đây." />}
    </div>
  )
}
