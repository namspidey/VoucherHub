import { useContext } from 'react'
import AuthStateContext from './AuthStateContext.js'

export default function useAuth() {
  return useContext(AuthStateContext)
}
