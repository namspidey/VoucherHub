const prefix = 'voucherhub:redeem:'
const storageKey = (userId, offerId) => `${prefix}${userId}:${offerId}`
export function getPendingKey(userId, offerId, storage = sessionStorage) {
  return storage.getItem(storageKey(userId, offerId))
}
export function beginRedemption(userId, offerId, storage = sessionStorage) {
  const existing = getPendingKey(userId, offerId, storage)
  if (existing) return existing
  const key = crypto.randomUUID()
  storage.setItem(storageKey(userId, offerId), key)
  return key
}
export function finishRedemption(userId, offerId, storage = sessionStorage) {
  storage.removeItem(storageKey(userId, offerId))
}
