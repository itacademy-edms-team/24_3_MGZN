// src/App.tsx
import React from 'react';
import { BrowserRouter as Router } from 'react-router-dom';
import './App.css';

// Компоненты
import Header from './components/Header.js';
import Footer from './components/Footer.js';
import CartModal from './components/CartModal.js';
import { CartProvider } from './components/CartContext.js';
import SessionHandler from './components/SessionHandler.tsx';
import AppRoutes from './components/AppRoutes.tsx'; // ✅ Импортируем новый компонент

function App() {
  return (
    <Router>
      <CartProvider>
        <div className="App">
          <SessionHandler />
          <Header />
          <CartModal />
          
          <main>
            {/* ✅ Роутинг вынесен в отдельный компонент внутри Router */}
            <AppRoutes />
          </main>
          
          <Footer />
        </div>
      </CartProvider>
    </Router>
  );
}

export default App;