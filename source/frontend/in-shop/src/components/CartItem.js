// CartItem.js
import React, { memo, useContext } from 'react';
import { CartContext } from '../components/CartContext';

const CartItem = memo(({ item }) => {
    const { changeQuantity, removeFromCart } = useContext(CartContext);

    return (
        <div key={item.orderItemId} className="cart-item-card">
            <img
                src={`https://localhost:7275${item.imageUrl}`}
                alt={item.productName}
                onError={(e) => {
                    e.target.src = 'https://localhost:7275/images/placeholder.svg';
                }}
                loading="lazy"
                className="cart-item-image"
            />
            
            <div className="cart-item-info">
                <h4>{item.productName}</h4>
                <p className="cart-item-price">Цена: {item.productPrice} ₽</p>
                
                <div className="quantity-controls">
                    <button 
                        className="qty-btn"
                        onClick={() => changeQuantity(item.orderItemId, Math.max(1, item.quantity - 1))}
                        disabled={item.quantity <= 1}
                    >
                        -
                    </button>
                    <span className="qty-value">{item.quantity}</span>
                    <button 
                        className="qty-btn"
                        onClick={() => changeQuantity(item.orderItemId, item.quantity + 1)}
                    >
                        +
                    </button>
                </div>
                
                <button 
                    className="remove-btn"
                    onClick={() => removeFromCart(item.orderItemId)}
                >
                    Удалить
                </button>
            </div>
        </div>
    );
});

export default CartItem;