// src/pages/CheckoutPage.js
import React, { useContext, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { CartContext } from '../components/CartContext';
import CheckoutItemCard from '../components/CheckoutItemCard';
import CheckoutForm from '../components/CheckoutForm';
import './CheckoutPage.css';
import '../components/CheckoutItemCard.css';

const CheckoutPage = () => {
    const { cart, loading, error, changeQuantity, removeFromCart, fetchCart, isValid } = useContext(CartContext);

    useEffect(() => {
        if (isValid) {
            fetchCart();
        }
    }, [isValid, fetchCart]);

    return (
        <div className="checkout-page">
            <div className="checkout-shell page-reveal">
                <header className="checkout-header">
                    <p className="checkout-header__eyebrow">.InShop</p>
                    <h1>Оформление заказа</h1>
                </header>

                <div className="checkout-container">
                    <div className="checkout-left page-reveal page-reveal--delay-1">
                        {loading ? (
                            <p className="checkout-status">Загрузка корзины...</p>
                        ) : cart.length === 0 ? (
                            <div className="checkout-empty">
                                <p>Корзина пуста. Добавьте товары перед оформлением заказа.</p>
                                <Link to="/" className="checkout-empty__link">
                                    В каталог
                                </Link>
                            </div>
                        ) : (
                            <CheckoutForm />
                        )}
                    </div>

                    <div className="checkout-right page-reveal page-reveal--delay-2">
                        <div className="checkout-items">
                            <h2>Состав заказа ({cart.length})</h2>
                            {loading ? (
                                <p className="checkout-status">Загрузка...</p>
                            ) : error ? (
                                <p className="checkout-error-text">Ошибка: {error}</p>
                            ) : cart.length === 0 ? (
                                <p className="checkout-status">Корзина пуста</p>
                            ) : (
                                <div className="items-list">
                                    {cart.map((item) => (
                                        <CheckoutItemCard
                                            key={item.orderItemId}
                                            item={item}
                                            changeQuantity={changeQuantity}
                                            removeFromCart={removeFromCart}
                                        />
                                    ))}
                                </div>
                            )}
                        </div>
                    </div>
                </div>
            </div>
        </div>
    );
};

export default CheckoutPage;
