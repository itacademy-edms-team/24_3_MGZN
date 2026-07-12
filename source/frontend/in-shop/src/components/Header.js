import React, { useContext } from 'react';
import { Link, useLocation } from 'react-router-dom';
import { CartContext } from '../components/CartContext';
import './Header.css';
import SearchComponent from './SearchComponent/SearchComponent';
import { useHomeLogoMorph } from '../hooks/useHomeLogoMorph';
import { useMatchMedia } from '../hooks/useMatchMedia';

const Header = () => {
    const { openCart, cart } = useContext(CartContext);
    const { pathname } = useLocation();
    const totalQuantity = cart.reduce((total, item) => total + item.quantity, 0);
    const isHome = pathname === '/' || pathname === '/catalog';
    const isMobile = useMatchMedia('(max-width: 768px)');
    const { style: morphStyle } = useHomeLogoMorph(isHome);

    return (
        <header className="header">
            <div
                key={isMobile ? pathname : 'header-container'}
                className={`header__container${isHome ? ' header__container--home' : ''}`}
            >
                <Link
                    to="/"
                    className={`header__logo-link${isHome ? ' header__logo-link--morph' : ''}`}
                    aria-label=".InShop — на главную"
                >
                    <h1 id="header-logo" className="header__logo">.InShop</h1>
                </Link>

                {!isHome && (
                    <div className="header__search">
                        <SearchComponent />
                    </div>
                )}

                <button
                    className="header__cart-button"
                    onClick={openCart}
                    aria-label="Открыть корзину"
                >
                    <img
                        src="/cart-icon.png"
                        alt=""
                        className="header__cart-icon-img"
                    />
                    {totalQuantity > 0 && (
                        <span className="header__cart-count">{totalQuantity}</span>
                    )}
                </button>
            </div>

            {isHome && morphStyle && (
                <Link
                    to="/"
                    className="home-logo-morph"
                    style={morphStyle}
                    aria-hidden="true"
                    tabIndex={-1}
                >
                    .InShop
                </Link>
            )}
        </header>
    );
};

export default Header;
