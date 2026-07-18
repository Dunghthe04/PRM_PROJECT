import { ConfigProvider, App as AntApp } from 'antd'
import viVN from 'antd/locale/vi_VN'
import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom'
import { AuthProvider } from './auth/AuthContext'
import { RequireAuth } from './auth/RequireAuth'
import { AdminLayout } from './layouts/AdminLayout'
import { TeacherLayout } from './layouts/TeacherLayout'
import { ForgotPasswordPage } from './pages/ForgotPasswordPage'
import { LoginPage } from './pages/LoginPage'
import { AdminAnnouncePage } from './pages/admin/AnnouncePage'
import { AdminAssignmentsPage } from './pages/admin/AssignmentsPage'
import { AdminCatalogPage } from './pages/admin/CatalogPage'
import { AdminFeesPage } from './pages/admin/FeesPage'
import { AdminReportsPage } from './pages/admin/ReportsPage'
import { AdminUsersPage } from './pages/admin/UsersPage'
import { TeacherAnnouncePage } from './pages/teacher/AnnouncePage'
import { TeacherAttendancePage } from './pages/teacher/AttendancePage'
import { TeacherClassesPage } from './pages/teacher/ClassesPage'
import { TeacherGradesImportPage } from './pages/teacher/GradesImportPage'
import { TeacherLeavePage } from './pages/teacher/LeavePage'

export default function App() {
  return (
    <ConfigProvider
      locale={viVN}
      theme={{
        token: {
          colorPrimary: '#FF6B00',
          borderRadius: 8,
        },
      }}
    >
      <AntApp>
        <AuthProvider>
          <BrowserRouter>
            <Routes>
              <Route path="/login" element={<LoginPage />} />
              <Route path="/forgot-password" element={<ForgotPasswordPage />} />
              <Route element={<RequireAuth roles={['Admin']} />}>
                <Route path="/admin" element={<AdminLayout />}>
                  <Route index element={<AdminReportsPage />} />
                  <Route path="users" element={<AdminUsersPage />} />
                  <Route path="catalog" element={<AdminCatalogPage />} />
                  <Route path="assignments" element={<AdminAssignmentsPage />} />
                  <Route path="fees" element={<AdminFeesPage />} />
                  <Route path="announce" element={<AdminAnnouncePage />} />
                </Route>
              </Route>
              <Route element={<RequireAuth roles={['Teacher']} />}>
                <Route path="/teacher" element={<TeacherLayout />}>
                  <Route index element={<TeacherClassesPage />} />
                  <Route path="attendance" element={<TeacherAttendancePage />} />
                  <Route path="leave" element={<TeacherLeavePage />} />
                  <Route path="grades" element={<TeacherGradesImportPage />} />
                  <Route path="announce" element={<TeacherAnnouncePage />} />
                </Route>
              </Route>
              <Route path="*" element={<Navigate to="/login" replace />} />
            </Routes>
          </BrowserRouter>
        </AuthProvider>
      </AntApp>
    </ConfigProvider>
  )
}
