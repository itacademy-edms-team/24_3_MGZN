// ============================================
// Файл: src/pages/OrderSuccessPage/OrderSuccessPage.js
// ============================================

import React, { useState, useEffect } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import { useSessionContext } from '../../context/SessionContext.tsx';
import { apiClient } from '../../api/client.ts';
import './OrderSuccessPage.css';

const OrderSuccessPage = () => {
    const [orderData, setOrderData] = useState(null);
    const [completedOrderId, setCompletedOrderId] = useState(null);
    const [isPaying, setIsPaying] = useState(false);
    const navigate = useNavigate();
    const location = useLocation();

    const {
        isValid,
        isLoading: sessionLoading,
        error: sessionError
    } = useSessionContext();

    useEffect(() => {
        const stateCompletedOrderId = location.state?.completedOrderId;
        const stateOrderData = location.state?.orderData;
        const storedCompletedOrderId = localStorage.getItem('completedOrderId');
        const storedOrderData = localStorage.getItem('orderData');

        if (stateCompletedOrderId) {
            setCompletedOrderId(stateCompletedOrderId);
        } else if (storedCompletedOrderId) {
            const parsedCompletedOrderId = parseInt(storedCompletedOrderId, 10);
            if (!Number.isNaN(parsedCompletedOrderId)) {
                setCompletedOrderId(parsedCompletedOrderId);
            }
        }

        if (stateOrderData) {
            setOrderData(stateOrderData);
            return;
        }

        if (storedOrderData) {
            try {
                const parsedData = JSON.parse(storedOrderData);
                setOrderData(parsedData);
            } catch (error) {
                console.error('Ошибка при парсинге данных заказа:', error);
            }
        }
    }, [location.state]);

    /**
     * ЮKassa: POST /Payment/initiate → редирект на страницу ЮKassa (без /payment).
     * Мок: переход на /payment с формой карты.
     */
    const handlePayNow = async () => {
        if (!completedOrderId) {
            alert('Номер оформленного заказа не найден.');
            return;
        }

        setIsPaying(true);

        try {
            const providerResponse = await apiClient.get('/Payment/provider');
            const provider = providerResponse.data?.provider ?? 'Mock';

            if (provider.toLowerCase() === 'yookassa') {
                const response = await apiClient.post('/Payment/initiate', {
                    orderId: completedOrderId
                });

                const redirectUrl = response.data?.redirectUrl;
                if (!redirectUrl) {
                    throw new Error('Сервер не вернул ссылку на оплату ЮKassa.');
                }

                window.location.href = redirectUrl;
                return;
            }

            navigate('/payment', {
                state: {
                    completedOrderId,
                    orderData: {
                        ...orderData,
                        orderId: completedOrderId
                    }
                }
            });
        } catch (error) {
            console.error('Ошибка при инициации оплаты:', error);
            alert(error.response?.data?.message || error.message || 'Не удалось начать оплату.');
        } finally {
            setIsPaying(false);
        }
    };

    if (sessionLoading) {
        return <div className="order-success-page loading">Инициализация...</div>;
    }

    if (sessionError || !isValid) {
        return (
            <div className="order-success-page error">
                <p>⚠️ Ошибка сессии: {sessionError || 'Сессия не активна'}</p>
                <button type="button" onClick={() => window.location.reload()}>Повторить</button>
            </div>
        );
    }

    if (!orderData) {
        return (
            <div className="order-success-page">
                <p>Данные заказа не найдены.</p>
                <button type="button" onClick={() => navigate('/')}>На главную</button>
            </div>
        );
    }

    const deliveryCost = orderData.shipMethod === 'Самовывоз' ? 0 : 1500;
    const itemsTotal = orderData.orderItems?.reduce((sum, item) => sum + (item.price * item.quantityItem), 0) || 0;
    const totalAmount = itemsTotal + deliveryCost;

    return (
        <div className="order-success-page">
            <div className="order-success-container">
                <div className="order-success-header">
                    <div className="success-icon">✓</div>
                    <h1>Заказ оформлен</h1>
                    <p className="order-number">Номер заказа: <strong>#{completedOrderId || 'N/A'}</strong></p>
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
                                        <span className="item-quantity">× {item.quantityItem}</span>
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
                                <span className="label">Итого</span>
                                <span className="value total">{totalAmount.toFixed(2)} ₽</span>
                            </div>
                        </div>
                    </div>

                    {orderData.payMethod === 'Онлайн' && (
                        <div className="payment-reminder">
                            <div className="reminder-icon">!</div>
                            <div className="reminder-content">
                                <h3>Требуется оплата</h3>
                                <p>Оплатите заказ в течение 24 часов, иначе он будет отменён.</p>
                                <button
                                    type="button"
                                    className="pay-button"
                                    onClick={handlePayNow}
                                    disabled={isPaying}
                                >
                                    {isPaying ? 'Переход к оплате...' : 'Оплатить заказ'}
                                </button>
                            </div>
                        </div>
                    )}
                </div>

                <div className="order-success-footer">
                    <p>Спасибо за покупку! По вопросам: поддержка@магазин.ру</p>
                    <button type="button" onClick={() => navigate('/')} className="back-to-shop">
                        Продолжить покупки
                    </button>
                </div>
            </div>
        </div>
    );
};

export default OrderSuccessPage;
