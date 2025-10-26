// src/components/CheckoutForm.js
import React, { useState, useEffect } from 'react';
import './CheckoutForm.css';

const CheckoutForm = ({ onSubmit }) => {
    const [formData, setFormData] = useState({
        customerFullName: '',
        customerEmail: '',
        customerPhoneNumber: '',
        shipAddress: '',
        shipMethod: 'Доставка на дом',
        payMethod: 'Онлайн',
        shipCompanyId: null
    });

    const [shipCompanies, setShipCompanies] = useState([]); // Список компаний доставки
    const [loading, setLoading] = useState(false); // Состояние загрузки
    const [error, setError] = useState(null); // Состояние ошибки

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

    const handleChange = (e) => {
        const { name, value } = e.target;
        setFormData(prev => ({ ...prev, [name]: value }));
    };

    const handleSubmit = (e) => {
        e.preventDefault();
        onSubmit(formData);
    };

    return (
        <form onSubmit={handleSubmit} className="checkout-form">
            <h2>Покупатель</h2>
            
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
            </div>

            <div className="form-group">
                <label>ФИО</label>
                <input
                    type="text"
                    name="customerFullName"
                    value={formData.customerFullName}
                    onChange={handleChange}
                    placeholder="Ваше имя"
                    required
                />
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
            </div>

            <div className="form-group">
                <label>Адрес доставки</label>
                <textarea
                    name="shipAddress"
                    value={formData.shipAddress}
                    onChange={handleChange}
                    placeholder="Улица, дом, квартира"
                    required
                />
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
                </div>
            )}
        </form>
    );
};

export default CheckoutForm;