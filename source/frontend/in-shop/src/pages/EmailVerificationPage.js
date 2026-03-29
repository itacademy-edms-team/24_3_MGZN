// ============================================
// Файл: src/pages/EmailVerificationPage.js
// ============================================

import React, { useState, useEffect, useCallback } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import { useSession } from '../hooks/useSession.ts';
import { apiClient } from '../api/client.ts';
import './EmailVerificationPage.css';

const EmailVerificationPage = () => {
    const [code, setCode] = useState(['', '', '', '']);
    const [error, setError] = useState('');
    const [loading, setLoading] = useState(false);
    
    const navigate = useNavigate();
    const location = useLocation();
    
    // ✅ Используем useSession
    const { 
        orderId, 
        isValid, 
        isLoading: sessionLoading,
        recreateSession 
    } = useSession();

    const email = location.state?.email || '';
    const orderDataFromState = location.state?.orderData;

    // Проверка email при загрузке
    useEffect(() => {
        if (!email) {
            navigate('/checkout');
        }
    }, [email, navigate]);

    const handleChange = useCallback((index, value) => {
        if (/^\d$/.test(value) || value === '') {
            const newCode = [...code];
            newCode[index] = value;
            setCode(newCode);
            if (value && index < 3) {
                document.getElementById(`code-${index + 1}`)?.focus();
            }
        }
    }, [code]);

    const handleKeyDown = useCallback((index, e) => {
        if (e.key === 'Backspace' && !code[index] && index > 0) {
            document.getElementById(`code-${index - 1}`)?.focus();
        }
    }, [code]);

    const handleSubmit = useCallback(async (e) => {
        e.preventDefault();
        const codeString = code.join('');

        if (codeString.length !== 4) {
            setError('Введите 4-значный код');
            return;
        }

        if (!isValid || !orderId) {
            setError('Сессия не активна');
            return;
        }

        setLoading(true);
        setError('');

        try {
            // 1. Проверка кода
            await apiClient.post('/Verification/validate-code', {
                email,
                code: codeString
            });

            // 2. Подготовка данных заказа
            const orderData = orderDataFromState || JSON.parse(localStorage.getItem('orderData'));
            
            if (!orderData) {
                throw new Error('Данные заказа не найдены');
            }

            const validatedOrderData = {
                // ✅ sessionId НЕ передаём — бэкенд берёт из cookie
                orderId: orderId,
                shipCompanyId: orderData.shipCompanyId ? parseInt(orderData.shipCompanyId) : 1,
                shipAddress: orderData.shipAddress,
                shipMethod: orderData.shipMethod,
                payMethod: orderData.payMethod,
                customerFullname: orderData.customerFullName || orderData.customerFullname,
                customerEmail: orderData.customerEmail || email,
                customerPhoneNumber: orderData.customerPhoneNumber,
                orderTotalAmount: orderData.orderTotalAmount,
                orderItems: (orderData.orderItems || []).map(item => ({
                    productId: item.productId,
                    quantityItem: item.quantityItem,
                    price: parseFloat(item.price.toFixed(2))
                }))
            };

            // 3. Оформление заказа
            const checkoutResponse = await apiClient.post('/Order/checkout', validatedOrderData);
            const checkoutData = checkoutResponse.data;
            
            console.log('Заказ оформлен:', checkoutData);

            // 4. ✅ ПЕРЕСОЗДАЁМ СЕССИЮ для нового черновика корзины
            console.log('Recreating session for new cart draft...');
            await recreateSession();

            // 5. Переход на страницу успеха
            navigate('/order-success', { 
                state: { 
                    orderId: checkoutData.orderId,
                    orderData: validatedOrderData
                } 
            });

        } catch (err) {
            console.error('Ошибка оформления заказа:', err);
            
            if (err.response?.status === 401) {
                setError('Сессия истекла. Перезагрузка...');
                setTimeout(() => window.location.reload(), 1500);
            } else {
                setError(err.response?.data?.message || err.message || 'Не удалось оформить заказ.');
            }
        } finally {
            setLoading(false);
        }
    }, [code, email, orderId, isValid, orderDataFromState, navigate, recreateSession]);

    // Лоадер сессии
    if (sessionLoading) {
        return <div className="email-verification-page loading">Инициализация...</div>;
    }

    if (!isValid) {
        return (
            <div className="email-verification-page error">
                <p>⚠️ Сессия не активна</p>
                <button onClick={() => window.location.reload()}>Повторить</button>
            </div>
        );
    }

    return (
        <div className="email-verification-page">
            <h1>Подтверждение почты</h1>
            <p>Код отправлен на: <strong>{email}</strong></p>

            <form onSubmit={handleSubmit} className="verification-form">
                <div className="code-inputs">
                    {code.map((digit, index) => (
                        <input
                            key={index}
                            id={`code-${index}`}
                            type="text"
                            maxLength="1"
                            value={digit}
                            onChange={(e) => handleChange(index, e.target.value)}
                            onKeyDown={(e) => handleKeyDown(index, e)}
                            className="code-input"
                            required
                            disabled={loading}
                        />
                    ))}
                </div>

                {error && <p className="error-message">{error}</p>}

                <button type="submit" className="submit-button" disabled={loading}>
                    {loading ? 'Проверка...' : 'Подтвердить'}
                </button>
            </form>
        </div>
    );
};

export default EmailVerificationPage;