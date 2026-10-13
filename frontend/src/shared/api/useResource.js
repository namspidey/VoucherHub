import { useState } from 'react'
import { useQuery, useQueryClient } from '@tanstack/react-query'

// Features own query keys and fetch functions; this hook only adapts display/error state.
export function useResource(key, fetcher, enabled = true) {
  const client = useQueryClient()
  const query = useQuery({ queryKey: key, queryFn: ({ signal }) => fetcher(signal), enabled })
  const [localError, setLocalError] = useState('')
  return {
    data: query.data ?? null,
    error: localError || query.error?.message || '',
    setError: setLocalError,
    setData: (value) => { if (value !== null) client.setQueryData(key, value) },
    refetch: query.refetch,
    isLoading: query.isPending,
  }
}
