import React from 'react';
import { BrowserRouter as Router, Routes, Route } from 'react-router-dom';
import './App.css';

// Компоненты
import Header from './components/Header';
import Footer from './components/Footer';
import CatalogPage from './pages/CatalogPage';
import CategoryPage from './pages/CategoryPage';
import ProductPage from './pages/ProductPage';

function App() {
    return (
        <Router>
            <div className="App">
                <Header />
                <main>
                    <Routes>
                        <Route path="/" element={<CatalogPage />} />
                        <Route path="/catalog" element={<CatalogPage />} />
                        <Route path="/category/:categoryName" element={<CategoryPage />} />
                        <Route path="/product/:productId" element={<ProductPage />} />
                    </Routes>
                </main>
                <Footer />
            </div>
        </Router>
    );
}

export default App;