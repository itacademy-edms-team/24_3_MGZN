// src/pages/OrderSuccessPage/OrderSuccessPage.js
import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { useSession } from '../../hooks/useUserSession.ts';
import './OrderSuccessPage.css';

const OrderSuccessPage = () => {
    const [orderData, setOrderData] = useState(null);
    const navigate = useNavigate(); // Теперь используется для навигации
    const { sessionId, loading: sessionLoading, error: sessionError } = useSession();

    useEffect(() => {
        // Попробуем получить данные заказа из localStorage
        const storedOrderData = localStorage.getItem('orderData');
        console.log('Полученные данные из localStorage:', storedOrderData);
        if (storedOrderData) {
            try {
                const parsedData = JSON.parse(storedOrderData);
                setOrderData(parsedData);
                // Сохраняем ID заказа в localStorage под ключом unpayedOrderId
                const orderIdToSave = localStorage.orderId - 1;
                if (orderIdToSave) {
                    localStorage.setItem('unpayedOrderId', orderIdToSave.toString());
                    console.log('unpayedOrderId сохранён в localStorage:', orderIdToSave);
                }
                console.log('Данные заказа успешно загружены:', JSON.parse(storedOrderData));
            } catch (error) {
                console.error('Ошибка при парсинге данных заказа:', error);
            }
        } else {
            console.log('Данные заказа в localStorage отсутствуют');
        }
    }, []);

    if (!orderData) {
        return <div className="order-success-page loading">Загрузка данных заказа...</div>;
    }

    // Перенаправляем на главную страницу, если данные заказа отсутствуют
    if (sessionLoading) {
        return <div>Создание новой сессии...</div>;
    }

    if (sessionError) {
        return (
            <div className="error">
                Ошибка сессии: {sessionError}
                <button onClick={() => window.location.reload()}>Повторить</button>
            </div>
        );
    }

    const handlePayNow = () => {
        // Получаем ID заказа из localStorage
        const unpayedOrderId = localStorage.getItem('unpayedOrderId');
        if (!unpayedOrderId) {
            alert('Не удалось получить ID заказа для оплаты.');
            return;
        }

        // Подготовим данные для передачи в state
        const orderDataToSend = {
            ...orderData,
            orderId: parseInt(unpayedOrderId) // Заменяем orderId на сохранённый unpayedOrderId
        };

        // Переход на страницу оплаты с передачей обновлённых данных заказа
        navigate('/payment', { state: { orderData: orderDataToSend } });
    };

    // Расчет стоимости доставки
    const deliveryCost = orderData.shipMethod === "Самовывоз" ? 0 : 1500;

    // Расчет общей стоимости
    const itemsTotal = orderData.orderItems?.reduce((sum, item) => sum + (item.price * item.quantityItem), 0) || 0;
    const totalAmount = itemsTotal + deliveryCost;

    return (
        <div className="order-success-page">
            <div className="order-success-container">
                <div className="order-success-header">
                    <div className="success-icon">✓</div>
                    <h1>Заказ оформлен</h1>
                    <p className="order-number">Номер заказа: <strong>#{orderData.sessionId || 'N/A'}</strong></p>
                </div>

                <div className="order-success-content">
                    <div className="order-info">
                        <h2>Информация о заказе</h2>

                        <div className="info-grid">
                            <div className="info-item">
                                <span className="label">Дата заказа</span>
                                <span className="value">{new Date().toLocaleDateString('ru-RU')}</span>
                            </div>

                            <div className="info-item">
                                <span className="label">Способ оплаты</span>
                                <span className="value">{orderData.payMethod}</span>
                            </div>

                            <div className="info-item">
                                <span className="label">Способ доставки</span>
                                <span className="value">{orderData.shipMethod}</span>
                            </div>

                            {orderData.shipAddress && (
                                <div className="info-item">
                                    <span className="label">Адрес доставки</span>
                                    <span className="value">{orderData.shipAddress}</span>
                                </div>
                            )}

                            {orderData.shipCompanyId && (
                                <div className="info-item">
                                    <span className="label">Компания доставки</span>
                                    <span className="value">{orderData.shipCompanyName}</span>
                                </div>
                            )}
                        </div>
                    </div>

                    <div className="customer-info">
                        <h2>Контактная информация</h2>

                        <div className="info-grid">
                            <div className="info-item">
                                <span className="label">ФИО</span>
                                <span className="value">{orderData.customerFullname}</span>
                            </div>

                            <div className="info-item">
                                <span className="label">Email</span>
                                <span className="value">{orderData.customerEmail}</span>
                            </div>

                            <div className="info-item">
                                <span className="label">Телефон</span>
                                <span className="value">{orderData.customerPhoneNumber}</span>
                            </div>
                        </div>
                    </div>

                    <div className="order-items">
                        <h2>Состав заказа ({orderData.orderItems?.length || 0})</h2>

                        <div className="items-list">
                            {orderData.orderItems?.map((item, index) => (
                                <div key={index} className="order-item">
                                    <div className="item-info">
                                        <span className="item-name">{item.productName || `Товар #${item.productId}`}</span>
                                        <span className="item-quantity">Количество: {item.quantityItem}</span>
                                    </div>
                                    <div className="item-price">
                                        {(item.price * item.quantityItem).toFixed(2)} ₽
                                    </div>
                                </div>
                            ))}
                        </div>
                    </div>

                    <div className="order-summary">
                        <h2>Стоимость заказа</h2>

                        <div className="summary-grid">
                            <div className="summary-item">
                                <span className="label">Товары</span>
                                <span className="value">{itemsTotal.toFixed(2)} ₽</span>
                            </div>

                            <div className="summary-item">
                                <span className="label">Доставка</span>
                                <span className="value">{deliveryCost} ₽</span>
                            </div>

                            <div className="summary-item total">
                                <span className="label">Итого к оплате</span>
                                <span className="value total">{totalAmount.toFixed(2)} ₽</span>
                            </div>
                        </div>
                    </div>

                    {orderData.payMethod === 'Онлайн' && (
                        <div className="payment-reminder">
                            <div className="reminder-icon">!</div>
                            <div className="reminder-content">
                                <h3>Требуется оплата заказа</h3>
                                <p>Ваш заказ оформлен, но еще не оплачен. Пожалуйста, оплатите заказ в течение 24 часов, иначе он будет автоматически отменен.</p>
                                <button className="pay-button" onClick={handlePayNow}>
                                    Оплатить заказ
                                </button>
                            </div>
                        </div>
                    )}
                </div>

                <div className="order-success-footer">
                    <p>Спасибо за покупку в нашем магазине! Если у вас возникнут вопросы, свяжитесь с нашей службой поддержки.</p>
                </div>
            </div>
        </div>
    );
};

export default OrderSuccessPage;