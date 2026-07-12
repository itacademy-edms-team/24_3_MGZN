import React, { useEffect, useMemo, useRef, useState } from 'react';
import { Link, useParams, useSearchParams } from 'react-router-dom';
import { apiClient } from '../../api/client';
import './OrderTrackPage.css';

export interface OrderTrackItem {
  productName: string;
  quantity: number;
  unitPrice: number;
  lineTotal: number;
}

export interface OrderTrackDto {
  orderId: number;
  orderStatus: string;
  orderStatusDisplay: string;
  orderDate: string;
  orderTotalAmount: number;
  shipMethod: string;
  shipAddress?: string | null;
  customerFullname: string;
  payMethod: string;
  payStatus: string;
  items: OrderTrackItem[];
}

const HAPPY_PATH = ['Unpaid', 'Paid', 'Shipped', 'Delivered'] as const;

const STEP_LABELS: Record<string, string> = {
  Unpaid: 'Оформлен',
  Paid: 'Оплачен',
  Shipped: 'Отправлен',
  Delivered: 'Доставлен',
};

/** Интервал опроса статуса, пока заказ не в терминальном состоянии. */
const POLL_INTERVAL_MS = 4000;

const isTerminalStatus = (status: string) =>
  status === 'Delivered' || status === 'Cancelled';

const formatMoney = (value: number) =>
  new Intl.NumberFormat('ru-RU', { style: 'currency', currency: 'RUB' }).format(value);

const getTimelineState = (status: string) => {
  if (status === 'Cancelled') {
    return { cancelled: true, activeIndex: -1 };
  }

  if (status === 'Unpaid' || status === 'Processing' || status === 'Draft') {
    return { cancelled: false, activeIndex: 0 };
  }

  const idx = HAPPY_PATH.indexOf(status as (typeof HAPPY_PATH)[number]);
  return { cancelled: false, activeIndex: idx >= 0 ? idx : 0 };
};

