import React, { useEffect, useState } from 'react';
import adminClient from '../api/adminClient.ts';
import { AdminOrderDetail } from '../types/adminTypes.ts';
import '../layout/AdminLayout.css';

interface Props {
  orderId: number;
  onClose: () => void;
}

/** Модалка с полной информацией о заказе: позиции, доставка, история статусов. */
const OrderDetailsModal: React.FC<Props> = ({ orderId, onClose }) => {
  const [details, setDetails] = useState<AdminOrderDetail | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    adminClient
      .get<AdminOrderDetail>(`/Admin/orders/${orderId}`)
      .then((r) => setDetails(r.data))
      .catch(() => setError('Не удалось загрузить детали заказа'))
      .finally(() => setLoading(false));
  }, [orderId]);

  return (
    <div className="admin-modal-overlay" onClick={onClose}>
      <div className="admin-card admin-order-details" onClick={(e) => e.stopPropagation()}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
          <h3>Заказ #{orderId}</h3>
          <button type="button" className="admin-btn admin-btn--secondary" onClick={onClose}>
            Закрыть
          </button>
        </div>
        {loading && <p>Загрузка…</p>}
        {error && <p className="admin-error">{error}</p>}
        {details && (
          <>
            <section className="admin-details-section">
              <h4>Общее</h4>
              <p>
                <strong>Статус:</strong> {details.orderStatus}
                {details.rawOrderStatus && details.rawOrderStatus !== details.orderStatus && (
                  <span style={{ color: '#6c757d' }}> (БД: {details.rawOrderStatus})</span>
                )}
              </p>
              <p><strong>Дата:</strong> {details.orderDate}</p>
              <p><strong>Сумма:</strong> {details.orderTotalAmount} ₽</p>
              <p><strong>Оплата:</strong> {details.payStatus} ({details.payMethod})</p>
            </section>

            <section className="admin-details-section">
              <h4>Покупатель</h4>
              <p>{details.customerFullname}</p>
              <p>{details.customerEmail}</p>
              <p>{details.customerPhoneNumber}</p>
              <p><strong>SessionId:</strong> {details.sessionId}</p>
            </section>

            <section className="admin-details-section">
              <h4>Доставка</h4>
              <p><strong>Способ:</strong> {details.shipMethod}</p>
              <p><strong>Адрес:</strong> {details.shipAddress || '—'}</p>
              <p><strong>ТК:</strong> {details.shipCompanyName || '—'}</p>
              <p><strong>Дата отгрузки:</strong> {details.shipDate || '—'}</p>
            </section>

            <section className="admin-details-section">
              <h4>Товары</h4>
              <table className="admin-table">
                <thead>
                  <tr>
                    <th>Название</th>
                    <th>Кол-во</th>
                    <th>Цена</th>
                    <th>Итого</th>
                  </tr>
                </thead>
                <tbody>
                  {details.items.map((item) => (
                    <tr key={item.orderItemId}>
                      <td>{item.productName}</td>
                      <td>{item.quantity}</td>
                      <td>{item.unitPrice}</td>
                      <td>{item.lineTotal}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </section>

            <section className="admin-details-section">
              <h4>История статусов</h4>
              {details.statusHistory.length === 0 ? (
                <p style={{ color: '#6c757d' }}>Записей пока нет</p>
              ) : (
                <ul className="admin-timeline">
                  {details.statusHistory.map((entry, idx) => (
                    <li key={`${entry.createdAt}-${idx}`}>
                      <time>{new Date(entry.createdAt).toLocaleString('ru-RU')}</time>
                      <span>
                        {entry.oldStatus ?? '—'} → <strong>{entry.newStatus}</strong>
                      </span>
                      <small>{entry.changedBy}</small>
                    </li>
                  ))}
                </ul>
              )}
            </section>
          </>
        )}
      </div>
    </div>
  );
};

export default OrderDetailsModal;
