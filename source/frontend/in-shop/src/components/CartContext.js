// ============================================
// Файл: src/components/CartContext.js
// ============================================

import React, { createContext, useState, useEffect, useCallback } from 'react';
import { apiClient } from '../api/client';
import { useSessionContext } from '../context/SessionContext';

export const CartContext = createContext();

const isDevelopment = process.env.NODE_ENV === 'development';

export const CartProvider = ({ children }) => {
    const [cart, setCart] = useState([]);
    const [isCartOpen, setIsCartOpen] = useState(false);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState(null);
    
    // ✅ Получаем данные сессии из хука
    const { orderId, isValid, updateOrderId } = useSessionContext();

    // Загрузка корзины из бэкенда
    const fetchCart = useCallback(async () => {
        // Бэкенд определяет текущую корзину по HttpOnly cookie сессии.
        if (!isValid) {
            if (isDevelopment) {
                console.log('[Cart] Session not ready, skipping fetch');
            }
            return;
        }
        
        try {
            setLoading(true);
            setError(null);
            
            // ✅ SessionId НЕ передаём — бэкенд берёт из cookie
            const response = await apiClient.get(`/Order/cart`);
            
            setCart(response.data);
        } catch (err) {
            console.error('[Cart] Error fetching cart:', err);
            setError(err.response?.data?.message || err.message || 'Не удалось загрузить корзину.');
            
            // Если 401 — сессия истекла, хук useSession автоматически пересоздаст
            if (err.response?.status === 401) {
                if (isDevelopment) {
                    console.log('[Cart] Session expired, will be recreated by useSession hook');
                }
            }
        } finally {
            setLoading(false);
        }
    }, [isValid]);

    // Открытие модального окна корзины
    const openCart = useCallback(() => {
        setIsCartOpen(true);
        fetchCart();
    }, [fetchCart]);

    // Закрытие модального окна корзины
    const closeCart = useCallback(() => {
        setIsCartOpen(false);
    }, []);

    // Добавление товара в корзину
    const addToCart = useCallback(async (product) => {
        if (!isValid) {
            setError('Пожалуйста, подождите инициализации сессии');
            return;
        }
        
        try {
            // ✅ SessionId НЕ передаём — бэкенд берёт из cookie
            const response = await apiClient.post('/Order', {
                productId: product.productId,
                quantity: 1
            });

            if (typeof response.data?.orderId === 'number') {
                updateOrderId(response.data.orderId);
            }

            // Перезагружаем корзину
            await fetchCart();
            
            return response.data;
        } catch (error) {
            console.error('[Cart] Error adding to cart:', error);
            
            if (error.response?.status === 401) {
                setError('Сессия истекла. Обновите страницу и попробуйте снова.');
            } else {
                setError('Не удалось добавить товар.');
            }
            
            throw error;
        }
    }, [isValid, fetchCart, updateOrderId]);

    // Изменение количества товара
    const changeQuantity = useCallback(async (orderItemId, newQuantity) => {
        if (!isValid) {
            setError('Сессия не активна');
            return;
        }
        
        try {
            // ✅ SessionId НЕ передаём
            await apiClient.put('/Order/updateQuantity', {
                orderItemId,
                quantity: newQuantity
            });

            // Обновляем локальное состояние
            setCart((prevCart) =>
                prevCart.map((item) =>
                    item.orderItemId === orderItemId
                        ? { ...item, quantity: newQuantity }
                        : item
                )
            );
        } catch (error) {
            console.error('[Cart] Error updating quantity:', error);
            
            if (error.response?.status === 401) {
                setError('Сессия истекла. Обновите страницу и попробуйте снова.');
            } else {
                setError('Не удалось обновить количество.');
            }
        }
    }, [isValid]);

    // Удаление товара из корзины
    const removeFromCart = useCallback(async (orderItemId) => {
        if (!isValid) {
            setError('Сессия не активна');
            return;
        }
        
        try {
            // ✅ SessionId НЕ передаём
            await apiClient.delete(`/Order/${orderItemId}`);

            // Обновляем локальное состояние
            setCart((prevCart) => prevCart.filter((item) => item.orderItemId !== orderItemId));
        } catch (error) {
            console.error('[Cart] Error removing item:', error);
            
            if (error.response?.status === 401) {
                setError('Сессия истекла. Обновите страницу и попробуйте снова.');
            } else {
                setError('Не удалось удалить товар.');
            }
        }
    }, [isValid]);

    // Очистка корзины
    const clearCart = useCallback(async () => {
        if (!isValid) {
            setError('Сессия не активна');
            return;
        }
        
        try {
            // ✅ SessionId НЕ передаём
            await apiClient.delete(`/Order/clear`);

            setCart([]);
        } catch (error) {
            console.error('[Cart] Error clearing cart:', error);
            
            if (error.response?.status === 401) {
                setError('Сессия истекла. Обновите страницу и попробуйте снова.');
            } else {
                setError('Не удалось очистить корзину.');
            }
        }
    }, [isValid]);

    // Эффект: загружаем корзину при готовой сессии, чтобы checkout и badge были актуальны.
    useEffect(() => {
        if (isValid) {
            fetchCart();
        }
    }, [isValid, fetchCart]);

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
                fetchCart,
                orderId,      // ✅ Экспортируем orderId для использования в других компонентах
                isValid,      // ✅ Экспортируем isValid
            }}
        >
            {children}
        </CartContext.Provider>
    );
};