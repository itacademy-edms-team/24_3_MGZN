import React from 'react';
import { Navigate, Route, Routes } from 'react-router-dom';
import { AdminAuthProvider, useAdminAuth } from '../auth/AdminAuthContext';
import AdminLayout from '../layout/AdminLayout';
import AdminCategoriesList from '../pages/AdminCategoriesList';
import AdminCategoryForm from '../pages/AdminCategoryForm';
import AdminDraftOrdersList from '../pages/AdminDraftOrdersList';
import AdminLogin from '../pages/AdminLogin';
import AdminOrdersList from '../pages/AdminOrdersList';
import AdminProductForm from '../pages/AdminProductForm';
import AdminProductsList from '../pages/AdminProductsList';
import AdminShipCompaniesList from '../pages/AdminShipCompaniesList';
import AdminShipCompanyForm from '../pages/AdminShipCompanyForm';

const RequireAdmin: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const { isAuthenticated, loading } = useAdminAuth();
  if (loading) return <p style={{ padding: '2rem' }}>Загрузка…</p>;
  if (!isAuthenticated) return <Navigate to="/admin/login" replace />;
  return <>{children}</>;
};

const AdminRoutes: React.FC = () => (
  <AdminAuthProvider>
    <Routes>
      <Route path="login" element={<AdminLogin />} />
      <Route
        path=""
        element={
          <RequireAdmin>
            <AdminLayout />
          </RequireAdmin>
        }
      >
        <Route index element={<Navigate to="products" replace />} />
        <Route path="products" element={<AdminProductsList />} />
        <Route path="products/new" element={<AdminProductForm />} />
        <Route path="products/:id" element={<AdminProductForm />} />
        <Route path="categories" element={<AdminCategoriesList />} />
        <Route path="categories/new" element={<AdminCategoryForm />} />
        <Route path="categories/:id" element={<AdminCategoryForm />} />
        <Route path="ship-companies" element={<AdminShipCompaniesList />} />
        <Route path="ship-companies/new" element={<AdminShipCompanyForm />} />
        <Route path="ship-companies/:id" element={<AdminShipCompanyForm />} />
        <Route path="orders" element={<AdminOrdersList />} />
        <Route path="orders/drafts" element={<AdminDraftOrdersList />} />
      </Route>
      <Route path="*" element={<Navigate to="/admin/products" replace />} />
    </Routes>
  </AdminAuthProvider>
);

export default AdminRoutes;
