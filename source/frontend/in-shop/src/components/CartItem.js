// CartItem.js
import React, { memo, useContext } from 'react';
import { CartContext } from '../components/CartContext';
import { resolveAssetUrl, PRODUCT_PLACEHOLDER_URL } from '../config/api.js';

const CartItem = memo(({ item }) => {
    const { changeQuantity, removeFromCart } = useContext(CartContext);

    return (
        <div key={item.orderItemId} className="cart-item-card">
            <img
                src={resolveAssetUrl(item.imageUrl) || PRODUCT_PLACEHOLDER_URL}
                alt={item.productName}
                onError={(e) => {
                    e.target.src = PRODUCT_PLACEHOLDER_URL;
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
                        data-testid="cart-remove-item"
                        onClick={() => removeFromCart(item.orderItemId)}
                    >
                    Удалить
                </button>
            </div>
        </div>
    );
});

export default CartItem;