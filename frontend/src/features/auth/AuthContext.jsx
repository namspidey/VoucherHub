import { useCallback, useEffect, useMemo, useState } from 'react'
import { useQuery, useQueryClient } from '@tanstack/react-query'
import { configureSession } from '../../shared/api/httpClient.js'
import { clearSession, getToken, getScope, getExpiry, hasLiveToken, saveSession } from './session.js'
import { authApi } from './api.js'
import AuthStateContext from './AuthStateContext.js'

configureSession({ getToken, clearSession })
export function AuthProvider({ children }) {
  const client = useQueryClient()
  const [scope, setScope] = useState(getScope)
  const live = scope !== 'anonymous' && hasLiveToken()
  const profile = useQuery({
    queryKey: ['me', scope], queryFn: ({ signal }) => authApi.profile(signal),
    enabled: live, retry: false,
  })
  useEffect(() => {
    const changed = () => {
      client.cancelQueries()
      client.clear()
      setScope(getScope())
    }
    window.addEventListener('voucherhub:session', changed)
    window.addEventListener('storage', changed)
    return () => {
      window.removeEventListener('voucherhub:session', changed)
      window.removeEventListener('storage', changed)
    }
  }, [client])
  useEffect(() => {
    if (!live) return
    const delay = Date.parse(getExpiry()) - Date.now()
    const timer = window.setTimeout(clearSession, Math.max(0, Math.min(delay, 2147483647)))
    return () => window.clearTimeout(timer)
  }, [live, scope])
  const refreshUser = useCallback(async () => {
    if (!hasLiveToken()) { clearSession(); return null }
    return client.fetchQuery({ queryKey: ['me', getScope()], queryFn: ({ signal }) => authApi.profile(signal), staleTime: 0 })
  }, [client])
  const login = useCallback(async (email, password) => {
    const result = await authApi.login(email, password)
    saveSession(result.accessToken, result.expiresAtUtc)
    // Reject a failed profile request: login page stays open and allows retry.
    return refreshUser()
  }, [refreshUser])
  const logout = useCallback(() => clearSession(), [])
  const status = !live ? 'anonymous' : profile.isPending ? 'loading' : profile.isError ? 'unavailable' : 'authenticated'
  const value = useMemo(() => ({
    user: live ? profile.data || null : null, loading: status === 'loading',
    status, error: profile.error?.message || '', login, logout, refreshUser,
  }), [live, profile.data, profile.error, status, login, logout, refreshUser])
  return <AuthStateContext.Provider value={value}>{children}</AuthStateContext.Provider>
}
