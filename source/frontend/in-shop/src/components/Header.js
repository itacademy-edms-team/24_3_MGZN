import React, { useContext } from 'react';
import { Link, useLocation } from 'react-router-dom';
import { CartContext } from '../components/CartContext';
import './Header.css';
import SearchComponent from './SearchComponent/SearchComponent';
import { useHomeLogoMorph } from '../hooks/useHomeLogoMorph';

const Header = () => {
    const { openCart, cart } = useContext(CartContext);
    const { pathname } = useLocation();
    const totalQuantity = cart.reduce((total, item) => total + item.quantity, 0);
    const isHome = pathname === '/' || pathname === '/catalog';
    const { style: morphStyle } = useHomeLogoMorph(isHome);

    const morphCover = Boolean(morphStyle);

    return (
        <header
            className={[
                'header',
                isHome ? 'header--home' : '',
                morphCover ? 'header--morph-cover' : '',
            ]
                .filter(Boolean)
                .join(' ')}
        >
            <div className="header__container">
                <Link
                    to="/"
                    className="header__logo-link"
                    aria-label=".InShop — на главную"
                >
                    <h1 id="header-logo" className="header__logo">.InShop</h1>
                </Link>

                <div className="header__search" aria-hidden={isHome || undefined}>
                    <SearchComponent />
                </div>

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
                <span
                    className="home-logo-morph"
                    style={morphStyle}
                    aria-hidden="true"
                >
                    .InShop
                </span>
            )}
        </header>
    );
};

export default Header;
