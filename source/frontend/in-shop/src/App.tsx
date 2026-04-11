// ============================================
// Файл: src/App.tsx
// ============================================

import React from 'react';
import { BrowserRouter as Router } from 'react-router-dom';
import './App.css';

// Компоненты
import Header from './components/Header.js';
import Footer from './components/Footer.js';
import CartModal from './components/CartModal.js';
import { CartProvider } from './components/CartContext.js';
import SessionHandler from './components/SessionHandler.tsx';
import AppRoutes from './components/AppRoutes.tsx';
import { SessionProvider } from './context/SessionContext.tsx';

function App() {
  return (
    <Router>
      {/* Один экземпляр сессии на всё приложение (иначе SessionHandler и CartProvider расходятся по state) */}
      <SessionProvider>
      {/* ✅ Сначала гарантируем готовую сессию, потом монтируем корзину/приложение */}
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
            <button onClick={() => window.location.reload()}>
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
    </Router>
  );
}

export default App;