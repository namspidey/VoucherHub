import { beginRedemption, finishRedemption } from './pendingRequests.js'

export async function redeemWithPendingKey(userId, offerId, send, storage = sessionStorage) {
  const key = beginRedemption(userId, offerId, storage)
  try {
    const voucher = await send(offerId, key)
    finishRedemption(userId, offerId, storage)
    return voucher
  } catch (error) {
    // A network/5xx response may be lost after DB commit. Retry the same intention.
    if (error.status >= 400 && error.status < 500 && error.status !== 429) {
      finishRedemption(userId, offerId, storage)
    }
    throw error
  }
}
