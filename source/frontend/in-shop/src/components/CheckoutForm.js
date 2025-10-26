// src/components/CheckoutForm.js
import React, { useState, useEffect } from 'react';
import './CheckoutForm.css';

const CheckoutForm = ({ onSubmit }) => {
    const [formData, setFormData] = useState({
        customerFullName: '',
        customerEmail: '',
        customerPhoneNumber: '',
        shipAddress: '',
        shipMethod: 'Самовывоз',
        payMethod: 'Наличными при получении',
        shipCompanyId: null
    });

    const [shipCompanies, setShipCompanies] = useState([]); // Список компаний доставки
    const [loading, setLoading] = useState(false); // Состояние загрузки
    const [error, setError] = useState(null); // Состояние ошибки

    // Состояния для ошибок валидации
    const [errors, setErrors] = useState({
        customerFullName: '',
        customerEmail: '',
        customerPhoneNumber: '',
        shipAddress: '',
        shipCompanyId: '' // Ошибка для выбора компании доставки
    });

    // Загрузка компаний доставки
    useEffect(() => {
        const fetchShipCompanies = async () => {
            try {
                setLoading(true);
                const response = await fetch('https://localhost:7275/api/Order/shipCompanies');
                console.log("Ok")
                if (!response.ok) throw new Error(`Ошибка ${response.status}: ${response.statusText}`);
                const data = await response.json();
                setShipCompanies(data);
            } catch (err) {
                console.error('Ошибка загрузки компаний доставки:', err);
                setError(err.message || 'Не удалось загрузить компании доставки.');
            } finally {
                setLoading(false);
            }
        };

        fetchShipCompanies();
    }, []);

    // Функция валидации email
    const validateEmail = (email) => {
        const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
        return emailRegex.test(email);
    };

    // Функция валидации телефона
    const validatePhone = (phone) => {
        // Регулярное выражение для проверки разных форматов телефона
        const phoneRegex = /^(\+7|8)?[\s\-]?\(?[0-9]{3}\)?[\s\-]?[0-9]{3}[\s\-]?[0-9]{2}[\s\-]?[0-9]{2}$/;
        return phoneRegex.test(phone);
    };

    // Функция валидации ФИО
    const validateFullName = (name) => {
        return name.trim().length > 0;
    };

    // Функция валидации адреса
    const validateAddress = (address) => {
        return address.trim().length > 0;
    };

    const handleChange = (e) => {
        const { name, value } = e.target;
        setFormData(prev => ({ ...prev, [name]: value }));

        // Очищаем ошибку при изменении поля
        if (errors[name]) {
            setErrors(prev => ({ ...prev, [name]: '' }));
        }
    };

    const handleSubmit = (e) => {
        e.preventDefault();
        
        // Проверка валидации перед отправкой
        let isValid = true;
        const newErrors = {};

        // Валидация ФИО
        if (!validateFullName(formData.customerFullName)) {
            newErrors.customerFullName = 'Введите корректное ФИО (пример: Иванов Иван Иванович)';
            isValid = false;
        }

        // Валидация email
        if (!validateEmail(formData.customerEmail)) {
            newErrors.customerEmail = 'Введите корректный email (пример: user@example.com)';
            isValid = false;
        }

        // Валидация телефона
        if (!validatePhone(formData.customerPhoneNumber)) {
            newErrors.customerPhoneNumber = 'Введите корректный номер телефона (+79999999999, +7 (999) 999 99 99, 89999999999)';
            isValid = false;
        }

        // Валидация адреса
        if (!validateAddress(formData.shipAddress)) {
            newErrors.shipAddress = 'Введите корректный адрес (пример: ул. Ленина, д. 1, кв. 1)';
            isValid = false;
        }

        // Валидация компании доставки (если выбран способ "Служба доставки")
        if (formData.shipMethod === 'Служба доставки' && !formData.shipCompanyId) {
            newErrors.shipCompanyId = 'Выберите компанию доставки';
            isValid = false;
        }

        setErrors(newErrors);

        if (isValid) {
            onSubmit(formData);
        } else {
            // Прокручиваем к первому полю с ошибкой
            const firstErrorField = Object.keys(newErrors)[0];
            if (firstErrorField) {
                const element = document.querySelector(`[name="${firstErrorField}"]`);
                if (element) {
                    element.scrollIntoView({ behavior: 'smooth', block: 'center' });
                    element.focus();
                }
            }
        }
    };

    return (
        <form onSubmit={handleSubmit} className="checkout-form">
            <h2>Покупатель</h2>
            
            <div className="form-group">
                <label>ФИО</label>
                <input
                    type="text"
                    name="customerFullName"
                    value={formData.customerFullName}
                    onChange={handleChange}
                    placeholder="Иванов Иван Иванович"
                    required
                />
                {errors.customerFullName && <p className="error-message">{errors.customerFullName}</p>}
            </div>

            <div className="form-group">
                <label>Эл. почта</label>
                <input
                    type="email"
                    name="customerEmail"
                    value={formData.customerEmail}
                    onChange={handleChange}
                    placeholder="email@example.com"
                    required
                />
                {errors.customerEmail && <p className="error-message">{errors.customerEmail}</p>}
            </div>

            <div className="form-group">
                <label>Телефон</label>
                <input
                    type="text"
                    name="customerPhoneNumber"
                    value={formData.customerPhoneNumber}
                    onChange={handleChange}
                    placeholder="+7 (9__) ___ __ __"
                    required
                />
                {errors.customerPhoneNumber && <p className="error-message">{errors.customerPhoneNumber}</p>}
            </div>

            

            <div className="form-group">
                <label>Способ оплаты</label>
                <select
                    name="payMethod"
                    value={formData.payMethod}
                    onChange={handleChange}
                >
                    <option key="online" value="Онлайн">Онлайн</option>
                    <option key="cash" value="Наличными">Наличными при получении</option>
                </select>
            </div>

            <div className="form-group">
                <label>Способ доставки</label>
                <select
                    name="shipMethod"
                    value={formData.shipMethod}
                    onChange={handleChange}
                >
                    <option key="pickup" value="Самовывоз">Самовывоз</option>
                    <option key="delivery-service" value="Служба доставки">Служба доставки</option>
                </select>
            </div>

            {formData.shipMethod !== 'Самовывоз' && (
                <div className="form-group">
                    <label>Адрес доставки</label>
                    <textarea
                        name="shipAddress"
                        value={formData.shipAddress}
                        onChange={handleChange}
                        placeholder="Улица, дом, квартира"
                        required
                    />
                    {errors.shipAddress && <p className="error-message">{errors.shipAddress}</p>}
                </div>
            )}

            {formData.shipMethod === 'Служба доставки' && (
                <div className="form-group">
                    <label>Компания доставки</label>
                    {loading ? (
                        <p>Загрузка компаний...</p>
                    ) : error ? (
                        <p className="error-message">{error}</p>
                    ) : (
                        <select
                            name="shipCompanyId"
                            value={formData.shipCompanyId || ''}
                            onChange={handleChange}
                            required
                        >
                            <option key="" value="">Выберите компанию</option>
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

            <button type="submit" className="submit-button">
                Оформить заказ
            </button>
        </form>
    );
};

export default CheckoutForm;