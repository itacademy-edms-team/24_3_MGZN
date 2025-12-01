// src/pages/PaymentPage/PaymentPage.js
import React, { useState } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import './PaymentPage.css'; // Создайте, если нужно

const PaymentPage = () => {
    const navigate = useNavigate();
    const location = useLocation();

    // Получаем данные заказа, если они переданы через state
    const orderData = location.state?.orderData || null;

    // Состояния для данных карты
    const [cardNumber, setCardNumber] = useState('');
    const [expiryDate, setExpiryDate] = useState('');
    const [cvv, setCvv] = useState('');
    const [cardholderName, setCardholderName] = useState('');

    const handlePayment = () => {
        // Здесь будет логика оплаты (например, вызов API)
        // Проверим, все ли поля заполнены
        if (!cardNumber || !expiryDate || !cvv || !cardholderName) {
            alert('Пожалуйста, заполните все поля формы.');
            return;
        }

        // Простая валидация номера карты (16 цифр)
        const cleanCardNumber = cardNumber.replace(/\s/g, '');
        if (!/^\d{16}$/.test(cleanCardNumber)) {
            alert('Номер карты должен содержать 16 цифр.');
            return;
        }

        // Простая валидация CVV (3 цифры)
        if (!/^\d{3}$/.test(cvv)) {
            alert('CVV должен содержать 3 цифры.');
            return;
        }

        // Простая валидация срока действия (MM/YY)
        if (!/^\d{2}\/\d{2}$/.test(expiryDate)) {
            alert('Срок действия должен быть в формате ММ/ГГ.');
            return;
        }

        // Пример отправки данных на сервер (заглушка)
        console.log({
            orderData,
            cardNumber: cleanCardNumber,
            expiryDate,
            cvv,
            cardholderName
        });

        alert('Платёж обрабатывается...');
        // После оплаты можно перенаправить на страницу успеха или уведомления
        // navigate('/payment-success');
    };

    const handleCancel = () => {
        // Возврат на страницу заказа или на главную
        navigate(-1);
    };

    return (
        <div className="payment-page">
            <div className="payment-container">
                <h1>Оплата заказа</h1>

                {orderData && (
                    <div className="order-summary">
                        <p><strong>Номер заказа:</strong> #{orderData.sessionId || 'N/A'}</p>
                        <p><strong>Сумма к оплате:</strong> {(orderData.orderTotalAmount || 0).toFixed(2)} ₽</p>
                    </div>
                )}

                <div className="payment-form">
                    <h2>Введите данные карты</h2>

                    <div className="form-group">
                        <label htmlFor="cardNumber">Номер карты</label>
                        <input
                            type="text"
                            id="cardNumber"
                            value={cardNumber}
                            onChange={(e) => {
                                // Оставляем только цифры и форматируем с пробелами
                                const value = e.target.value.replace(/\D/g, '').slice(0, 16);
                                const formattedValue = value.replace(/(\d{4})/g, '$1 ').trim();
                                setCardNumber(formattedValue);
                            }}
                            placeholder="1234 5678 9012 3456"
                            maxLength="19" // 16 цифр + 3 пробела
                        />
                    </div>

                    <div className="form-row">
                        <div className="form-group">
                            <label htmlFor="expiryDate">Срок действия (ММ/ГГ)</label>
                            <input
                                type="text"
                                id="expiryDate"
                                value={expiryDate}
                                onChange={(e) => {
                                    // Форматируем ввод: ММ/ГГ
                                    let value = e.target.value.replace(/\D/g, '').slice(0, 4);
                                    if (value.length >= 3) {
                                        value = value.substring(0, 2) + '/' + value.substring(2, 4);
                                    }
                                    setExpiryDate(value);
                                }}
                                placeholder="ММ/ГГ"
                                maxLength="5"
                            />
                        </div>

                        <div className="form-group">
                            <label htmlFor="cvv">CVV</label>
                            <input
                                type="text"
                                id="cvv"
                                value={cvv}
                                onChange={(e) => {
                                    const value = e.target.value.replace(/\D/g, '').slice(0, 3);
                                    setCvv(value);
                                }}
                                placeholder="123"
                                maxLength="3"
                            />
                        </div>
                    </div>

                    <div className="form-group">
                        <label htmlFor="cardholderName">Имя держателя карты</label>
                        <input
                            type="text"
                            id="cardholderName"
                            value={cardholderName}
                            onChange={(e) => setCardholderName(e.target.value)}
                            placeholder="Иван Иванов"
                        />
                    </div>

                    <div className="payment-actions">
                        <button className="pay-button" onClick={handlePayment}>Оплатить</button>
                        <button className="cancel-button" onClick={handleCancel}>Отмена</button>
                    </div>
                </div>
            </div>
        </div>
    );
};

export default PaymentPage;