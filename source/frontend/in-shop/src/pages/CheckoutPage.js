// src/pages/CheckoutPage.js
import React, { useContext, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { CartContext } from '../components/CartContext';
import CheckoutItemCard from '../components/CheckoutItemCard';
import CheckoutForm from '../components/CheckoutForm';
import './CheckoutPage.css';
import '../components/CheckoutItemCard.css';

const CheckoutPage = () => {
    const { cart, loading, error, changeQuantity, removeFromCart } = useContext(CartContext);
    const navigate = useNavigate();

    // Общая сумма заказа
    const totalAmount = cart.reduce((sum, item) => sum + (item.productPrice * item.quantity), 0);

    

    return (
        <div className="checkout-page">
            <h1>Оформление заказа</h1>
            <div className="checkout-container">
                
                {/* Левая колонка: Форма и список товаров */}
                <div className="checkout-left">
                    {/* Форма оформления заказа */}
                    <CheckoutForm />
                    
                </div>
                {/* Правая колонка: Итоговая стоимость */}
                <div className="checkout-right">
                    
                    {/* Список товаров */}
                    <div className="checkout-items">
                        <h2>Состав заказа ({cart.length})</h2>
                        {loading ? (
                            <p>Загрузка...</p>
                        ) : error ? (
                            <p className="error-message">Ошибка: {error}</p>
                        ) : cart.length === 0 ? (
                            <p className="empty-cart">Корзина пуста</p>
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
    );
};

export default CheckoutPage;