// src/components/CheckoutItemCard.js
import React from 'react';
import './CheckoutItemCard.css';

const CheckoutItemCard = ({ item, changeQuantity, removeFromCart }) => {
    return (
        <div className="checkout-item-card">
            <img
                src={`https://localhost:7275${item.imageUrl}`}
                alt={item.productName}
                onError={(e) => {
                    e.target.src = '/placeholder-image.jpg';
                }}
                loading="lazy"
                className="item-image"
            />
            
            <div className="item-info">
                <h4>{item.productName}</h4>
                <p className="item-price">Цена: {item.productPrice} ₽</p>
                
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
};

export default CheckoutItemCard;