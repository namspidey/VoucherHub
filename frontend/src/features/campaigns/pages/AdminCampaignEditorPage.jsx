import CampaignForm from '../components/CampaignForm.jsx'
import OfferForm from '../components/OfferForm.jsx'
import { useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import AdminShell from '../../../shared/ui/AdminShell.jsx'
import AlertMessage from '../../../shared/ui/AlertMessage.jsx'
import LoadingState from '../../../shared/ui/states/LoadingState.jsx'
import { campaignApi } from '../api.js'
import { useAdminCampaign, useOffers } from '../queries.js'
import { useQueryClient } from '@tanstack/react-query'
import Dialog from '../../../shared/ui/Dialog.jsx'
import { badgeClass, formatDateTime, formatPoints, statusLabel, toIso, toLocalInput } from '../../../shared/lib/format.js'

const emptyCampaign = { name: '', description: '', startsAt: '', endsAt: '', maxRedemptionsPerUser: 1 }
const emptyOffer = { name: '', description: '', pointsCost: 1, totalQuantity: 1, expiresAt: '' }

export default function AdminCampaignEditorPage() {
  const { id } = useParams()
  const campaignQuery = useAdminCampaign(id)
  const offersQuery = useOffers(id)
  if (id && (campaignQuery.error || offersQuery.error)) return <div className="container py-5"><AlertMessage message={campaignQuery.error || offersQuery.error} /></div>
  if (id && (!campaignQuery.data || !offersQuery.data)) return <LoadingState fullscreen />
  return <CampaignEditor key={id || 'new'} initialCampaign={campaignQuery.data} offers={offersQuery.data || []} reload={() => Promise.all([campaignQuery.refetch(), offersQuery.refetch()])} />
}

function CampaignEditor({ initialCampaign, offers, reload }) {
  const { id } = useParams()
  const navigate = useNavigate()
  const editing = Boolean(id)
  const [campaign, setCampaign] = useState(() => initialCampaign || { status: 'DRAFT' })
  const client = useQueryClient()
  const [statusAction, setStatusAction] = useState(null)
  const [form, setForm] = useState(() => initialCampaign ? {
    name: initialCampaign.name, description: initialCampaign.description || '',
    startsAt: toLocalInput(initialCampaign.startsAt), endsAt: toLocalInput(initialCampaign.endsAt),
    maxRedemptionsPerUser: initialCampaign.maxRedemptionsPerUser,
  } : emptyCampaign)
  const [offerForm, setOfferForm] = useState(null)
  const [editingOfferId, setEditingOfferId] = useState(null)
  const [error, setError] = useState('')
  const [success, setSuccess] = useState('')
  const [busy, setBusy] = useState(false)

  const load = async () => {
    await reload()
    client.invalidateQueries({ queryKey: ['admin-campaigns'] })
    client.invalidateQueries({ queryKey: ['campaigns'] })
    client.invalidateQueries({ queryKey: ['campaign'] })
  }

  const campaignPayload = () => ({
    name: form.name,
    description: form.description || null,
    startsAt: toIso(form.startsAt),
    endsAt: toIso(form.endsAt),
    maxRedemptionsPerUser: Number(form.maxRedemptionsPerUser),
  })

  const saveCampaign = async (event) => {
    event.preventDefault()
    setBusy(true); setError(''); setSuccess('')
    try {
      if (editing) {
        const result = await campaignApi.save(id, campaignPayload())
        setCampaign(result)
        await load()
        setSuccess('Đã lưu cấu hình campaign.')
      } else {
        const result = await campaignApi.save(null, campaignPayload())
        client.invalidateQueries({ queryKey: ['admin-campaigns'] })
        navigate(`/admin/campaigns/${result.id}`, { replace: true })
      }
    } catch (err) { setError(err.message) } finally { setBusy(false) }
  }

  const changeStatus = async (action) => {
    setBusy(true); setError(''); setSuccess('')
    try {
      const result = await campaignApi.changeStatus(id, action)
      setCampaign(result)
      setStatusAction(null)
      await load()
      setSuccess(action === 'publish' ? 'Campaign đã được publish.' : 'Campaign đã được hủy.')
    } catch (err) { setError(err.message) } finally { setBusy(false) }
  }

  const openOffer = (offer = null) => {
    setEditingOfferId(offer?.id || null)
    setOfferForm(offer ? {
      name: offer.name,
      description: offer.description || '',
      pointsCost: offer.pointsCost,
      totalQuantity: offer.totalQuantity,
      expiresAt: toLocalInput(offer.expiresAt),
    } : { ...emptyOffer, expiresAt: form.endsAt ? toLocalInput(new Date(new Date(form.endsAt).getTime() + 86400000)) : '' })
  }

  const saveOffer = async (event) => {
    event.preventDefault()
    setBusy(true); setError(''); setSuccess('')
    try {
      await campaignApi.saveOffer(id, editingOfferId, {
          name: offerForm.name,
          description: offerForm.description || null,
          pointsCost: Number(offerForm.pointsCost),
          totalQuantity: Number(offerForm.totalQuantity),
          expiresAt: toIso(offerForm.expiresAt),
      })
      setOfferForm(null)
      setEditingOfferId(null)
      await load()
      setSuccess('Đã lưu voucher offer.')
    } catch (err) { setError(err.message) } finally { setBusy(false) }
  }

  if (!campaign) return <LoadingState fullscreen />
  const draft = campaign.status === 'DRAFT'

  return (
    <AdminShell
      title={editing ? 'Quản lý campaign' : 'Tạo campaign'}
      description={editing ? 'Cấu hình campaign, voucher offer và trạng thái phát hành.' : 'Tạo bản nháp trước, sau đó thêm voucher offer.'}
      actions={<Link className="btn btn-outline-secondary" to="/admin/campaigns"><i className="bi bi-arrow-left me-2" />Danh sách</Link>}
    >
      <Dialog open={Boolean(statusAction)} title={statusAction === 'publish' ? 'Publish campaign' : 'Cancel campaign'} onClose={() => { if (!busy) setStatusAction(null) }}>
        <p>{statusAction === 'publish' ? 'Sau publish cấu hình đổi voucher sẽ được khóa.' : 'Cancel sẽ dừng các lượt đổi mới.'}</p>
        <AlertMessage message={error} />
        <button className="btn btn-primary" disabled={busy} onClick={() => changeStatus(statusAction)}>Xác nhận</button>
      </Dialog>
      <AlertMessage message={error} onClose={() => setError('')} />
      <AlertMessage type="success" message={success} onClose={() => setSuccess('')} />

      {editing && (
        <div className="card border-0 shadow-sm mb-4">
          <div className="card-body d-flex flex-column flex-md-row justify-content-between align-items-md-center gap-3">
            <div><span className={`badge ${badgeClass(campaign.status)}`}>{statusLabel[campaign.status]}</span><span className="small text-secondary ms-3">Cập nhật {formatDateTime(campaign.updatedAt)}</span></div>
            <div className="d-flex gap-2">
              {draft && <button className="btn btn-success" disabled={busy} onClick={() => setStatusAction('publish')}><i className="bi bi-send-check me-2" />Publish</button>}
              {campaign.status !== 'CANCELLED' && <button className="btn btn-outline-danger" disabled={busy} onClick={() => setStatusAction('cancel')}><i className="bi bi-x-octagon me-2" />Cancel</button>}
            </div>
          </div>
        </div>
      )}

      <div className="card border-0 shadow-sm mb-4">
        <div className="card-header bg-white p-4"><h2 className="h5 mb-0">Thông tin campaign</h2></div>
        <div className="card-body p-4">
          <CampaignForm form={form} setForm={setForm} draft={draft} busy={busy} editing={editing} onSubmit={saveCampaign} />
        </div>
      </div>

      {editing && (
        <div className="card border-0 shadow-sm">
          <div className="card-header bg-white p-4 d-flex justify-content-between align-items-center">
            <div><h2 className="h5 mb-1">Voucher offers</h2><span className="small text-secondary">{offers.length} loại voucher</span></div>
            {draft && <button className="btn btn-primary" onClick={() => openOffer()}><i className="bi bi-plus-lg me-2" />Thêm offer</button>}
          </div>
          {offerForm && (
            <div className="card-body p-4 bg-body-tertiary border-bottom">
              <OfferForm offerForm={offerForm} setOfferForm={setOfferForm} editingOfferId={editingOfferId} busy={busy} onSubmit={saveOffer} />
            </div>
          )}
          <div className="table-responsive">
            <table className="table align-middle mb-0"><thead><tr><th>Offer</th><th>Giá</th><th>Đã phát / Tổng</th><th>Hết hạn</th><th /></tr></thead>
              <tbody>{offers.length ? offers.map((offer) => <tr key={offer.id}>
                <td><strong>{offer.name}</strong><div className="small text-secondary">{offer.description}</div></td>
                <td>{formatPoints(offer.pointsCost)} điểm</td>
                <td>{offer.issuedQuantity} / {offer.totalQuantity}</td>
                <td>{formatDateTime(offer.expiresAt)}</td>
                <td className="text-end">{draft && <button className="btn btn-sm btn-outline-primary" onClick={() => openOffer(offer)}><i className="bi bi-pencil" /></button>}</td>
              </tr>) : <tr><td colSpan="5" className="text-center text-secondary py-5">Chưa có voucher offer.</td></tr>}</tbody>
            </table>
          </div>
        </div>
      )}
    </AdminShell>
  )
}
