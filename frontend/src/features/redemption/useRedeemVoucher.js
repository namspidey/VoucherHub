import { useMutation, useQueryClient } from '@tanstack/react-query'
import { getPendingKey } from './pendingRequests.js'
import { redeemWithPendingKey } from './redeemWithPendingKey.js'
import { redeemVoucher } from './api.js'
export function useRedeemVoucher(userId) {
  const client = useQueryClient()
  const mutation = useMutation({
    mutationFn: (offerId) => redeemWithPendingKey(userId, offerId, redeemVoucher),
    onSuccess: () => {
      // Do not change the committed mutation result if refetch fails.
      for (const prefix of ['me', 'campaign', 'campaigns', 'wallet', 'vouchers']) {
        client.invalidateQueries({ queryKey: [prefix] })
      }
    },
  })
  return { ...mutation, hasPending: (offerId) => userId && Boolean(getPendingKey(userId, offerId)) }
}
