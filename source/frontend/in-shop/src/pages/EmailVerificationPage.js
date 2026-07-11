// ============================================
// Файл: src/pages/EmailVerificationPage.js
// ============================================

import React, { useState, useEffect, useCallback, useRef } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import { useSessionContext } from '../context/SessionContext';
import { apiClient } from '../api/client';
import { readCheckoutDraft, saveCompletedOrder } from '../utils/checkoutStorage';
import './EmailVerificationPage.css';

const EmailVerificationPage = () => {
    const [code, setCode] = useState(['', '', '', '']);
    const [error, setError] = useState('');
    const [loading, setLoading] = useState(false);
    
    const navigate = useNavigate();
    const location = useLocation();
    const submittedRef = useRef(false);
    
    // ✅ Используем useSession
    const { 
        orderId, 
        isValid, 
        isLoading: sessionLoading
    } = useSessionContext();

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
        if (submittedRef.current || loading) {
            return;
        }

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
        submittedRef.current = true;

        try {
            // 1. Проверка кода
            await apiClient.post('/Verification/validate-code', {
                email,
                code: codeString
            });

            // 2. Подготовка данных заказа
            const orderData = orderDataFromState || readCheckoutDraft();
            
            if (!orderData) {
                throw new Error('Данные заказа не найдены');
            }

            const validatedOrderData = {
                shipCompanyId: orderData.shipCompanyId ? parseInt(orderData.shipCompanyId) : 1,
                shipAddress: orderData.shipAddress,
                shipMethod: orderData.shipMethod,
                payMethod: orderData.payMethod,
                customerFullname: orderData.customerFullName || orderData.customerFullname,
                customerEmail: orderData.customerEmail || email,
                customerPhoneNumber: orderData.customerPhoneNumber,
                orderItems: (orderData.orderItems || []).map(item => ({
                    productId: item.productId,
                    quantityItem: item.quantityItem
                }))
            };

            // 3. Оформление заказа
            const checkoutResponse = await apiClient.post('/Order/checkout', validatedOrderData);
            const checkoutData = checkoutResponse.data;
            
            const completedOrderId = checkoutData?.orderId;
            if (!completedOrderId) {
                throw new Error('Сервер не вернул номер оформленного заказа.');
            }

            const completedOrderData = {
                ...orderData,
                ...checkoutData,
                orderId: completedOrderId,
            };

            saveCompletedOrder(completedOrderData);

            // 4. Переход на страницу успеха
            navigate('/order-success', { 
                state: { 
                    completedOrderId,
                    orderData: completedOrderData
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
            submittedRef.current = false;
        } finally {
            setLoading(false);
        }
    }, [code, email, orderId, isValid, orderDataFromState, navigate, loading]);

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