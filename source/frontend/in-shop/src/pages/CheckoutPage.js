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

    // Обработчик отправки формы
    const handleSubmit = async (formData) => {
        try {
            const sessionId = localStorage.getItem('sessionId');
            if (!sessionId) throw new Error('SessionId не найден.');

            // Здесь вы отправляете данные на бэкенд
            await fetch('https://localhost:7275/api/Order/checkout', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({
                    sessionId,
                    ...formData,
                    items: cart.map(item => ({
                        productId: item.productId,
                        quantity: item.quantity,
                        price: item.productPrice
                    })),
                    totalAmount
                })
            });

            // После успешной отправки перенаправляем на страницу подтверждения
            alert('Заказ успешно оформлен!');
            navigate('/order-success');
        } catch (err) {
            console.error('Ошибка оформления заказа:', err);
            alert('Не удалось оформить заказ.');
        }
    };

    return (
        <div className="checkout-page">
            <h1>Оформление заказа</h1>
            <div className="checkout-container">
                
                {/* Левая колонка: Форма и список товаров */}
                <div className="checkout-left">
                    {/* Форма оформления заказа */}
                    <CheckoutForm onSubmit={handleSubmit} />
                    
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
                    <div className="summary-box">
                        <h3>Стоимость заказа</h3>
                        <div className="summary-row">
                            <span>Товары ({cart.length})</span>
                            <span>{totalAmount.toFixed(2)} ₽</span>
                        </div>
                        <div className="summary-row">
                            <span>Доставка</span>
                            <span>1500 ₽</span>
                        </div>
                        <div className="summary-row">
                            <span>Скидка</span>
                            <span>-0 ₽</span>
                        </div>
                        <div className="summary-row">
                            <span>Итого</span>
                            <strong>{(totalAmount + 1500).toFixed(2)} ₽</strong>
                        </div>
                    </div>
                    <button className="checkout-button" onClick={() => document.querySelector('form').dispatchEvent(new Event('submit'))}>
                        Оформить заказ
                    </button>
                </div>
            </div>
        </div>
    );
};

export default CheckoutPage;