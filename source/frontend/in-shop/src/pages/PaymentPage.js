// Страница оплаты для МОКА (PaymentsAPI): ввод карты на нашем сайте.
// При Payment:Provider = YooKassa не используется — оплата с /order-success.
import React, { useMemo, useEffect, useState } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import { apiClient } from '../api/client';
import { redirectToPaymentProvider } from '../utils/paymentRedirect';
import { readCompletedOrder } from '../utils/checkoutStorage';
import './PaymentPage.css';

const PaymentPage = () => {
    const navigate = useNavigate();
    const location = useLocation();
    const [errorMessage, setErrorMessage] = useState('');

    // ЮKassa: эта страница не нужна — возвращаем на экран заказа.
    useEffect(() => {
        apiClient.get('/Payment/provider')
            .then((res) => {
                const provider = res.data?.provider ?? 'Mock';
                if (provider.toLowerCase() === 'yookassa') {
                    const stored = readCompletedOrder();
                    navigate('/order-success', {
                        replace: true,
                        state: {
                            completedOrderId: stored?.orderId,
                            orderData: stored || undefined,
                        },
                    });
                }
            })
            .catch(() => {});
    }, [navigate]);

    const orderDataFromState = location.state?.orderData || null;
    const completedOrderIdFromState = location.state?.completedOrderId;
    const trackingTokenFromState = location.state?.trackingToken || null;
    const storedOrderData = useMemo(() => {
        return readCompletedOrder();
    }, []);

    const orderData = orderDataFromState || storedOrderData;
    const resolvedOrderIdRaw =
        completedOrderIdFromState ??
        orderDataFromState?.orderId ??
        storedOrderData?.orderId;
    const resolvedOrderId = Number.parseInt(String(resolvedOrderIdRaw), 10);
    const hasValidOrderId = !Number.isNaN(resolvedOrderId);

    const handlePayment = async () => {
        if (!hasValidOrderId) {
            setErrorMessage('Ошибка: не удалось получить номер оформленного заказа.');
            return;
        }

        // Mock provider must never collect raw card data on the shop origin.
        const paymentData = {
            orderId: resolvedOrderId,
            mockPaymentToken: 'inshop-test-approved',
            ...(trackingTokenFromState ? { trackingToken: trackingTokenFromState } : {}),
        };

        try {
            setErrorMessage('');
            const response = await apiClient.post('/Payment/process', paymentData);

            // ЮKassa: бэкенд возвращает redirectUrl — уходим на страницу оплаты ЮMoney.
            const redirectUrl = response.data?.redirectUrl;
            if (redirectUrl) {
                redirectToPaymentProvider(redirectUrl);
                return;
            }

            navigate('/payment-confirmation', {
                state: {
                    orderId: paymentData.orderId,
                    ...(trackingTokenFromState ? { trackingToken: trackingTokenFromState } : {}),
                },
            });
        } catch (error) {
            console.error('Ошибка при отправке данных оплаты:', error);
            setErrorMessage(error.response?.data?.message || 'Ошибка при отправке данных оплаты. Проверьте соединение с интернетом.');
        }
    };

    if (!hasValidOrderId) {
        return (
            <div className="payment-page">
                <div className="payment-container page-reveal">
                    <h1>Оплата заказа</h1>
                    <p>Номер оформленного заказа не найден.</p>
                    <button
                        className="cancel-button"
                        onClick={() => navigate('/order-success')}
                    >
                        Назад
                    </button>
                </div>
            </div>
        );
    }

    const handleCancel = () => {
        if (trackingTokenFromState && hasValidOrderId) {
            navigate(`/order-track/${resolvedOrderId}?t=${encodeURIComponent(trackingTokenFromState)}`);
            return;
        }

        navigate('/order-success', {
            state: {
                completedOrderId: resolvedOrderId,
                orderData: orderData
                    ? { ...orderData, orderId: resolvedOrderId }
                    : { orderId: resolvedOrderId },
            },
        });
    };

    return (
        <div className="payment-page">
            <div className="payment-container page-reveal">
                <h1>Оплата заказа</h1>

                {orderData && (
                    <div className="order-summary page-reveal page-reveal--delay-1">
                        <p><strong>Номер заказа:</strong> #{resolvedOrderId}</p>
                        <p><strong>Сумма к оплате:</strong> {(orderData.orderTotalAmount ?? orderData.displayTotalAmount ?? 0).toFixed(2)} ₽</p>
                    </div>
                )}

                <div className="payment-form page-reveal page-reveal--delay-2">
                    <h2>Тестовая оплата</h2>
                    <p>
                        Реквизиты банковской карты не вводятся на сайте магазина. Для production-оплаты
                        используется защищённый редирект платёжного провайдера.
                    </p>
                    {errorMessage && <p className="error-message">{errorMessage}</p>}

                    <div className="payment-actions">
                        <button className="pay-button" onClick={handlePayment}>Оплатить тестово</button>
                        <button className="cancel-button" onClick={handleCancel}>Отмена</button>
                    </div>
                </div>
            </div>
        </div>
    );
};

export default PaymentPage;