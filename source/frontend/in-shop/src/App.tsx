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

function App() {
  return (
    <Router>
      <CartProvider>
        {/* ✅ SessionHandler ОБЁРТЫВАЕТ всё приложение */}
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
          <div className="App">
            <Header />
            <CartModal />
            
            <main>
              {/* ✅ Роутинг внутри SessionHandler */}
              <AppRoutes />
            </main>
            
            <Footer />
          </div>
        </SessionHandler>
      </CartProvider>
    </Router>
  );
}

export default App;