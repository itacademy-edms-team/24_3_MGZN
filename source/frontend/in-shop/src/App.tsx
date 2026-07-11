// ============================================
// Файл: src/App.tsx
// ============================================

import React from 'react';
import { BrowserRouter as Router, Routes, Route } from 'react-router-dom';
import './App.css';

import Header from './components/Header.js';
import Footer from './components/Footer.js';
import CartModal from './components/CartModal.js';
import { CartProvider } from './components/CartContext.js';
import SessionHandler from './components/SessionHandler';
import AppRoutes from './components/AppRoutes';
import ErrorBoundary from './components/ErrorBoundary';
import { SessionProvider } from './context/SessionContext';
import AdminRoutes from './admin/routes/AdminRoutes';

/** Витрина: сессия + корзина. Покупательские потоки не изменены. */
const ShopApp: React.FC = () => (
  <SessionProvider>
    <SessionHandler
      fallback={
        <div className="app-loader">
          <div className="spinner" />
          <p>Инициализация сессии...</p>
        </div>
      }
      errorFallback={
        <div className="app-error">
          <p>⚠️ Не удалось инициализировать сессию</p>
          <button type="button" onClick={() => window.location.reload()}>
            Повторить
          </button>
        </div>
      }
    >
      <CartProvider>
        <div className="App">
          <Header />
          <CartModal />
          <main>
            <ErrorBoundary title="Ошибка витрины">
              <AppRoutes />
            </ErrorBoundary>
          </main>
          <Footer />
        </div>
      </CartProvider>
    </SessionHandler>
  </SessionProvider>
);

function App() {
  return (
    <Router>
      <Routes>
        <Route path="/admin/*" element={<ErrorBoundary title="Ошибка админ-панели"><AdminRoutes /></ErrorBoundary>} />
        <Route path="/*" element={<ShopApp />} />
      </Routes>
    </Router>
  );
}

export default App;
