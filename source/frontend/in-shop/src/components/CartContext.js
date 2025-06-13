// CartContext.js

// Импортируем необходимые модули из React
import React, { createContext, useState, useEffect } from 'react';
// Импортируем стили для компонента (если они нужны)
import './CartContext.css';

// Создаем контекст корзины. Этот контекст будет использоваться для передачи данных о корзине между компонентами.
export const CartContext = createContext();

// CartProvider — это провайдер контекста, который оборачивает все дочерние компоненты и предоставляет им доступ к данным корзины.
export const CartProvider = ({ children }) => {
    // Состояние корзины. Изначально корзина пуста (пустой массив).
    const [cart, setCart] = useState([]);

    // Состояние видимости модального окна корзины. По умолчанию окно закрыто (false).
    const [isCartOpen, setIsCartOpen] = useState(false);

    // Эффект загрузки корзины из localStorage при монтировании компонента
    useEffect(() => {
        // Пытаемся получить сохраненную корзину из localStorage.
        // Если данных нет, инициализируем корзину пустым массивом.
        const savedCart = JSON.parse(localStorage.getItem('cart')) || [];
        // Устанавливаем загруженные данные в состояние cart.
        setCart(savedCart);
    }, []); // Этот эффект выполняется только один раз при монтировании компонента.

    // Эффект сохранения корзины в localStorage при каждом изменении состояния cart
    useEffect(() => {
        // Сохраняем текущее состояние корзины в localStorage в виде строки JSON.
        localStorage.setItem('cart', JSON.stringify(cart));
    }, [cart]); // Этот эффект выполняется каждый раз, когда изменяется состояние cart.

    // Функция для добавления товара в корзину
    const addToCart = (product) => {
        // Проверяем, существует ли товар с таким же productId уже в корзине.
        const existingProduct = cart.find((item) => item.productId === product.productId);

        if (existingProduct) {
            // Если товар уже есть в корзине, увеличиваем его количество на 1.
            setCart(
                cart.map((item) =>
                    item.productId === product.productId
                        ? { ...item, quantity: item.quantity + 1 } // Обновляем количество
                        : item // Оставляем остальные товары без изменений
                )
            );
        } else {
            // Если товара еще нет в корзине, добавляем его с количеством 1.
            setCart([...cart, { ...product, quantity: 1 }]);
        }
    };

    // Функция для удаления товара из корзины
    const removeFromCart = (productId) => {
        // Фильтруем массив корзины, исключая товар с указанным productId.
        setCart(cart.filter((item) => item.productId !== productId));
    };

    // Функция для очистки корзины
    const clearCart = () => {
        // Устанавливаем состояние корзины в пустой массив.
        setCart([]);
    };

    // Возвращаем провайдер контекста, предоставляющий доступ к данным и функциям корзины.
    return (
        <CartContext.Provider
            value={{
                cart, // Текущее состояние корзины
                addToCart, // Функция для добавления товара
                removeFromCart, // Функция для удаления товара
                clearCart, // Функция для очистки корзины
                isCartOpen, // Состояние видимости модального окна
                openCart: () => setIsCartOpen(true), // Функция для открытия модального окна
                closeCart: () => setIsCartOpen(false), // Функция для закрытия модального окна
            }}
        >
            {/* Все дочерние компоненты получают доступ к контексту */}
            {children}
        </CartContext.Provider>
    );
};