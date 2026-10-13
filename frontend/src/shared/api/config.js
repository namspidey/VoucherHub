const configured = import.meta.env?.VITE_API_BASE_URL
if (import.meta.env?.PROD && !configured) {
  throw new Error('Thiếu VITE_API_BASE_URL trong cấu hình production.')
}
export const API_BASE_URL = (configured || 'http://localhost:5242/api').replace(/\/$/, '')
