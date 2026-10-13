import { apiRequest } from '../../shared/api/httpClient.js'
import { queryString } from '../../shared/api/queryString.js'
export const usersApi = {
list: (filters, signal) => apiRequest(`/admin/users?${queryString(filters)}`, { signal }),
block: (id, payload) => apiRequest(`/admin/users/${id}/block`, { method: 'POST', body: JSON.stringify(payload) })
}
