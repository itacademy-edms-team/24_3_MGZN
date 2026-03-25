// src/components/AppRoutes/AppRoutes.tsx
import React from 'react';
import { Routes, Route, useLocation } from 'react-router-dom';
import CatalogPage from '../pages/CatalogPage.js';
import CategoryPage from '../pages/CategoryPage.tsx';
import ProductPage from '../pages/ProductPage.tsx';
import CheckoutPage from '../pages/CheckoutPage.js';
import EmailVerificationPage from '../pages/EmailVerificationPage.js';
import OrderSuccessPage from '../pages/OrderSuccessPage/OrderSuccessPage.js';
import PaymentPage from '../pages/PaymentPage.js';
import PaymentConfirmationPage from '../pages/PaymentConfirmationPage/PaymentConfirmationPage.js';
import SearchResultsPage from '../pages/SearchResultPage/SearchResultsPage.tsx';

const AppRoutes: React.FC = () => {
  const location = useLocation(); // ✅ Теперь это работает — мы внутри Router!

  return (
    // ✅ key={location.key} заставляет пересоздавать компонент при изменении параметров URL
    <Routes>
      <Route path="/" element={<CatalogPage />} />
      <Route path="/catalog" element={<CatalogPage />} />
      <Route path="/category/:categoryName" element={<CategoryPage />} />
      <Route path="/product/:productId" element={<ProductPage />} />
      <Route path="/checkout" element={<CheckoutPage />} />
      <Route path="/email-verification" element={<EmailVerificationPage />} />
      <Route path="/order-success" element={<OrderSuccessPage />} />
      <Route path="/payment" element={<PaymentPage />} />
      <Route path="/payment-confirmation" element={<PaymentConfirmationPage />} />
      <Route path="/search" element={<SearchResultsPage />} />
    </Routes>
  );
};

export default AppRoutes;