import { apiRequest } from '../../shared/api/httpClient.js'
export const authApi = {
  login: (email, password) => apiRequest('/auth/login', { method: 'POST', public: true, body: JSON.stringify({ email, password }) }),
  register: (form) => apiRequest('/auth/register', { method: 'POST', public: true, body: JSON.stringify(form) }),
  resend: (email) => apiRequest('/auth/resend-verification', { method: 'POST', public: true, body: JSON.stringify({ email }) }),
  verify: (token) => apiRequest('/auth/verify-email', { method: 'POST', public: true, body: JSON.stringify({ token }) }),
  profile: (signal) => apiRequest('/users/me', { signal }),
}
