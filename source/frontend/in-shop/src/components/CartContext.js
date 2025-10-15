// CartContext.js
import React, { createContext, useState, useEffect } from 'react';
import axios from 'axios';

export const CartContext = createContext();

export const CartProvider = ({ children }) => {
    const [cart, setCart] = useState([]); // Состояние корзины
    const [isCartOpen, setIsCartOpen] = useState(false); // Состояние видимости модального окна
    const [loading, setLoading] = useState(false); // Состояние загрузки
    const [error, setError] = useState(null); // Состояние ошибки

    // Загрузка корзины из бэкенда
    const fetchCart = async () => {
        try {
            setLoading(true);
            const sessionId = localStorage.getItem('sessionId');
            if (!sessionId) {
                throw new Error('SessionId не найден.');
            }

            const response = await axios.get(`https://localhost:7275/api/Order/cart?sessionId=${encodeURIComponent(sessionId)}`);
            setCart(response.data);
            setError(null);
        } catch (err) {
            console.error('Ошибка загрузки корзины:', err);
            setError(err.message || 'Не удалось загрузить корзину.');
        } finally {
            setLoading(false);
        }
    };

    // Открытие модального окна корзины
    const openCart = () => {
        setIsCartOpen(true);
        fetchCart(); // Загружаем корзину при открытии
    };

    // Закрытие модального окна корзины
    const closeCart = () => {
        setIsCartOpen(false);
    };

    // Функция для добавления товара в корзину
    const addToCart = async (product) => {
        try {
            const sessionId = localStorage.getItem('sessionId');
            if (!sessionId) throw new Error('SessionId не найден.');

            const response = await axios.post('https://localhost:7275/api/Order', {
                productId: product.productId,
                sessionId: parseInt(sessionId, 10)
            });

            // После добавления товара перезагружаем корзину
            await fetchCart();
            
        } catch (error) {
            console.error('Ошибка добавления товара:', error);
            alert('Не удалось добавить товар.');
        }
    };

    // Функция изменения количества товара
    const changeQuantity = async (orderItemId, newQuantity) => {
        try {
            const sessionId = localStorage.getItem('sessionId');
            if (!sessionId) throw new Error('SessionId не найден.');

            // Отправляем запрос на бэкенд для обновления количества
            await axios.put('https://localhost:7275/api/Order/updateQuantity', {
                orderItemId,
                quantity: newQuantity
            });

            // Обновляем локальное состояние корзины БЕЗ перезагрузки
            setCart((prevCart) =>
                prevCart.map((item) =>
                    item.orderItemId === orderItemId
                        ? { ...item, quantity: newQuantity }
                        : item
                )
            );

            // Пересчитываем итоговую сумму заказа (если нужно)
            // Это можно сделать на бэкенде или локально
        } catch (error) {
            console.error('Ошибка обновления количества:', error);
            alert('Не удалось обновить количество.');
        }
    };

    // Функция для удаления товара из корзины
    const removeFromCart = async (orderItemId) => {
        try {
            const sessionId = localStorage.getItem('sessionId');
            if (!sessionId) throw new Error('SessionId не найден.');

            await axios.delete(`https://localhost:7275/api/Order/${orderItemId}`);

            // Обновляем локальное состояние корзины БЕЗ перезагрузки
            setCart((prevCart) => prevCart.filter((item) => item.orderItemId !== orderItemId));

        } catch (error) {
            console.error('Ошибка удаления товара:', error);
            alert('Не удалось удалить товар.');
        }
    };

    // Функция для очистки корзины
    const clearCart = async () => {
        try {
            const sessionId = localStorage.getItem('sessionId');
            if (!sessionId) throw new Error('SessionId не найден.');

            // Отправляем запрос на бэкенд для очистки корзины
            await axios.delete(`https://localhost:7275/api/Order/clear?sessionId=${parseInt(sessionId, 10)}`);

            // Очищаем локальное состояние корзины
            setCart([]);
        } catch (error) {
            console.error('Ошибка очистки корзины:', error);
            alert('Не удалось очистить корзину.');
        }
    };

    return (
        <CartContext.Provider
            value={{
                cart,
                isCartOpen,
                loading,
                error,
                openCart,
                closeCart,
                addToCart,
                removeFromCart,
                clearCart,
                changeQuantity,
                fetchCart
            }}
        >
            {children}
        </CartContext.Provider>
    );
};