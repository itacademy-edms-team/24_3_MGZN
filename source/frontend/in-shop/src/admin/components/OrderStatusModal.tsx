import React, { useEffect, useState } from 'react';
import adminClient from '../api/adminClient';
import { AdminOrder } from '../types/adminTypes';
import { isTerminalOrderStatus } from '../utils/adminUtils';
import '../layout/AdminLayout.css';

interface Props {
  order: AdminOrder;
  onClose: () => void;
  onUpdated: () => void;
}

const OrderStatusModal: React.FC<Props> = ({ order, onClose, onUpdated }) => {
  const [allowed, setAllowed] = useState<string[]>([]);
  const [selected, setSelected] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const terminal = isTerminalOrderStatus(order.orderStatus);

  useEffect(() => {
    if (terminal) {
      setError('Статус нельзя изменить для завершённых или отменённых заказов');
      return;
    }
    adminClient
      .get<string[]>(`/Admin/orders/${order.orderId}/allowed-statuses`)
      .then((r) => {
        setAllowed(r.data);
        if (r.data.length > 0) setSelected(r.data[0]);
      })
      .catch(() => setError('Не удалось загрузить допустимые статусы'));
  }, [order.orderId, terminal]);

  const submit = async () => {
    if (!selected) return;
    setLoading(true);
    setError(null);
    try {
      await adminClient.put(`/Admin/orders/${order.orderId}/status`, { newStatus: selected });
      onUpdated();
      onClose();
    } catch (e: unknown) {
      const msg =
        (e as { response?: { data?: { message?: string } } })?.response?.data?.message ||
        'Ошибка смены статуса';
      setError(msg);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="admin-modal-overlay" onClick={onClose}>
      <div className="admin-card admin-card--modal" onClick={(e) => e.stopPropagation()}>
        <div className="admin-modal-header">
          <h3>Заказ #{order.orderId}</h3>
        </div>
        <p>
          Текущий статус: <strong>{order.orderStatus}</strong>
          {order.rawOrderStatus && order.rawOrderStatus !== order.orderStatus && (
            <span className="admin-muted"> (в БД: {order.rawOrderStatus})</span>
          )}
        </p>
        <label htmlFor="admin-order-status">Новый статус</label>
        <select
          id="admin-order-status"
          className="admin-status-select"
          value={selected}
          onChange={(e) => setSelected(e.target.value)}
          disabled={terminal || allowed.length === 0}
        >
          {allowed.map((s) => (
            <option key={s} value={s}>
              {s}
            </option>
          ))}
        </select>
        {error && <p className="admin-error">{error}</p>}
        <div className="admin-form-actions">
          <button type="button" className="admin-btn" onClick={submit} disabled={loading || !selected || terminal}>
            Сохранить
          </button>
          <button type="button" className="admin-btn admin-btn--secondary" onClick={onClose}>
            Отмена
          </button>
        </div>
      </div>
    </div>
  );
};

export default OrderStatusModal;
