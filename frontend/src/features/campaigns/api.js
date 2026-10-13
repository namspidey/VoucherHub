import { apiRequest } from '../../shared/api/httpClient.js'
import { queryString } from '../../shared/api/queryString.js'
export const campaignApi = {
  list: (filters, signal) => apiRequest(`/campaigns?${queryString(filters)}`, { public: true, signal }),
  detail: (id, signal) => apiRequest(`/campaigns/${id}`, { public: true, signal }),
  adminList: (filters, signal) => apiRequest(`/admin/campaigns?${queryString(filters)}`, { signal }),
  adminDetail: (id, signal) => apiRequest(`/admin/campaigns/${id}`, { signal }),
  offers: (id, signal) => apiRequest(`/admin/campaigns/${id}/offers`, { signal }),
  save: (id, payload) => apiRequest(id ? `/admin/campaigns/${id}` : '/admin/campaigns', { method: id ? 'PUT' : 'POST', body: JSON.stringify(payload) }),
  saveOffer: (id, offerId, payload) => apiRequest(`/admin/campaigns/${id}/offers${offerId ? `/${offerId}` : ''}`, { method: offerId ? 'PUT' : 'POST', body: JSON.stringify(payload) }),
  changeStatus: (id, action) => apiRequest(`/admin/campaigns/${id}/${action}`, { method: 'POST' }),
}
