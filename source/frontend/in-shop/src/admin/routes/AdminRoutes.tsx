import React from 'react';
import { Navigate, Route, Routes } from 'react-router-dom';
import { AdminAuthProvider, useAdminAuth } from '../auth/AdminAuthContext.tsx';
import AdminLayout from '../layout/AdminLayout.tsx';
import AdminDashboard from '../pages/AdminDashboard.tsx';
import AdminDraftOrdersList from '../pages/AdminDraftOrdersList.tsx';
import AdminLogin from '../pages/AdminLogin.tsx';
import AdminOrdersList from '../pages/AdminOrdersList.tsx';
import AdminProductForm from '../pages/AdminProductForm.tsx';
import AdminProductsList from '../pages/AdminProductsList.tsx';
import { ADMIN_TOKEN_KEY } from '../api/adminClient.ts';

const RequireAdmin: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const { isAuthenticated, loading } = useAdminAuth();
  if (loading) return <p style={{ padding: '2rem' }}>Загрузка…</p>;
  if (!isAuthenticated && !sessionStorage.getItem(ADMIN_TOKEN_KEY)) {
    return <Navigate to="/admin/login" replace />;
  }
  if (!isAuthenticated) return <Navigate to="/admin/login" replace />;
  return <>{children}</>;
};

const AdminRoutes: React.FC = () => (
  <AdminAuthProvider>
    <Routes>
      <Route path="login" element={<AdminLogin />} />
      <Route
        path="/"
        element={
          <RequireAdmin>
            <AdminLayout />
          </RequireAdmin>
        }
      >
        <Route index element={<AdminDashboard />} />
        <Route path="products" element={<AdminProductsList />} />
        <Route path="products/new" element={<AdminProductForm />} />
        <Route path="products/:id" element={<AdminProductForm />} />
        <Route path="orders" element={<AdminOrdersList />} />
        <Route path="orders/drafts" element={<AdminDraftOrdersList />} />
      </Route>
      <Route path="*" element={<Navigate to="/admin" replace />} />
    </Routes>
  </AdminAuthProvider>
);

export default AdminRoutes;
