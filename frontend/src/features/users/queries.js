import { useResource } from '../../shared/api/useResource.js'
import { getScope } from '../auth/session.js'
import { usersApi } from './api.js'
export const useList = (filters) => useResource(['users', 'list', getScope(), filters], signal => usersApi.list(filters, signal))
