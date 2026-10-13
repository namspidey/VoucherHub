import { apiRequest } from '../../shared/api/httpClient.js'
import { queryString } from '../../shared/api/queryString.js'
export const walletApi = {
history: (filters, signal) => apiRequest(`/users/me/point-transactions?${queryString(filters)}`, { signal }),
adminHistory: (id, signal) => apiRequest(`/admin/users/${id}/point-transactions?page=1&pageSize=10`, { signal }),
grant: (id, payload) => apiRequest(`/admin/users/${id}/points/grants`, { method: 'POST', body: JSON.stringify(payload) })
}
