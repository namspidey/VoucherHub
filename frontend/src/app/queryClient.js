import { QueryClient } from '@tanstack/react-query'
export const queryClient = new QueryClient({
  defaultOptions: {
    queries: { staleTime: 10000, retry: (count, error) => count < 1 && (error.status === 0 || error.status >= 500) },
    mutations: { retry: false },
  },
})
