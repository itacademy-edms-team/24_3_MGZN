// CartModal.js
import React, { useContext, useState, useEffect } from 'react';
import { CartContext } from '../components/CartContext';
import CartItem from '../components/CartItem';
import './CartModal.css';
import { Link } from 'react-router-dom';

const CartModal = () => {
    const { isCartOpen, closeCart, cart, loading, error, clearCart } = useContext(CartContext);

    const [isClosing, setIsClosing] = useState(false);

    const totalAmount = cart.reduce((total, item) => total + (item.productPrice * item.quantity), 0);
    const showInitialLoading = loading && cart.length === 0;
    const showEmptyState = !loading && !error && cart.length === 0;
    const showCartItems = cart.length > 0;

    const handleClose = () => {
        setIsClosing(true);
    };

    useEffect(() => {
        if (isClosing) {
            const timer = setTimeout(() => {
                closeCart();
                setIsClosing(false);
            }, 300);

            return () => clearTimeout(timer);
        }
    }, [isClosing, closeCart]);

    useEffect(() => {
        if (!isCartOpen && !isClosing) return undefined;

        const prevOverflow = document.body.style.overflow;
        document.body.style.overflow = 'hidden';

        return () => {
            document.body.style.overflow = prevOverflow;
        };
    }, [isCartOpen, isClosing]);

    if (!isCartOpen && !isClosing) return null;

    return (
        <div
            className={`cart-modal-overlay${isClosing ? ' cart-modal-overlay--closing' : ''}`}
            onClick={handleClose}
        >
            <div
                className={`cart-modal${isClosing ? ' cart-modal--closing' : ''}`}
                onClick={(e) => e.stopPropagation()}
                role="dialog"
                aria-modal="true"
                aria-label="Корзина"
            >
                <div className="cart-header">
                    <h2>Корзина</h2>
                    <button type="button" className="cart-close-btn" onClick={handleClose} aria-label="Закрыть корзину">
                        ×
                    </button>
                </div>

                <div className="cart-content">
                    {error && <p className="error-message">Ошибка: {error}</p>}

                    {showInitialLoading && (
                        <p className="cart-loading-message">Загрузка корзины…</p>
                    )}

                    {showEmptyState && (
                        <p className="empty-cart-message">Корзина пуста.</p>
                    )}

                    {showCartItems && (
                        <>
                            <div className="cart-items">
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
                                    <button type="button" onClick={clearCart} className="clear-cart-button" data-testid="clear-cart-button">
                                        Очистить корзину
                                    </button>
                                    <Link to="/checkout" onClick={closeCart} className="checkout-cart-button">
                                        Оформить заказ
                                    </Link>
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
