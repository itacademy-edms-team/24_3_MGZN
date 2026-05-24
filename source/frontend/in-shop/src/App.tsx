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
import SessionHandler from './components/SessionHandler.tsx';
import AppRoutes from './components/AppRoutes.tsx';
import { SessionProvider } from './context/SessionContext.tsx';
import AdminRoutes from './admin/routes/AdminRoutes.tsx';

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
            <AppRoutes />
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
        <Route path="/admin/*" element={<AdminRoutes />} />
        <Route path="/*" element={<ShopApp />} />
      </Routes>
    </Router>
  );
}

export default App;
