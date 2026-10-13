import { apiRequest } from '../../shared/api/httpClient.js'
import { queryString } from '../../shared/api/queryString.js'
export const voucherApi = {
mine: (filters, signal) => apiRequest(`/users/me/vouchers?${queryString(filters)}`, { signal }),
issued: (filters, signal) => apiRequest(`/admin/user-vouchers?${queryString(filters)}`, { signal })
}
