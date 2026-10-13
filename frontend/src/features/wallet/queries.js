import { useResource } from '../../shared/api/useResource.js'
import { getScope } from '../auth/session.js'
import { walletApi } from './api.js'
export const useHistory = (filters) => useResource(['wallet', 'history', getScope(), filters], signal => walletApi.history(filters, signal))
export const useAdminHistory = (id) => useResource(['wallet', 'adminHistory', getScope(), id], signal => walletApi.adminHistory(id, signal), Boolean(id))
