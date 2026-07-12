// ============================================
// Файл: src/components/AppRoutes/AppRoutes.tsx
// ============================================

import React from 'react';
import { Routes, Route } from 'react-router-dom';

// Pages
import CatalogPage from '../pages/CatalogPage';
import CategoryPage from '../pages/CategoryPage';
import ProductPage from '../pages/ProductPage';
import CheckoutPage from '../pages/CheckoutPage.js';
import EmailVerificationPage from '../pages/EmailVerificationPage.js';
import OrderSuccessPage from '../pages/OrderSuccessPage/OrderSuccessPage.js';
import PaymentPage from '../pages/PaymentPage.js';
import PaymentConfirmationPage from '../pages/PaymentConfirmationPage/PaymentConfirmationPage.js';
import SearchPage from '../pages/SearchPage';
import OrderTrackPage from '../pages/OrderTrackPage/OrderTrackPage';

const AppRoutes: React.FC = () => {
  return (
    <Routes>
      {/* Каталог */}
      <Route path="/" element={<CatalogPage />} />
      <Route path="/catalog" element={<CatalogPage />} />
      
      {/* Категория */}
      <Route path="/category/:categoryName" element={<CategoryPage />} />
      
      {/* Товар */}
      <Route path="/product/:productId" element={<ProductPage />} />
      
      {/* Поиск */}
      <Route path="/search" element={<SearchPage />} />
      
      {/* Оформление заказа */}
      <Route path="/checkout" element={<CheckoutPage />} />
      
      {/* Оплата */}
      <Route path="/payment" element={<PaymentPage />} />
      <Route path="/payment-confirmation" element={<PaymentConfirmationPage />} />
      
      {/* Успешный заказ */}
      <Route path="/order-success" element={<OrderSuccessPage />} />

      {/* Отслеживание заказа из письма */}
      <Route path="/order-track/:orderId" element={<OrderTrackPage />} />
      
      {/* Верификация email */}
      <Route path="/email-verification" element={<EmailVerificationPage />} />
      
      {/* 404 - не найдено */}
      <Route path="*" element={
        <div className="page-reveal" style={{ textAlign: 'center', padding: '48px' }}>
          <h2>Страница не найдена</h2>
          <a href="/" style={{ color: '#000', textDecoration: 'underline' }}>Вернуться на главную</a>
        </div>
      } />
    </Routes>
  );
};

export default AppRoutes;