import React, { useContext } from 'react';
import { Link } from 'react-router-dom';
import { CartContext } from '../components/CartContext'; // Импортируем контекст корзины
import './CartContext.css';
import './Header.css';

const Header = () => {
    const { cart, isCartOpen, openCart, closeCart, removeFromCart, clearCart } = useContext(CartContext);
    const totalQuantity = cart.reduce((total, item) => total + item.quantity, 0);

    return (
        <header className="header">
            <div className="header-content">
                <Link to="/" className="logo">
                    <h1>.InShop</h1>
                </Link>
            </div>

            {/* Иконка корзины */}
            <div className="cart-icon" onClick={openCart}>
                <img src="/cart-icon.png" alt="Корзина" />
                <span className="cart-count">{totalQuantity}</span>
            </div>

            {/* Модальное окно корзины */}
            {isCartOpen && (
                <div className="cart-modal-overlay" onClick={closeCart}>
                    <div className="cart-modal" onClick={(e) => e.stopPropagation()}>
                        <h2>Корзина</h2>
                        {cart.length === 0 ? (
                            <p className="empty-cart-message">Корзина пуста.</p>
                        ) : (
                            <>
                                <ul>
                                    {cart.map((item) => (
                                        <li key={item.productId} className="cart-item">
                                            <img
                                                src={`https://localhost:7275${item.imageUrl}`} 
                                                alt={item.productName}
                                                onError={(e) => {
                                                    e.target.src = 'https://localhost:7275/images/placeholder.svg';
                                                }}
                                                loading="lazy"
                                                style={{ width: '80px', height: '80px', marginRight: '10px' }}
                                            />
                                            <div className="cart-item-details">
                                                <h4>{item.productName}</h4>
                                                <p>Цена: {item.productPrice} ₽</p>
                                                <p>Количество: {item.quantity}</p>
                                                <button onClick={() => removeFromCart(item.productId)}>Удалить</button>
                                            </div>
                                        </li>
                                    ))}
                                </ul>
                                {/* Блок с кнопками отображается только если корзина не пуста */}
                                <div className="cart-actions">
                                    <button onClick={clearCart} className='clear-cart-button'>Очистить корзину</button>
                                </div>
                            </>
                        )}
                    </div>
                </div>
            )}
        </header>
    );
};

export default Header;