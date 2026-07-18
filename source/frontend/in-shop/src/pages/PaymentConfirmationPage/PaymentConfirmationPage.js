// Страница после return_url ЮKassa: синхронизация статуса на бэкенде + короткий polling.
import React, { useState, useEffect, useRef } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import { apiClient } from '../../api/client';
import { readCompletedOrder } from '../../utils/checkoutStorage';
import './PaymentConfirmationPage.css';

const MAX_POLL_ATTEMPTS = 15;
const POLL_INTERVAL_MS = 2000;
const isDevelopment = process.env.NODE_ENV === 'development';

const PaymentConfirmationPage = () => {
    const navigate = useNavigate();
    const location = useLocation();
    const confirmCalledRef = useRef(false);

    const searchParams = new URLSearchParams(location.search);
    const orderIdFromQuery = searchParams.get('orderId');
    const parsedQueryOrderId = orderIdFromQuery ? Number.parseInt(orderIdFromQuery, 10) : NaN;
    const orderId = location.state?.orderId
        ?? (!Number.isNaN(parsedQueryOrderId) ? parsedQueryOrderId : null);
    const trackingToken = location.state?.trackingToken
        ?? searchParams.get('t')
        ?? null;

    const [status, setStatus] = useState('checking');
    const [error, setError] = useState(null);
    const [provider, setProvider] = useState(null);
    const [trackingPath, setTrackingPath] = useState(null);

    const goToConfirmedOrder = () => {
        if (trackingToken && orderId) {
            navigate(`/order-track/${orderId}?t=${encodeURIComponent(trackingToken)}`, {
                replace: true,
            });
            return;
        }

        const storedOrder = readCompletedOrder();
        const sameOrder = storedOrder
            && Number(storedOrder.orderId) === Number(orderId);

        navigate('/order-success', {
            replace: true,
            state: {
                completedOrderId: orderId,
                orderData: sameOrder
                    ? storedOrder
                    : { orderId, payMethod: 'Онлайн' },
                paymentFailed: true,
            },
        });
    };

    useEffect(() => {
        if (!orderId) {
            alert('Неверные данные для подтверждения оплаты.');
            navigate('/');
        }
    }, [orderId, navigate]);

    useEffect(() => {
        if (!orderId) {
            return undefined;
        }

        let intervalId;
        let pollCount = 0;
        let cancelled = false;

        const loadTrackingLink = async () => {
            if (trackingToken && orderId) {
                if (!cancelled) {
                    setTrackingPath(`/order-track/${orderId}?t=${encodeURIComponent(trackingToken)}`);
                }
                return;
            }

            try {
                const res = await apiClient.get(`/Order/${orderId}/tracking-link`);
                const path = res.data?.trackingPath;
                if (!cancelled && path) {
                    setTrackingPath(path);
                }
            } catch (err) {
                if (isDevelopment) {
                    console.warn('tracking-link:', err.response?.data || err.message);
                }
            }
        };

        const runYooKassaConfirm = async () => {
            if (confirmCalledRef.current) {
                return;
            }
            confirmCalledRef.current = true;

            try {
                await apiClient.post('/Payment/confirm-yookassa', {
                    orderId,
                    ...(trackingToken ? { trackingToken } : {}),
                });
            } catch (err) {
                if (isDevelopment) {
                    console.warn('confirm-yookassa:', err.response?.data || err.message);
                }
            }
        };

        const checkPaymentStatus = async () => {
            try {
                const response = await apiClient.get(`/Payment/status/${orderId}`, {
                    params: trackingToken ? { t: trackingToken } : undefined,
                });
                const currentStatus = response.data?.Status ?? response.data?.status;

                if (!currentStatus) {
                    throw new Error(`Поле Status отсутствует в ответе: ${JSON.stringify(response.data)}`);
                }

                if (currentStatus === 'Payed') {
                    setStatus('paid');
                    if (intervalId) clearInterval(intervalId);
                    await loadTrackingLink();
                    return;
                }

                if (currentStatus !== 'Unpayed') {
                    throw new Error(`Неизвестный статус: ${currentStatus}`);
                }

                pollCount += 1;
                if (pollCount >= MAX_POLL_ATTEMPTS) {
                    setError('Оплата ещё не подтверждена. Подождите или обновите страницу.');
                    setStatus('error');
                    if (intervalId) clearInterval(intervalId);
                }
            } catch (err) {
                console.error('Ошибка проверки статуса оплаты:', err);
                setError(err.message);
                setStatus('error');
                if (intervalId) clearInterval(intervalId);
            }
        };

        const start = async () => {
            try {
                const res = await apiClient.get('/Payment/provider');
                const p = res.data?.provider ?? 'Mock';
                if (!cancelled) {
                    setProvider(p);
                }

                if (p.toLowerCase() === 'yookassa') {
                    await runYooKassaConfirm();
                }
            } catch {
                // confirm всё равно попробуем через polling
            }

            if (cancelled) return;

            await checkPaymentStatus();
            intervalId = setInterval(checkPaymentStatus, POLL_INTERVAL_MS);
        };

        start();

        return () => {
            cancelled = true;
            if (intervalId) clearInterval(intervalId);
        };
    }, [orderId, trackingToken]);

    if (!orderId) {
        return <div className="payment-confirmation-page">Загрузка...</div>;
    }

    if (status === 'error') {
        return (
            <div className="payment-confirmation-page">
                <div className="confirmation-container page-reveal">
                    <div className="error-icon" aria-hidden="true">!</div>
                    <h1>Оплата не подтверждена</h1>
                    <p>Заказ #{orderId} сохранён, но оплата не прошла или ещё не подтверждена.</p>
                    {error && <p className="error-detail">{error}</p>}
                    {provider?.toLowerCase() === 'yookassa' && (
                        <p className="hint">
                            Если вы уже оплатили заказ, обновите страницу — статус мог обновиться с задержкой.
                            Иначе вернитесь к заказу и повторите оплату.
                        </p>
                    )}
                    <div className="confirmation-actions">
                        <button type="button" className="done-button" onClick={goToConfirmedOrder}>
                            Вернуться к заказу
                        </button>
                        <button type="button" className="back-button" onClick={() => window.location.reload()}>
                            Обновить статус
                        </button>
                    </div>
                </div>
            </div>
        );
    }

    if (status === 'paid') {
        return (
            <div className="payment-confirmation-page">
                <div className="confirmation-container page-reveal">
                    <div className="success-icon">✓</div>
                    <h1>Заказ успешно оплачен</h1>
                    <p>Номер заказа: #{orderId}</p>
                    <div className="confirmation-actions">
                        {trackingPath && (
                            <button
                                type="button"
                                className="done-button"
                                onClick={() => navigate(trackingPath)}
                            >
                                Отследить заказ
                            </button>
                        )}
                        <button type="button" className="back-button" onClick={() => navigate('/')}>
                            На главную
                        </button>
                    </div>
                </div>
            </div>
        );
    }

    return (
        <div className="payment-confirmation-page">
            <div className="confirmation-container page-reveal">
                <div className="loading-spinner" />
                <h1>Проверка статуса оплаты</h1>
                <p>Заказ #{orderId}</p>
                <p>Пожалуйста, подождите...</p>
            </div>
        </div>
    );
};

export default PaymentConfirmationPage;
