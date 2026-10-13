import { useResource } from '../../shared/api/useResource.js'
import { getScope } from '../auth/session.js'
import { auditApi } from './api.js'
export const useList = (filters) => useResource(['audit', 'list', getScope(), filters], signal => auditApi.list(filters, signal))
