// ============================================
// Файл: src/components/CartContext.js
// ============================================

import React, { createContext, useState, useEffect, useCallback } from 'react';
import { apiClient } from '../api/client.ts';
import { useSessionContext } from '../context/SessionContext.tsx';

export const CartContext = createContext();

export const CartProvider = ({ children }) => {
    const [cart, setCart] = useState([]);
    const [isCartOpen, setIsCartOpen] = useState(false);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState(null);
    
    // ✅ Получаем данные сессии из хука
    const { orderId, isValid } = useSessionContext();

    // Загрузка корзины из бэкенда
    const fetchCart = useCallback(async () => {
        // Если сессия не валидна или нет orderId — не загружаем
        if (!isValid || !orderId) {
            console.log('[Cart] Session not ready, skipping fetch');
            return;
        }
        
        try {
            setLoading(true);
            setError(null);
            
            // ✅ SessionId НЕ передаём — бэкенд берёт из cookie
            const response = await apiClient.get(`/Order/cart`, {
                params: { orderId } // Если бэкенд поддерживает фильтр по orderId
            });
            
            setCart(response.data);
        } catch (err) {
            console.error('[Cart] Error fetching cart:', err);
            setError(err.response?.data?.message || err.message || 'Не удалось загрузить корзину.');
            
            // Если 401 — сессия истекла, хук useSession автоматически пересоздаст
            if (err.response?.status === 401) {
                console.log('[Cart] Session expired, will be recreated by useSession hook');
            }
        } finally {
            setLoading(false);
        }
    }, [isValid, orderId]);

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
        if (!isValid || !orderId) {
            alert('Пожалуйста, подождите инициализации сессии');
            return;
        }
        
        try {
            // ✅ SessionId НЕ передаём — бэкенд берёт из cookie
            const response = await apiClient.post('/Order', {
                productId: product.productId,
                orderId: orderId, // Если бэкенд принимает orderId вместо sessionId
                quantity: 1
            });

            // Перезагружаем корзину
            await fetchCart();
            
            return response.data;
        } catch (error) {
            console.error('[Cart] Error adding to cart:', error);
            
            if (error.response?.status === 401) {
                alert('Сессия истекла, страница будет перезагружена...');
                window.location.reload();
            } else {
                alert('Не удалось добавить товар.');
            }
            
            throw error;
        }
    }, [isValid, orderId, fetchCart]);

    // Изменение количества товара
    const changeQuantity = useCallback(async (orderItemId, newQuantity) => {
        if (!isValid) {
            alert('Сессия не активна');
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
                alert('Сессия истекла, страница будет перезагружена...');
                window.location.reload();
            } else {
                alert('Не удалось обновить количество.');
            }
        }
    }, [isValid]);

    // Удаление товара из корзины
    const removeFromCart = useCallback(async (orderItemId) => {
        if (!isValid) {
            alert('Сессия не активна');
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
                alert('Сессия истекла, страница будет перезагружена...');
                window.location.reload();
            } else {
                alert('Не удалось удалить товар.');
            }
        }
    }, [isValid]);

    // Очистка корзины
    const clearCart = useCallback(async () => {
        if (!isValid || !orderId) {
            alert('Сессия не активна');
            return;
        }
        
        try {
            // ✅ SessionId НЕ передаём
            await apiClient.delete(`/Order/clear`, {
                params: { orderId }
            });

            setCart([]);
        } catch (error) {
            console.error('[Cart] Error clearing cart:', error);
            
            if (error.response?.status === 401) {
                alert('Сессия истекла, страница будет перезагружена...');
                window.location.reload();
            } else {
                alert('Не удалось очистить корзину.');
            }
        }
    }, [isValid, orderId]);

    // Эффект: загружаем корзину при изменении orderId
    useEffect(() => {
        if (isValid && orderId && isCartOpen) {
            fetchCart();
        }
    }, [isValid, orderId, isCartOpen, fetchCart]);

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