import React from 'react';
import './Header.css';

const Header = () => {
    return (
        <header className="header">
            <div className="header-content">
                <h1>InShop</h1>
            </div>
            <div className="cart-icon">
                <img src="/cart-icon.png" alt="Корзина" />
            </div>
        </header>
    );
};

export default Header;