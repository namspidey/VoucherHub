import { lazy, Suspense } from 'react'
import LoadingState from '../shared/ui/states/LoadingState.jsx'
import { Navigate, Route, Routes } from 'react-router-dom'
import AppLayout from './layouts/AppLayout.jsx'
import ProtectedRoute from './ProtectedRoute.jsx'
const HomePage = lazy(() => import('../features/campaigns/pages/HomePage.jsx'))
const CampaignDetailPage = lazy(() => import('../features/campaigns/pages/CampaignDetailPage.jsx'))
const LoginPage = lazy(() => import('../features/auth/pages/LoginPage.jsx'))
const RegisterPage = lazy(() => import('../features/auth/pages/RegisterPage.jsx'))
const VerifyEmailPage = lazy(() => import('../features/auth/pages/VerifyEmailPage.jsx'))
const AccountPage = lazy(() => import('../features/users/pages/AccountPage.jsx'))
const MyVouchersPage = lazy(() => import('../features/vouchers/pages/MyVouchersPage.jsx'))
const PointHistoryPage = lazy(() => import('../features/wallet/pages/PointHistoryPage.jsx'))
const AdminOverviewPage = lazy(() => import('../features/dashboard/pages/AdminOverviewPage.jsx'))
const AdminCampaignsPage = lazy(() => import('../features/campaigns/pages/AdminCampaignsPage.jsx'))
const AdminCampaignEditorPage = lazy(() => import('../features/campaigns/pages/AdminCampaignEditorPage.jsx'))
const AdminUsersPage = lazy(() => import('../features/users/pages/AdminUsersPage.jsx'))
const AdminIssuedVouchersPage = lazy(() => import('../features/vouchers/pages/AdminIssuedVouchersPage.jsx'))
const AdminAuditLogsPage = lazy(() => import('../features/audit/pages/AdminAuditLogsPage.jsx'))
const NotFoundPage = lazy(() => import('../features/navigation/pages/NotFoundPage.jsx'))

function App() {
  return (
    <Suspense fallback={<LoadingState fullscreen />}><Routes>
      <Route element={<AppLayout />}>
        <Route index element={<HomePage />} />
        <Route path="campaigns/:id" element={<CampaignDetailPage />} />
        <Route path="login" element={<LoginPage />} />
        <Route path="register" element={<RegisterPage />} />
        <Route path="verify-email" element={<VerifyEmailPage />} />

        <Route element={<ProtectedRoute />}>
          <Route path="account" element={<AccountPage />} />
          <Route path="my-vouchers" element={<MyVouchersPage />} />
          <Route path="point-history" element={<PointHistoryPage />} />
        </Route>

        <Route element={<ProtectedRoute role="ADMIN" />}>
          <Route path="admin" element={<AdminOverviewPage />} />
          <Route path="admin/campaigns" element={<AdminCampaignsPage />} />
          <Route path="admin/campaigns/new" element={<AdminCampaignEditorPage />} />
          <Route path="admin/campaigns/:id" element={<AdminCampaignEditorPage />} />
          <Route path="admin/users" element={<AdminUsersPage />} />
          <Route path="admin/issued-vouchers" element={<AdminIssuedVouchersPage />} />
          <Route path="admin/audit-logs" element={<AdminAuditLogsPage />} />
        </Route>

        <Route path="404" element={<NotFoundPage />} />
        <Route path="*" element={<Navigate to="/404" replace />} />
      </Route>
    </Routes></Suspense>
  )
}

export default App
