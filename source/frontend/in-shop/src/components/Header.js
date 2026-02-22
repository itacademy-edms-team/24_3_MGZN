import React, { useContext } from 'react';
import { Link } from 'react-router-dom';
import { CartContext } from '../components/CartContext';
import './Header.css'; // Импортируем стили
import SearchComponent from './SearchComponent/SearchComponent.tsx'; // Убедитесь в правильности пути

/**
 * Компонент шапки приложения
 * Содержит логотип, компонент поиска и иконку корзины
 */
const Header = () => {
    const { openCart, cart } = useContext(CartContext);
    // Вычисляем общее количество товаров в корзине
    const totalQuantity = cart.reduce((total, item) => total + item.quantity, 0);

    return (
        <header className="header">
            <div className="header__container">
                {/* Логотип */}
                <Link to="/" className="header__logo-link">
                    <h1 className="header__logo">.InShop</h1>
                </Link>

                {/* Компонент поиска */}
                
                <SearchComponent />
                

                {/* Иконка корзины */}
                <button
                    className="header__cart-button"
                    onClick={openCart}
                    aria-label="Открыть корзину" // Добавляем aria-label для доступности
                >
                    <img
                        src="/cart-icon.png" // Убедитесь, что путь к иконке корректен
                        alt=""
                        className="header__cart-icon-img"
                    />
                    {totalQuantity > 0 && (
                        <span className="header__cart-count">{totalQuantity}</span>
                    )}
                </button>
            </div>
        </header>
    );
};

export default Header;