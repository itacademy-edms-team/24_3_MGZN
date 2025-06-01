import React from 'react';
import { Link } from 'react-router-dom'; // Импортируем Link
import './Header.css';

const Header = () => {
    return (
        <header className="header">
            <div className="header-content">
                <Link to="/" className="header-content">
                    <h1>InShop</h1>
                </Link>
            </div>
            <div className="cart-icon">
                <img src="/cart-icon.png" alt="Корзина" />
            </div>
        </header>
    );
};

export default Header;