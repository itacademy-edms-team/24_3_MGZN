import React from 'react';
import { BrowserRouter as Router, Routes, Route } from 'react-router-dom';
import './App.css';

// Компоненты
import Header from './components/Header';
import Footer from './components/Footer';
import CatalogPage from './pages/CatalogPage';
import CategoryPage from './pages/CategoryPage';
import ProductPage from './pages/ProductPage';
import { CartProvider } from './components/CartContext'; // Импортируем CartProvider
import SessionHandler from './components/SessionHandler.tsx';

function App() {
    return (
        <Router>
            {/* Оборачиваем всё приложение в CartProvider */}
            <CartProvider>
                <div className="App">
                    <SessionHandler />
                    {/* Хедер с иконкой корзины */}
                    <Header />

                    {/* Основной контент */}
                    <main>
                        <Routes>
                            {/* Главная страница */}
                            <Route path="/" element={<CatalogPage />} />
                            {/* Страница каталога */}
                            <Route path="/catalog" element={<CatalogPage />} />
                            {/* Страница категории */}
                            <Route path="/category/:categoryName" element={<CategoryPage />} />
                            {/* Страница товара */}
                            <Route path="/product/:productId" element={<ProductPage />} />
                        </Routes>
                    </main>

                    {/* Футер */}
                    <Footer />
                </div>
            </CartProvider>
        </Router>
    );
}

export default App;