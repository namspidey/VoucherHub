import { API_BASE_URL } from './config.js'
import { ApiError } from './ApiError.js'
import { errorMessages } from './errorMessages.js'

let sessionAdapter = { getToken: () => null, clearSession: () => {} }
export function configureSession(adapter) { sessionAdapter = adapter }

export async function apiRequest(path, options = {}) {
  const { public: isPublic = false, ...requestOptions } = options
  const headers = new Headers(options.headers)
  const token = isPublic ? null : sessionAdapter.getToken()
  if (token) headers.set('Authorization', `Bearer ${token}`)
  if (options.body) headers.set('Content-Type', 'application/json')
  let response
  try {
    response = await fetch(`${API_BASE_URL}${path}`, { ...requestOptions, headers })
  } catch (error) {
    if (error.name === 'AbortError') throw error
    throw new ApiError('Không thể kết nối API. Vui lòng thử lại.')
  }
  const text = await response.text()
  let data = null
  if (text && /\bjson\b/i.test(response.headers.get('content-type') || '')) {
    try { data = JSON.parse(text) } catch { throw new ApiError('Response API không hợp lệ.', response.status >= 400 ? response.status : 502) }
  }
  if (!response.ok) {
    if (response.status === 401 && token && token === sessionAdapter.getToken()) sessionAdapter.clearSession()
    const validation = data?.errors ? Object.values(data.errors).flat().join(' ') : null
    const fallback = { 401: 'Phiên đăng nhập không hợp lệ.', 403: 'Bạn không có quyền thực hiện thao tác.', 404: 'Không tìm thấy dữ liệu.', 409: 'Thao tác xung đột với dữ liệu hiện tại.', 429: 'Bạn thao tác quá nhanh. Hãy thử lại sau.' }
    throw new ApiError(validation || errorMessages[data?.code] || data?.message || data?.title ||
      fallback[response.status] || 'Có lỗi xảy ra. Vui lòng thử lại.', response.status, data?.code, data?.errors)
  }
  assertSafeNumbers(data)
  return data
}
function assertSafeNumbers(value) {
  if (typeof value === 'number' && Number.isInteger(value) && !Number.isSafeInteger(value)) {
    throw new ApiError('Dữ liệu điểm vượt phạm vi số chính xác của giao diện. Vui lòng liên hệ quản trị.', 502, 'UNSAFE_INTEGER')
  }
  if (value && typeof value === 'object') Object.values(value).forEach(assertSafeNumbers)
}
