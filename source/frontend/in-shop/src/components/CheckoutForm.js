// src/components/CheckoutForm.js
import React, { useState, useEffect, useContext } from 'react';
import { useNavigate } from 'react-router-dom';
import { CartContext } from '../components/CartContext';
import './CheckoutForm.css';

const CheckoutForm = ({ onSubmit }) => {
    const { cart } = useContext(CartContext); // Получаем корзину из контекста
    const navigate = useNavigate(); // Для навигации после оформления заказа

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
        customerFullname: '',
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
        const phoneRegex = /^(\+7|8)?[\s-]?\(?[0-9]{3}\)?[\s-]?[0-9]{3}[\s-]?[0-9]{2}[\s-]?[0-9]{2}$/;
        return phoneRegex.test(phone);
    };

    // Функция валидации ФИО (гибкая)
    const validateFullName = (name) => {
        // Регулярное выражение для проверки ФИО (3 слова с возможными дефисами и пробелами)
        const fullNameRegex = /^[А-ЯЁ][а-яё-]+\s+[А-ЯЁ][а-яё-]+\s+[А-ЯЁ][а-яё-]+$/;
        return fullNameRegex.test(name.trim());
    };

    // Функция валидации адреса
    const validateAddress = (address) => {
        return address.trim().length > 0;
    };

    const handleChange = (e) => {
    const { name, value } = e.target;

    if (name === 'customerPhoneNumber') {
        // Форматирование номера телефона
        let formattedValue = value.replace(/\D/g, ''); // Убираем все не-цифры

        // Если введено больше 11 цифр, оставляем только первые 11
        if (formattedValue.length > 11) {
            formattedValue = formattedValue.slice(0, 11);
        }

        // Применяем формат +7 (999) 999-99-99
        let formattedOutput = '';
        if (formattedValue.length > 0) {
            formattedOutput = '+7 '; // Всегда добавляем +7
        }
        if (formattedValue.length > 1) {
            // Берём 3 цифры после +7
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

        // Валидация
        if (formattedValue.length !== 11) {
            setErrors(prev => ({
                ...prev,
                customerPhoneNumber: 'Введите корректный номер телефона (+79999999999, +7 (999) 999 99 99, 89999999999)'
            }));
        } else {
            setErrors(prev => ({
                ...prev,
                customerPhoneNumber: ''
            }));
        }
    } else {
        // Для остальных полей
        setFormData(prev => ({ ...prev, [name]: value }));

        // Валидация в реальном времени (для ФИО)
        if (name === 'customerFullName') {
            if (value && !validateFullName(value)) {
                setErrors(prev => ({
                    ...prev,
                    customerFullName: 'Введите корректное ФИО (пример: Иванов Иван Иванович)'
                }));
            } else {
                setErrors(prev => ({
                    ...prev,
                    customerFullName: ''
                }));
            }
        }

        // Очищаем ошибку при изменении поля
        if (errors[name]) {
            setErrors(prev => ({ ...prev, [name]: '' }));
        }
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
        if (!formData.customerEmail || !validateEmail(formData.customerEmail)) {
            newErrors.customerEmail = 'Введите корректный email (пример: user@example.com)';
            isValid = false;
        }

        // Валидация телефона
        if (!validatePhone(formData.customerPhoneNumber)) {
            newErrors.customerPhoneNumber = 'Введите корректный номер телефона (+79999999999, +7 (999) 999 99 99, 89999999999)';
            isValid = false;
        }

        // Валидация адреса
        if (formData.shipMethod !== 'Самовывоз' && !validateAddress(formData.shipAddress)) {
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
            // Подготовка данных для заказа
            const orderData = {
                sessionId: parseInt(localStorage.getItem('sessionId')) || 0,
                shipCompanyId: formData.shipMethod === 'Служба доставки' ? (formData.shipCompanyId ? parseInt(formData.shipCompanyId) : 1) : 1,
                shipAddress: formData.shipMethod === 'Самовывоз' ? 'Самовывоз' : formData.shipAddress,
                shipMethod: formData.shipMethod,
                payMethod: formData.payMethod,
                customerFullname: formData.customerFullName,
                customerEmail: formData.customerEmail,
                customerPhoneNumber: formData.customerPhoneNumber,
                orderTotalAmount: parseFloat((cart.reduce((sum, item) => sum + (item.productPrice * item.quantity), 0) + (formData.shipMethod === 'Служба доставки' ? 1500 : 0)).toFixed(2)),
                orderItems: cart.map(item => ({
                    productId: item.productId,
                    productName: item.productName,
                    quantityItem: item.quantity,
                    price: parseFloat(item.productPrice.toFixed(2))
                })),
                shipCompanyName: formData.shipMethod === 'Служба доставки' ? 
                    shipCompanies.find(company => company.shipCompanyId === parseInt(formData.shipCompanyId))?.shipCompanyName : 
                    'Самовывоз'
            };
            
            // Сохранение данных заказа в локальном хранилище
            try {
                localStorage.setItem('orderData', JSON.stringify(orderData));
                console.log('Данные заказа успешно сохранены в localStorage:', orderData);
            } catch (error) {
                console.error('Ошибка при сохранении данных в localStorage:', error);
            }
            
            // Отправка запроса на отправку email с кодом подтверждения
            fetch('https://localhost:7275/api/Verification/send-code', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({
                    email: formData.customerEmail
                })
            })
            .then(response => {
                if (!response.ok) throw new Error(`Ошибка ${response.status}: ${response.statusText}`);
                return response.json();
            })
            .then(data => {
                // Перенаправление на страницу подтверждения электронной почты
                navigate('/email-verification', { state: { email: formData.customerEmail } });
            })
            .catch(err => {
                console.error('Ошибка отправки email с кодом:', err);
                alert('Не удалось отправить код подтверждения. Проверьте соединение с интернетом.');
            });
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
            <h2>Данные заказа</h2>
            
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
                {errors.customerFullname && <p className="error-message">{errors.customerFullname}</p>}
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
                    <option key="cash" value="Наличными при получении">Наличными при получении</option>
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
                    <span>Итого к оплате:</span>
                    <span>{parseFloat((cart.reduce((sum, item) => sum + (item.productPrice * item.quantity), 0) + (formData.shipMethod === 'Служба доставки' ? 1500 : 0)).toFixed(2))} ₽</span>
                </div>
            </div>
            <button type="submit" className="submit-button">
                Оформить заказ
            </button>
        </form>
    );
};

export default CheckoutForm;