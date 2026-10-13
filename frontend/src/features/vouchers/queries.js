import { useResource } from '../../shared/api/useResource.js'
import { getScope } from '../auth/session.js'
import { voucherApi } from './api.js'
export const useMine = (filters) => useResource(['vouchers', 'mine', getScope(), filters], signal => voucherApi.mine(filters, signal))
export const useIssued = (filters) => useResource(['vouchers', 'issued', getScope(), filters], signal => voucherApi.issued(filters, signal))
