import { BrowserRouter } from 'react-router-dom'
import { QueryClientProvider } from '@tanstack/react-query'
import { queryClient } from './queryClient.js'
import { AuthProvider } from '../features/auth/AuthContext.jsx'
export default function Providers({ children }) {
  return <BrowserRouter><QueryClientProvider client={queryClient}><AuthProvider>{children}</AuthProvider></QueryClientProvider></BrowserRouter>
}
