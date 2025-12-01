// src/pages/EmailVerificationPage.js
import React, { useState, useEffect } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import { createUserSession } from '../services/SessionService.ts';
import './EmailVerificationPage.css';

const EmailVerificationPage = () => {
    const [code, setCode] = useState(['', '', '', '']);
    const [error, setError] = useState('');
    const [loading, setLoading] = useState(false);
    const navigate = useNavigate();
    const location = useLocation();

    const email = location.state?.email || '';

    useEffect(() => {
        if (!email) {
            navigate('/checkout');
        }
    }, [email, navigate]);

    const handleChange = (index, value) => {
        if (/^\d$/.test(value) || value === '') {
            const newCode = [...code];
            newCode[index] = value;
            setCode(newCode);

            // Автоматический переход к следующему инпуту
            if (value && index < 3) {
                document.getElementById(`code-${index + 1}`).focus();
            }
        }
    };

    const handleKeyDown = (index, e) => {
        if (e.key === 'Backspace' && !code[index] && index > 0) {
            document.getElementById(`code-${index - 1}`).focus();
        }
    };

    const handleSubmit = (e) => {
        e.preventDefault();
        const codeString = code.join('');

        if (codeString.length !== 4) {
            setError('Введите 4-значный код');
            return;
        }

        setLoading(true);
        setError('');

        // Сначала проверяем код подтверждения
        fetch('https://localhost:7275/api/Verification/validate-code', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify({
                email,
                code: codeString
            })
        })
        .then(async () => {
            // После успешной верификации кода получаем сохраненные данные заказа
            const orderData = JSON.parse(localStorage.getItem('orderData'));
            
            // Обновляем сессию
            await createUserSession();
            
            // Добавляем отладочную информацию
            console.log('Данные заказа из localStorage:', orderData);
            
            // Проверяем, что все необходимые поля присутствуют и имеют правильные типы
            const validatedOrderData = {
                sessionId: orderData.sessionId ? parseInt(orderData.sessionId) : 0,
                shipCompanyId: orderData.shipCompanyId ? parseInt(orderData.shipCompanyId) : 1,
                shipAddress: orderData.shipAddress,
                shipMethod: orderData.shipMethod,
                payMethod: orderData.payMethod,
                customerFullname: orderData.customerFullName,
                customerEmail: orderData.customerEmail || '',
                customerPhoneNumber: orderData.customerPhoneNumber,
                orderTotalAmount: orderData.orderTotalAmount,
                orderItems: (orderData.orderItems || []).map(item => ({
                    productId: item.productId,
                    quantityItem: item.quantityItem,
                    price: parseFloat(item.price.toFixed(2))
                }))
            };

            console.log('Финальные данные для отправки:', JSON.stringify(validatedOrderData, null, 2));
            
            // Отправляем данные заказа на бэкенд
            return fetch('https://localhost:7275/api/Order/checkout', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify(validatedOrderData)
            })
            .then(response => {
                console.log('Response status:', response.status);
                console.log('Response status text:', response.statusText);
                console.log('Response headers:', Object.fromEntries(response.headers.entries()));
                return response.text().then(text => {
                    try {
                        const jsonResponse = JSON.parse(text);
                        console.log('Response JSON:', jsonResponse);
                        return jsonResponse;
                    } catch (e) {
                        console.log('Response text (not JSON):', text);
                        return { error: text };
                    }
                });
            });
        })
        .then(data => {
            console.log('Успешно оформлен заказ, перенаправляем на страницу заказа');
            


            // Перенаправление на страницу "Заказ оформлен"
            navigate('/order-success');
        })
        .catch(err => {
            console.error('Ошибка оформления заказа:', err);
            setError('Не удалось оформить заказ. Проверьте соединение с интернетом.');
        })
        .finally(() => {
            setLoading(false);
        });
    };

    return (
        <div className="email-verification-page">
            <h1>Подтверждение электронной почты</h1>
            <p>Мы отправили 4-значный код на вашу электронную почту: <strong>{email}</strong></p>
            
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