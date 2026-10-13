import { useResource } from '../../shared/api/useResource.js'
import { getScope } from '../auth/session.js'
import { campaignApi } from '../campaigns/api.js'
import { usersApi } from '../users/api.js'
import { voucherApi } from '../vouchers/api.js'
import { auditApi } from '../audit/api.js'
export function useOverview() {
  return useResource(['overview', getScope()], async (signal) => {
    const filters = { page: 1, pageSize: 5 }
    const [campaigns, users, vouchers, audits] = await Promise.all([
      campaignApi.adminList(filters, signal), usersApi.list(filters, signal),
      voucherApi.issued(filters, signal), auditApi.list(filters, signal),
    ])
    return { campaigns, users, vouchers, audits }
  })
}
