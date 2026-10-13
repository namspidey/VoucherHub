import { apiRequest } from '../../shared/api/httpClient.js'
import { queryString } from '../../shared/api/queryString.js'
export const auditApi = {
list: (filters, signal) => apiRequest(`/admin/audit-logs?${queryString(filters)}`, { signal })
}
