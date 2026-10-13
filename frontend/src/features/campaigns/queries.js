import { useResource } from '../../shared/api/useResource.js'
import { campaignApi } from './api.js'
import { getScope } from '../auth/session.js'
export const useCatalog = (filters) => useResource(['campaigns', 'public', filters], (signal) => campaignApi.list(filters, signal))
export const useCampaign = (id) => useResource(['campaign', id], (signal) => campaignApi.detail(id, signal))
export const useAdminCampaigns = (filters) => useResource(['admin-campaigns', getScope(), filters], (signal) => campaignApi.adminList(filters, signal))
export const useAdminCampaign = (id) => useResource(['admin-campaign', getScope(), id], (signal) => campaignApi.adminDetail(id, signal), Boolean(id))
export const useOffers = (id) => useResource(['admin-offers', getScope(), id], (signal) => campaignApi.offers(id, signal), Boolean(id))
