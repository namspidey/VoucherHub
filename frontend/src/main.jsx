import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import 'bootstrap/dist/css/bootstrap.min.css'
import 'bootstrap-icons/font/bootstrap-icons.css'
import './shared/styles/base.css'
import Providers from './app/providers.jsx'
import App from './app/router.jsx'
createRoot(document.getElementById('root')).render(
  <StrictMode><Providers><App /></Providers></StrictMode>,
)
