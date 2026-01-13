// src/pages/PaymentConfirmationPage.js
import React, { useState, useEffect } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import './PaymentConfirmationPage.css';

const PaymentConfirmationPage = () => {
    const navigate = useNavigate();
    const location = useLocation();

    const orderId = location.state?.orderId;

    console.log("PaymentConfirmationPage: received orderId:", orderId);

    // useEffect для проверки orderId и навигации
    useEffect(() => {
        if (!orderId) {
            console.log("PaymentConfirmationPage: No orderId, navigating to /");
            alert('Неверные данные для подтверждения оплаты.');
            navigate('/');
        }
    }, [orderId, navigate]);

    // Все хуки вызываются на верхнем уровне
    const [status, setStatus] = useState('checking');
    const [error, setError] = useState(null);

    // useEffect для опроса статуса
    useEffect(() => {
        if (!orderId) {
            console.log("PaymentConfirmationPage: useEffect - no orderId, returning");
            return;
        }

        console.log("PaymentConfirmationPage: Starting status polling for orderId:", orderId);

        let intervalId;

        const checkPaymentStatus = async () => {
            try {
                console.log("PaymentConfirmationPage: Fetching status for orderId:", orderId);
                const response = await fetch(`https://localhost:7275/api/Payment/status/${orderId}`, {
                    method: 'GET',
                    headers: { 'Content-Type': 'application/json' },
                });

                if (!response.ok) {
                    throw new Error(`Ошибка получения статуса: ${response.status}`);
                }

                const data = await response.json();
                console.log("PaymentConfirmationPage: API response data:", data);

                const currentStatus = data?.status;
                console.log("PaymentConfirmationPage: currentStatus:", currentStatus);

                if (!currentStatus) {
                    throw new Error(`Поле Status отсутствует в ответе: ${JSON.stringify(data)}`);
                }

                if (currentStatus === 'Payed') {
                    setStatus('paid');
                    if (intervalId) clearInterval(intervalId);
                } else if (currentStatus === 'Unpayed') {
                    setStatus('checking');
                } else {
                    throw new Error(`Неизвестный статус: ${currentStatus}`);
                }
            } catch (err) {
                console.error('Ошибка проверки статуса оплаты:', err);
                setError(err.message);
                setStatus('error');
                if (intervalId) clearInterval(intervalId);
            }
        };

        intervalId = setInterval(checkPaymentStatus, 1000);
        checkPaymentStatus();

        return () => {
            if (intervalId) clearInterval(intervalId);
        };
    }, [orderId, navigate]); // Указываем orderId как зависимость

    // Рендерим компонент в зависимости от orderId и status
    if (!orderId) {
        console.log("PaymentConfirmationPage: Render - no orderId, returning null/div");
        return <div>Загрузка...</div>; // или null
    }

    if (status === 'error') {
        return (
            <div className="payment-confirmation-page">
                <div className="confirmation-container">
                    <h1>Ошибка проверки оплаты</h1>
                    <p>Произошла ошибка: {error}</p>
                    <button className="back-button" onClick={() => navigate(-1)}>Назад</button>
                </div>
            </div>
        );
    }

    if (status === 'paid') {
        return (
            <div className="payment-confirmation-page">
                <div className="confirmation-container">
                    <div className="success-icon">✓</div>
                    <h1>Заказ успешно оплачен</h1>
                    <p>Номер заказа: #{orderId}</p>
                    <button className="done-button" onClick={() => navigate('/')}>На главную</button>
                </div>
            </div>
        );
    }

    // Статус 'checking'
    return (
        <div className="payment-confirmation-page">
            <div className="confirmation-container">
                <div className="loading-spinner"></div>
                <h1>Проверка статуса оплаты</h1>
                <p>Заказ #{orderId}</p>
                <p>Пожалуйста, подождите...</p>
            </div>
        </div>
    );
};

export default PaymentConfirmationPage;