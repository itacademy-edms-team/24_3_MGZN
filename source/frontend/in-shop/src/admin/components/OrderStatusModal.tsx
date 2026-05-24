import React, { useEffect, useState } from 'react';
import adminClient from '../api/adminClient.ts';
import { AdminOrder } from '../types/adminTypes.ts';
import { isTerminalOrderStatus } from '../utils/adminUtils.ts';
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
    <div
      style={{
        position: 'fixed',
        inset: 0,
        background: 'rgba(0,0,0,0.5)',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        zIndex: 200,
      }}
      onClick={onClose}
    >
      <div className="admin-card" style={{ maxWidth: 420, width: '90%' }} onClick={(e) => e.stopPropagation()}>
        <h3>Заказ #{order.orderId}</h3>
        <p>
          Текущий статус: <strong>{order.orderStatus}</strong>
          {order.rawOrderStatus && order.rawOrderStatus !== order.orderStatus && (
            <span style={{ color: '#6c757d' }}> (в БД: {order.rawOrderStatus})</span>
          )}
        </p>
        <label>Новый статус</label>
        <select
          value={selected}
          onChange={(e) => setSelected(e.target.value)}
          style={{ width: '100%', marginBottom: '1rem' }}
          disabled={terminal || allowed.length === 0}
        >
          {allowed.map((s) => (
            <option key={s} value={s}>
              {s}
            </option>
          ))}
        </select>
        {error && <p className="admin-error">{error}</p>}
        <div style={{ display: 'flex', gap: '0.5rem' }}>
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