const OrderTrackPage: React.FC = () => {
  const { orderId } = useParams<{ orderId: string }>();
  const [searchParams] = useSearchParams();
  const token = searchParams.get('t');

  const [order, setOrder] = useState<OrderTrackDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [statusFlash, setStatusFlash] = useState(false);
  const previousStatusRef = useRef<string | null>(null);

  useEffect(() => {
    const id = Number(orderId);
    if (!Number.isFinite(id) || id <= 0 || !token) {
      setError('Ссылка отслеживания некорректна или устарела.');
      setLoading(false);
      return;
    }

    let cancelled = false;
    let intervalId: ReturnType<typeof setInterval> | null = null;
    let flashTimeoutId: ReturnType<typeof setTimeout> | null = null;

    const stopPolling = () => {
      if (intervalId) {
        clearInterval(intervalId);
        intervalId = null;
      }
    };

    const applyOrder = (next: OrderTrackDto) => {
      const prevStatus = previousStatusRef.current;
      if (prevStatus !== null && prevStatus !== next.orderStatus) {
        setStatusFlash(true);
        if (flashTimeoutId) {
          clearTimeout(flashTimeoutId);
        }
        flashTimeoutId = setTimeout(() => {
          if (!cancelled) {
            setStatusFlash(false);
          }
        }, 700);
      }
      previousStatusRef.current = next.orderStatus;
      setOrder(next);
      setError(null);

      if (isTerminalStatus(next.orderStatus)) {
        stopPolling();
      }
    };

    const fetchOrder = async (isInitial: boolean): Promise<OrderTrackDto | null> => {
      try {
        const res = await apiClient.get<OrderTrackDto>(`/Order/track/${id}`, {
          params: { t: token },
        });
        if (cancelled) {
          return null;
        }

        applyOrder(res.data);
        return res.data;
      } catch {
        if (cancelled) {
          return null;
        }
        // Фоновый опрос не должен прятать уже показанный заказ при сетевом сбое.
        if (isInitial || previousStatusRef.current === null) {
          setError('Заказ не найден. Проверьте ссылку из письма.');
          setOrder(null);
        }
        return null;
      } finally {
        if (!cancelled && isInitial) {
          setLoading(false);
        }
      }
    };

    setLoading(true);
    setError(null);
    previousStatusRef.current = null;

    void fetchOrder(true).then((data) => {
      if (cancelled || !data || isTerminalStatus(data.orderStatus)) {
        return;
      }

      intervalId = setInterval(() => {
        void fetchOrder(false);
      }, POLL_INTERVAL_MS);
    });

    return () => {
      cancelled = true;
      stopPolling();
      if (flashTimeoutId) {
        clearTimeout(flashTimeoutId);
      }
    };
  }, [orderId, token]);

  const timeline = useMemo(
    () => (order ? getTimelineState(order.orderStatus) : { cancelled: false, activeIndex: 0 }),
    [order]
  );

  const isLive = Boolean(order && !isTerminalStatus(order.orderStatus));

  if (loading) {
    return (
      <div className="order-track-page order-track-page--loading">
        <div className="order-track-spinner" />
        <p>Загружаем статус заказа…</p>
      </div>
    );
  }

  if (error || !order) {
    return (
      <div className="order-track-page">
        <div className="order-track-container order-track-container--error page-reveal">
          <h1>Не удалось открыть заказ</h1>
          <p>{error || 'Заказ не найден.'}</p>
          <Link to="/" className="order-track-home-link">
            На главную
          </Link>
        </div>
      </div>
    );
  }

  return (
    <div className="order-track-page">
      <div className="order-track-container page-reveal">
        <header
          className={`order-track-header ${timeline.cancelled ? 'order-track-header--cancelled' : ''}`}
        >
          <p className="order-track-eyebrow">Отслеживание заказа</p>
          <h1>Заказ #{order.orderId}</h1>
          <p
            className={`order-track-status-badge ${statusFlash ? 'order-track-status-badge--flash' : ''}`}
          >
            {order.orderStatusDisplay}
          </p>
          {isLive && (
            <p className="order-track-live-hint">Статус обновляется автоматически</p>
          )}
        </header>

        <div className="order-track-content">
          {timeline.cancelled ? (
            <section className="order-track-card order-track-cancelled anim-fade-up" style={{ animationDelay: '0.05s' }}>
              <h2>Заказ отменён</h2>
              <p>Если это произошло по ошибке — напишите нам, ответив на письмо о заказе.</p>
            </section>
          ) : (
            <section className="order-track-card anim-fade-up" style={{ animationDelay: '0.05s' }}>
              <h2>Статус</h2>
              <ol className="order-track-timeline">
                {HAPPY_PATH.map((step, index) => {
                  const done = index <= timeline.activeIndex;
                  const current = index === timeline.activeIndex;
                  return (
                    <li
                      key={`${step}-${order.orderStatus}`}
                      className={[
                        'order-track-step',
                        done ? 'order-track-step--done' : '',
                        current ? 'order-track-step--current' : '',
                      ]
                        .filter(Boolean)
                        .join(' ')}
                      style={{ animationDelay: `${0.08 + index * 0.08}s` }}
                    >
                      <span className="order-track-step-dot" aria-hidden />
                      <span className="order-track-step-label">{STEP_LABELS[step]}</span>
                    </li>
                  );
                })}
              </ol>
            </section>
          )}

          <section className="order-track-card anim-fade-up" style={{ animationDelay: '0.2s' }}>
            <h2>Детали</h2>
            <div className="order-track-grid">
              <div>
                <span className="order-track-label">Дата</span>
                <span className="order-track-value">{order.orderDate}</span>
              </div>
              <div>
                <span className="order-track-label">Получатель</span>
                <span className="order-track-value">{order.customerFullname}</span>
              </div>
              <div>
                <span className="order-track-label">Оплата</span>
                <span className="order-track-value">{order.payMethod}</span>
              </div>
              <div>
                <span className="order-track-label">Доставка</span>
                <span className="order-track-value">{order.shipMethod}</span>
              </div>
              {order.shipAddress && (
                <div className="order-track-grid-full">
                  <span className="order-track-label">Адрес</span>
                  <span className="order-track-value">{order.shipAddress}</span>
                </div>
              )}
            </div>
          </section>

          <section className="order-track-card anim-fade-up" style={{ animationDelay: '0.32s' }}>
            <h2>Состав</h2>
            <ul className="order-track-items">
              {order.items.map((item, index) => (
                <li key={`${item.productName}-${index}`} className="order-track-item">
                  <div>
                    <span className="order-track-item-name">{item.productName}</span>
                    <span className="order-track-item-qty">
                      {item.quantity} шт. × {formatMoney(item.unitPrice)}
                    </span>
                  </div>
                  <span className="order-track-item-total">{formatMoney(item.lineTotal)}</span>
                </li>
              ))}
            </ul>
            <p className="order-track-total">
              Итого: <strong>{formatMoney(order.orderTotalAmount)}</strong>
            </p>
          </section>

          <div className="order-track-footer anim-fade-up" style={{ animationDelay: '0.42s' }}>
            <Link to="/">В каталог</Link>
          </div>
        </div>
      </div>
    </div>
  );
};

export default OrderTrackPage;
