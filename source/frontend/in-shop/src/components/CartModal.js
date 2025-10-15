// CartModal.js
import React, { useContext, useState, useEffect } from 'react';
import { CartContext } from '../components/CartContext';
import CartItem from '../components/CartItem'; // Импортируем оптимизированную карточку
import './CartModal.css';

const CartModal = () => {
    const { isCartOpen, closeCart, cart, loading, error, clearCart } = useContext(CartContext);
    
    // Состояние для анимации закрытия
    const [isClosing, setIsClosing] = useState(false);

    // Подсчет общей суммы заказа
    const totalAmount = cart.reduce((total, item) => total + (item.productPrice * item.quantity), 0);

    // Обработчик закрытия с анимацией
    const handleClose = () => {
        setIsClosing(true);
    };

    // После завершения анимации закрытия - действительно закрываем модалку
    useEffect(() => {
        if (isClosing) {
            const timer = setTimeout(() => {
                closeCart();
                setIsClosing(false);
            }, 300);

            return () => clearTimeout(timer);
        }
    }, [isClosing, closeCart]);

    // Если модалка полностью закрыта - не рендерим её
    if (!isCartOpen && !isClosing) return null;

    return (
        <div className="cart-modal-overlay" onClick={handleClose}>
            <div 
                className={`cart-modal ${isClosing ? 'slide-out' : ''}`} 
                onClick={(e) => e.stopPropagation()}
            >
                <div className="cart-header">
                    <h2>Корзина</h2>
                    <button className="cart-close-btn" onClick={handleClose}>
                        ×
                    </button>
                </div>

                <div className="cart-content">
                    {loading && <p>Загрузка корзины...</p>}
                    {error && <p className="error-message">Ошибка: {error}</p>}
                    
                    {!loading && !error && cart.length === 0 ? (
                        <p className="empty-cart-message">Корзина пуста.</p>
                    ) : (
                        <>
                            <div className="cart-items">
                                {/* Используем оптимизированную карточку */}
                                {cart.map((item) => (
                                    <CartItem key={item.orderItemId} item={item} />
                                ))}
                            </div>
                            
                            <div className="cart-footer">
                                <div className="cart-total">
                                    <span>Итого:</span>
                                    <strong>{totalAmount.toFixed(2)} ₽</strong>
                                </div>
                                
                                <div className="cart-actions">
                                    <button onClick={clearCart} className="clear-cart-button">
                                        Очистить корзину
                                    </button>
                                    <button className="checkout-button">
                                        Оформить заказ
                                    </button>
                                </div>
                            </div>
                        </>
                    )}
                </div>
            </div>
        </div>
    );
};

export default CartModal;