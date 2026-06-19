// ============================================
// Файл: src/components/CheckoutForm.js
// ============================================

import React, { useState, useEffect, useContext, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { CartContext } from './CartContext.js';
import { useSessionContext } from '../context/SessionContext.tsx';
import { apiClient } from '../api/client.ts';
import './CheckoutForm.css';

const CheckoutForm = ({ onSubmit }) => {
    const { cart } = useContext(CartContext);
    const navigate = useNavigate();
    
    // ✅ Получаем данные сессии
    const { orderId, isValid, isLoading: sessionLoading } = useSessionContext();

    const [formData, setFormData] = useState({
        customerFullName: '',
        customerEmail: '',
        customerPhoneNumber: '',
        shipAddress: '',
        shipMethod: 'Самовывоз',
        payMethod: 'Наличными при получении',
        shipCompanyId: null
    });

    const [shipCompanies, setShipCompanies] = useState([]);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState(null);
    const [errors, setErrors] = useState({});

    // Загрузка компаний доставки
    useEffect(() => {
        const fetchShipCompanies = async () => {
            try {
                setLoading(true);
                const response = await apiClient.get('/Order/shipCompanies');
                setShipCompanies(response.data);
            } catch (err) {
                console.error('Ошибка загрузки компаний доставки:', err);
                setError(err.message || 'Не удалось загрузить компании доставки.');
            } finally {
                setLoading(false);
            }
        };

        fetchShipCompanies();
    }, []);

    // Валидация
    const validateEmail = (email) => /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email);
    const validatePhone = (phone) => /^(\+7|8)?[\s-]?\(?[0-9]{3}\)?[\s-]?[0-9]{3}[\s-]?[0-9]{2}[\s-]?[0-9]{2}$/.test(phone);
    const validateFullName = (name) => /^[А-ЯЁ][а-яё-]+\s+[А-ЯЁ][а-яё-]+\s+[А-ЯЁ][а-яё-]+$/.test(name.trim());
    const validateAddress = (address) => address.trim().length > 0;

    const handleChange = useCallback((e) => {
        const { name, value } = e.target;

        if (name === 'customerPhoneNumber') {
            // Форматирование телефона
            let formattedValue = value.replace(/\D/g, '').slice(0, 11);
            let formattedOutput = '';
            
            if (formattedValue.length > 0) formattedOutput = '+7 ';
            if (formattedValue.length > 1) {
                formattedOutput += `(${formattedValue.substring(1, 4)}`;
                if (formattedValue.length > 4) {
                    formattedOutput += `) ${formattedValue.substring(4, 7)}`;
                    if (formattedValue.length > 7) {
                        formattedOutput += `-${formattedValue.substring(7, 9)}`;
                        if (formattedValue.length > 9) {
                            formattedOutput += `-${formattedValue.substring(9, 11)}`;
                        }
                    }
                }
            }

            setFormData(prev => ({ ...prev, [name]: formattedOutput }));

            if (formattedValue.length !== 11) {
                setErrors(prev => ({ ...prev, customerPhoneNumber: 'Введите корректный номер телефона' }));
            } else {
                setErrors(prev => ({ ...prev, customerPhoneNumber: '' }));
            }
        } else {
            setFormData(prev => ({ ...prev, [name]: value }));
            
            if (name === 'customerFullName' && value && !validateFullName(value)) {
                setErrors(prev => ({ ...prev, customerFullName: 'Введите корректное ФИО' }));
            } else {
                setErrors(prev => ({ ...prev, [name]: '' }));
            }
        }
    }, []);

    const handleSubmit = useCallback(async (e) => {
        e.preventDefault();
        
        // Валидация
        const newErrors = {};
        if (!validateFullName(formData.customerFullName)) {
            newErrors.customerFullName = 'Введите корректное ФИО';
        }
        if (!formData.customerEmail || !validateEmail(formData.customerEmail)) {
            newErrors.customerEmail = 'Введите корректный email';
        }
        if (!validatePhone(formData.customerPhoneNumber)) {
            newErrors.customerPhoneNumber = 'Введите корректный номер телефона';
        }
        if (formData.shipMethod !== 'Самовывоз' && !validateAddress(formData.shipAddress)) {
            newErrors.shipAddress = 'Введите корректный адрес';
        }
        if (formData.shipMethod === 'Служба доставки' && !formData.shipCompanyId) {
            newErrors.shipCompanyId = 'Выберите компанию доставки';
        }

        if (Object.keys(newErrors).length > 0) {
            setErrors(newErrors);
            const firstError = Object.keys(newErrors)[0];
            const element = document.querySelector(`[name="${firstError}"]`);
            if (element) {
                element.scrollIntoView({ behavior: 'smooth', block: 'center' });
                element.focus();
            }
            return;
        }

        if (!isValid || !orderId) {
            alert('Сессия не активна. Пожалуйста, подождите.');
            return;
        }

        setLoading(true);
        setError(null);

        // Подготовка данных заказа
        const orderData = {
            shipCompanyId: formData.shipMethod === 'Служба доставки' ? parseInt(formData.shipCompanyId) : 1,
            shipAddress: formData.shipMethod === 'Самовывоз' ? 'Самовывоз' : formData.shipAddress,
            shipMethod: formData.shipMethod,
            payMethod: formData.payMethod,
            customerFullname: formData.customerFullName,
            customerEmail: formData.customerEmail,
            customerPhoneNumber: formData.customerPhoneNumber,
            orderTotalAmount: parseFloat((
                cart.reduce((sum, item) => sum + (item.productPrice * item.quantity), 0) + 
                (formData.shipMethod === 'Служба доставки' ? 1500 : 0)
            ).toFixed(2)),
            orderItems: cart.map(item => ({
                productId: item.productId,
                productName: item.productName,
                quantityItem: item.quantity,
                price: parseFloat(item.productPrice.toFixed(2))
            })),
            shipCompanyName: formData.shipMethod === 'Служба доставки' ? 
                shipCompanies.find(c => c.shipCompanyId === parseInt(formData.shipCompanyId))?.shipCompanyName : 
                'Самовывоз'
        };

        try {
            // Сохраняем в localStorage для отображения на странице успеха
            localStorage.setItem('orderData', JSON.stringify(orderData));
            
            // Отправка кода подтверждения на email
            // Отправка письма через SMTP на бэкенде часто дольше 10 с — общий timeout в apiClient её обрывает
            await apiClient.post(
                '/Verification/send-code',
                { email: formData.customerEmail },
                { timeout: 120000 }
            );
            
            // Переход на страницу верификации
            navigate('/email-verification', { 
                state: { 
                    email: formData.customerEmail,
                    orderData: orderData // Передаём данные заказа
                } 
            });
            
        } catch (err) {
            console.error('Ошибка при оформлении:', err);
            
            if (err.response?.status === 401) {
                alert('Сессия истекла. Страница будет перезагружена.');
                window.location.reload();
            } else if (err.code === 'ECONNABORTED') {
                setError('Сервер не ответил вовремя. Попробуйте ещё раз — часто со второй попытки отправка быстрее.');
                alert('Превышено время ожидания. Повторите отправку кода.');
            } else {
                setError(err.response?.data?.message || 'Не удалось отправить код подтверждения.');
                alert('Не удалось отправить код подтверждения. Проверьте соединение.');
            }
        } finally {
            setLoading(false);
        }
    }, [formData, cart, shipCompanies, isValid, orderId, navigate]);

    // Показываем лоадер, если сессия ещё не загрузилась
    if (sessionLoading) {
        return <div className="checkout-loading">Инициализация...</div>;
    }

    if (!isValid) {
        return <div className="checkout-error">Ошибка сессии. <button onClick={() => window.location.reload()}>Обновить</button></div>;
    }

    return (
        <form onSubmit={handleSubmit} className="checkout-form">
            <h2>Данные заказа</h2>
            
            <div className="form-group">
                <label>ФИО</label>
                <input type="text" name="customerFullName" value={formData.customerFullName} onChange={handleChange} placeholder="Иванов Иван Иванович" required />
                {errors.customerFullName && <p className="error-message">{errors.customerFullName}</p>}
            </div>

            <div className="form-group">
                <label>Эл. почта</label>
                <input type="email" name="customerEmail" value={formData.customerEmail} onChange={handleChange} placeholder="email@example.com" required />
                {errors.customerEmail && <p className="error-message">{errors.customerEmail}</p>}
            </div>

            <div className="form-group">
                <label>Телефон</label>
                <input type="text" name="customerPhoneNumber" value={formData.customerPhoneNumber} onChange={handleChange} placeholder="+7 (9__) ___ __ __" required />
                {errors.customerPhoneNumber && <p className="error-message">{errors.customerPhoneNumber}</p>}
            </div>

            <div className="form-group">
                <label>Способ оплаты</label>
                <select name="payMethod" value={formData.payMethod} onChange={handleChange}>
                    <option value="Онлайн">Онлайн</option>
                    <option value="Наличными при получении">Наличными при получении</option>
                </select>
            </div>

            <div className="form-group">
                <label>Способ доставки</label>
                <select name="shipMethod" value={formData.shipMethod} onChange={handleChange}>
                    <option value="Самовывоз">Самовывоз</option>
                    <option value="Служба доставки">Служба доставки</option>
                </select>
            </div>

            {formData.shipMethod !== 'Самовывоз' && (
                <div className="form-group">
                    <label>Адрес доставки</label>
                    <textarea name="shipAddress" value={formData.shipAddress} onChange={handleChange} placeholder="Улица, дом, квартира" required />
                    {errors.shipAddress && <p className="error-message">{errors.shipAddress}</p>}
                </div>
            )}

            {formData.shipMethod === 'Служба доставки' && (
                <div className="form-group">
                    <label>Компания доставки</label>
                    {loading ? (
                        <p>Загрузка...</p>
                    ) : error ? (
                        <p className="error-message">{error}</p>
                    ) : (
                        <select name="shipCompanyId" value={formData.shipCompanyId || ''} onChange={handleChange} required>
                            <option value="">Выберите компанию</option>
                            {shipCompanies.map(company => (
                                <option key={company.shipCompanyId} value={company.shipCompanyId}>
                                    {company.shipCompanyName}
                                </option>
                            ))}
                        </select>
                    )}
                    {errors.shipCompanyId && <p className="error-message">{errors.shipCompanyId}</p>}
                </div>
            )}

            <div className="order-summary">
                <div className="order-total">
                    <span>Стоимость товаров:</span>
                    <span>{cart.reduce((sum, item) => sum + (item.productPrice * item.quantity), 0).toFixed(2)} ₽</span>
                </div>
                <div className="order-total">
                    <span>Доставка:</span>
                    <span>{formData.shipMethod === 'Служба доставки' ? '1500.00 ₽' : 'Самовывоз'}</span>
                </div>
                <div className="order-total">
                    <span>Итого:</span>
                    <span>{parseFloat((cart.reduce((sum, item) => sum + (item.productPrice * item.quantity), 0) + (formData.shipMethod === 'Служба доставки' ? 1500 : 0)).toFixed(2))} ₽</span>
                </div>
            </div>
            
            {error && <p className="error-message global">{error}</p>}
            
            <button type="submit" className="submit-button" disabled={loading}>
                {loading ? 'Отправка...' : 'Оформить заказ'}
            </button>
        </form>
    );
};

export default CheckoutForm;