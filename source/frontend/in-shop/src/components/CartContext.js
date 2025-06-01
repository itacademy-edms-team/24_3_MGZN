// CartContext.js
import React, { createContext, useState, useEffect } from 'react';
import './CartContext.css';

export const CartContext = createContext();

export const CartProvider = ({ children }) => {
    // Состояние корзины
    const [cart, setCart] = useState([]);
    // Состояние видимости модального окна
    const [isCartOpen, setIsCartOpen] = useState(false);

    // Загрузка корзины из localStorage
    useEffect(() => {
        const savedCart = JSON.parse(localStorage.getItem('cart')) || [];
        setCart(savedCart);
    }, []);

    // Сохранение корзины в localStorage
    useEffect(() => {
        localStorage.setItem('cart', JSON.stringify(cart));
    }, [cart]);

    // Добавление товара в корзину
    const addToCart = (product) => {
        const existingProduct = cart.find((item) => item.productId === product.productId);

        if (existingProduct) {
            setCart(
                cart.map((item) =>
                    item.productId === product.productId
                        ? { ...item, quantity: item.quantity + 1 }
                        : item
                )
            );
        } else {
            setCart([...cart, { ...product, quantity: 1 }]);
        }
    };

    // Удаление товара из корзины
    const removeFromCart = (productId) => {
        setCart(cart.filter((item) => item.productId !== productId));
    };

    // Очистка корзины
    const clearCart = () => {
        setCart([]);
    };

    return (
        <CartContext.Provider
            value={{
                cart,
                addToCart,
                removeFromCart,
                clearCart,
                isCartOpen,
                openCart: () => setIsCartOpen(true),
                closeCart: () => setIsCartOpen(false),
            }}
        >
            {children}
        </CartContext.Provider>
    );
};