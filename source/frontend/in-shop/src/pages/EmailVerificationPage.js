import React, { useState, useEffect } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import { createUserSession } from '../services/SessionService.ts';
import './EmailVerificationPage.css';

const EmailVerificationPage = () => {
    const [code, setCode] = useState(['', '', '', '']);
    const [error, setError] = useState(''); // Это будет для ошибок API
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

    const handleSubmit = async (e) => { // Сделаем функцию асинхронной
        e.preventDefault();
        const codeString = code.join('');

        if (codeString.length !== 4) {
            setError('Введите 4-значный код');
            return;
        }

        setLoading(true);
        setError(''); // Очистим старую ошибку перед новой попыткой

        try {
            // --- ШАГ 1: Проверка кода ---
            const validateResponse = await fetch('https://localhost:7275/api/Verification/validate-code', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({
                    email,
                    code: codeString
                })
            });

            // Проверим статус ответа
            if (!validateResponse.ok) {
                // Если статус не 2xx, значит ошибка
                const validateErrorData = await validateResponse.json().catch(() => ({ error: 'Неизвестная ошибка при проверке кода' }));
                // Покажем сообщение об ошибке от API
                setError(validateErrorData.message || validateErrorData.error || 'Неверный или недействительный код подтверждения.');
                setLoading(false);
                return; // Прерываем выполнение
            }

            // Если код валиден, продолжаем
            const validateData = await validateResponse.json();
            console.log('Код подтверждения валиден:', validateData);

            // --- ШАГ 2: Получение данных заказа ---
            const orderData = JSON.parse(localStorage.getItem('orderData'));

            if (!orderData) {
                setError('Данные заказа не найдены в сессии.');
                setLoading(false);
                return;
            }

            await createUserSession();

            console.log('Данные заказа из localStorage:', orderData);

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

            // --- ШАГ 3: Отправка заказа ---
            const checkoutResponse = await fetch('https://localhost:7275/api/Order/checkout', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify(validatedOrderData)
            });

            if (!checkoutResponse.ok) {
                const checkoutErrorData = await checkoutResponse.json().catch(() => ({ error: 'Неизвестная ошибка при оформлении заказа' }));
                setError(checkoutErrorData.message || checkoutErrorData.error || 'Не удалось оформить заказ.');
                setLoading(false);
                return; // Прерываем выполнение
            }

            const checkoutData = await checkoutResponse.json();
            console.log('Заказ успешно оформлен:', checkoutData);

            // --- ШАГ 4: Перенаправление ---
            console.log('Успешно оформлен заказ, перенаправляем на страницу заказа');
            navigate('/order-success');

        } catch (err) {
            console.error('Ошибка оформления заказа:', err);
            setError('Не удалось оформить заказ. Проверьте соединение с интернетом.');
            setLoading(false);
        }
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

                {/* --- ОТОБРАЖЕНИЕ ОШИБКИ --- */}
                {error && <p className="error-message">{error}</p>}
                {/* --- /ОТОБРАЖЕНИЕ ОШИБКИ --- */}

                <button type="submit" className="submit-button" disabled={loading}>
                    {loading ? 'Проверка...' : 'Подтвердить'}
                </button>
            </form>
        </div>
    );
};

export default EmailVerificationPage;