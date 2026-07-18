// ============================================
// Файл: src/pages/OrderSuccessPage/OrderSuccessPage.js
// ============================================

import React, { useState, useEffect } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import { useSessionContext } from '../../context/SessionContext';
import { apiClient } from '../../api/client';
import { redirectToPaymentProvider } from '../../utils/paymentRedirect';
import { clearCheckoutStorage, readCompletedOrder } from '../../utils/checkoutStorage';
import './OrderSuccessPage.css';

const OrderSuccessPage = () => {
    const [orderData, setOrderData] = useState(null);
    const [completedOrderId, setCompletedOrderId] = useState(null);
    const [isPaying, setIsPaying] = useState(false);
    const [trackingPath, setTrackingPath] = useState(null);
    const [paymentFailed, setPaymentFailed] = useState(false);
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
        const storedOrderData = readCompletedOrder();
        const queryOrderIdRaw = new URLSearchParams(location.search).get('orderId');
        const queryOrderId = queryOrderIdRaw ? Number.parseInt(queryOrderIdRaw, 10) : NaN;
        const resolvedOrderId = stateCompletedOrderId
            ?? (!Number.isNaN(queryOrderId) ? queryOrderId : null)
            ?? storedOrderData?.orderId
            ?? null;

        setPaymentFailed(Boolean(location.state?.paymentFailed));

        if (resolvedOrderId != null) {
            setCompletedOrderId(resolvedOrderId);
        }

        if (stateOrderData) {
            setOrderData({
                ...stateOrderData,
                orderId: stateOrderData.orderId ?? resolvedOrderId,
            });
            return;
        }

        if (storedOrderData) {
            setOrderData({
                ...storedOrderData,
                orderId: storedOrderData.orderId ?? resolvedOrderId,
            });
            return;
        }

        // После неуспешной оплаты / внешнего редиректа данные в storage могли истечь —
        // оставляем минимальный черновик, чтобы можно было повторить оплату.
        if (resolvedOrderId != null) {
            setOrderData({
                orderId: resolvedOrderId,
                payMethod: 'Онлайн',
                orderItems: [],
            });
        }
    }, [location.state, location.search]);

    useEffect(() => {
        if (!completedOrderId || !isValid) {
            return undefined;
        }

        let cancelled = false;
        apiClient
            .get(`/Order/${completedOrderId}/tracking-link`)
            .then((res) => {
                if (!cancelled && res.data?.trackingPath) {
                    setTrackingPath(res.data.trackingPath);
                }
            })
            .catch(() => {
                /* кнопка отслеживания опциональна */
            });

        return () => {
            cancelled = true;
        };
    }, [completedOrderId, isValid]);

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

                redirectToPaymentProvider(redirectUrl);
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
            <div className="order-success-page error page-reveal">
                <p>⚠️ Ошибка сессии: {sessionError || 'Сессия не активна'}</p>
                <button type="button" onClick={() => window.location.reload()}>Повторить</button>
            </div>
        );
    }

    if (!orderData) {
        return (
            <div className="order-success-page page-reveal">
                <p>Данные заказа не найдены.</p>
                <button type="button" onClick={() => navigate('/')}>На главную</button>
            </div>
        );
    }

    const orderItems = (orderData.orderItems || []).map((item) => {
        const rawQuantity = Number(
            item.quantityItem
            ?? item.quantity
            ?? item.QuantityItem
            ?? item.Quantity
            ?? 0
        );
        const quantity = rawQuantity > 0 ? rawQuantity : 1;
        const unitPrice = Number(item.displayPrice ?? item.price ?? 0);
        const lineTotal = item.totalPrice != null
            ? Number(item.totalPrice)
            : unitPrice * quantity;
        return {
            productId: item.productId,
            productName:
                item.productName
                || item.product?.productName
                || (item.productId != null ? `Товар #${item.productId}` : 'Товар'),
            quantity: Number.isFinite(quantity) ? quantity : 1,
            unitPrice: Number.isFinite(unitPrice) ? unitPrice : 0,
            lineTotal: Number.isFinite(lineTotal) ? lineTotal : 0,
        };
    });

    const deliveryCost = !orderData.shipMethod
        ? 0
        : (orderData.shipMethod === 'Самовывоз' ? 0 : 1500);
    const itemsTotal = orderItems.reduce((sum, item) => sum + item.lineTotal, 0);
    const rawTotal = orderData.orderTotalAmount ?? orderData.displayTotalAmount ?? (itemsTotal + deliveryCost);
    const totalAmount = Number.isFinite(Number(rawTotal)) ? Number(rawTotal) : itemsTotal + deliveryCost;

    return (
        <div className="order-success-page">
            <div className="order-success-container page-reveal">
                <div className="order-success-header">
                    <h1>Заказ оформлен</h1>
                    <p className="order-number">Номер заказа: <strong>#{completedOrderId || 'N/A'}</strong></p>
                </div>

                {paymentFailed && (
                    <div className="payment-failed-banner page-reveal">
                        <strong>Оплата не подтверждена.</strong>
                        {' '}
                        Заказ сохранён — вы можете повторить оплату ниже.
                    </div>
                )}

                <div className="order-success-content">
                    <div className="order-info page-reveal page-reveal--delay-1">
                        <h2>Информация о заказе</h2>
                        <div className="info-grid">
                            <div className="info-item">
                                <span className="label">Дата заказа</span>
                                <span className="value">{new Date().toLocaleDateString('ru-RU')}</span>
                            </div>
                            <div className="info-item">
                                <span className="label">Способ оплаты</span>
                                <span className="value">{orderData.payMethod || 'Онлайн'}</span>
                            </div>
                            {orderData.shipMethod && (
                                <div className="info-item">
                                    <span className="label">Способ доставки</span>
                                    <span className="value">{orderData.shipMethod}</span>
                                </div>
                            )}
                            {orderData.shipAddress && (
                                <div className="info-item">
                                    <span className="label">Адрес доставки</span>
                                    <span className="value">{orderData.shipAddress}</span>
                                </div>
                            )}
                        </div>
                    </div>

                    {(orderData.customerFullname || orderData.customerFullName || orderData.customerEmail || orderData.customerPhoneNumber) && (
                    <div className="customer-info page-reveal page-reveal--delay-2">
                        <h2>Контактная информация</h2>
                        <div className="info-grid">
                            {(orderData.customerFullname || orderData.customerFullName) && (
                                <div className="info-item">
                                    <span className="label">ФИО</span>
                                    <span className="value">{orderData.customerFullname || orderData.customerFullName}</span>
                                </div>
                            )}
                            {orderData.customerEmail && (
                                <div className="info-item">
                                    <span className="label">Email</span>
                                    <span className="value">{orderData.customerEmail}</span>
                                </div>
                            )}
                            {orderData.customerPhoneNumber && (
                                <div className="info-item">
                                    <span className="label">Телефон</span>
                                    <span className="value">{orderData.customerPhoneNumber}</span>
                                </div>
                            )}
                        </div>
                    </div>
                    )}

                    {orderItems.length > 0 && (
                    <div className="order-items page-reveal page-reveal--delay-3">
                        <h2>Состав заказа ({orderItems.length})</h2>
                        <div className="items-list">
                            {orderItems.map((item, index) => (
                                <div key={`${item.productId}-${index}`} className="order-item">
                                    <div className="item-info">
                                        <span className="item-name">{item.productName}</span>
                                        <span className="item-quantity">× {item.quantity}</span>
                                    </div>
                                    <div className="item-price">
                                        {item.lineTotal.toFixed(2)} ₽
                                    </div>
                                </div>
                            ))}
                        </div>
                    </div>
                    )}

                    {(orderItems.length > 0 || orderData.orderTotalAmount != null || orderData.displayTotalAmount != null) && (
                    <div className="order-summary">
                        <h2>Стоимость заказа</h2>
                        <div className="summary-grid">
                            {orderItems.length > 0 && (
                                <div className="summary-item">
                                    <span className="label">Товары</span>
                                    <span className="value">{itemsTotal.toFixed(2)} ₽</span>
                                </div>
                            )}
                            {orderData.shipMethod && (
                                <div className="summary-item">
                                    <span className="label">Доставка</span>
                                    <span className="value">{deliveryCost} ₽</span>
                                </div>
                            )}
                            <div className="summary-item total">
                                <span className="label">Итого</span>
                                <span className="value total">{totalAmount.toFixed(2)} ₽</span>
                            </div>
                        </div>
                    </div>
                    )}

                    {(orderData.payMethod === 'Онлайн' || !orderData.payMethod) && (
                        <div className="payment-reminder page-reveal page-reveal--delay-4">
                            <div className="reminder-icon">!</div>
                            <div className="reminder-content">
                                <h3>{paymentFailed ? 'Повторите оплату' : 'Требуется оплата'}</h3>
                                <p>
                                    {paymentFailed
                                        ? 'Предыдущая попытка не подтверждена. Оплатите заказ, чтобы завершить оформление.'
                                        : 'Оплатите заказ в течение 24 часов, иначе он будет отменён.'}
                                </p>
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

                <div className="order-success-footer page-reveal-block">
                    <p>Спасибо за покупку! Детали заказа также отправлены на email.</p>
                    <div className="order-success-actions">
                        {trackingPath && (
                            <button
                                type="button"
                                className="back-to-shop"
                                onClick={() => navigate(trackingPath)}
                            >
                                Отследить заказ
                            </button>
                        )}
                        <button
                            type="button"
                            className="order-success-secondary"
                            onClick={() => {
                                clearCheckoutStorage();
                                navigate('/');
                            }}
                        >
                            Продолжить покупки
                        </button>
                    </div>
                </div>
            </div>
        </div>
    );
};

export default OrderSuccessPage;
