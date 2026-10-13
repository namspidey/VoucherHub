import { apiRequest } from '../../shared/api/httpClient.js'
export const redeemVoucher = (offerId, key) => apiRequest(`/voucher-offers/${offerId}/redeem`, {
  method: 'POST', headers: { 'Idempotency-Key': key },
})
