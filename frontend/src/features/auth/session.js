const TOKEN = 'voucherhub_access_token'
const EXPIRY = 'voucherhub_token_expiry'
const SCOPE = 'voucherhub_session_scope'
export const getToken = () => localStorage.getItem(TOKEN)
export const getScope = () => localStorage.getItem(SCOPE) || 'anonymous'
export const getExpiry = () => localStorage.getItem(EXPIRY)
export function clearSession() {
  localStorage.removeItem(TOKEN); localStorage.removeItem(EXPIRY); localStorage.removeItem(SCOPE)
  window.dispatchEvent(new Event('voucherhub:session'))
}
export function saveSession(accessToken, expiresAtUtc) {
  localStorage.setItem(TOKEN, accessToken); localStorage.setItem(EXPIRY, expiresAtUtc)
  localStorage.setItem(SCOPE, crypto.randomUUID())
  window.dispatchEvent(new Event('voucherhub:session'))
}
export const hasLiveToken = () => Boolean(getToken() && (!getExpiry() || Date.parse(getExpiry()) > Date.now()))
