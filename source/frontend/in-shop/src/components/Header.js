// src/components/Header.js
import React, { useContext } from 'react';
import { Link } from 'react-router-dom';
import { CartContext } from '../components/CartContext';
import './Header.css';

const Header = () => {
    const { openCart, cart } = useContext(CartContext);
    const totalQuantity = cart.reduce((total, item) => total + item.quantity, 0);

    return (
        <header className="header">
            <div className="header-content">
                <Link to="/" className="logo">
                    <h1>.InShop</h1>
                </Link>
            </div>

            {/* Иконка корзины */}
            <div className="cart-icon" onClick={openCart}>
                <img src="/cart-icon.png" alt="Корзина" />
                {totalQuantity > 0 && (
                    <span className="cart-count">{totalQuantity}</span>
                )}
            </div>
        </header>
    );
};

export default Header;