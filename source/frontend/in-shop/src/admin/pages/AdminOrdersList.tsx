import React, { useCallback, useEffect, useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import adminClient from '../api/adminClient';
import AdminPagination from '../components/AdminPagination';
import OrderDetailsModal from '../components/OrderDetailsModal';
import OrderStatusModal from '../components/OrderStatusModal';
import { AdminOrder, PagedResult } from '../types/adminTypes';
import { isTerminalOrderStatus } from '../utils/adminUtils';

interface Props {
  draftOnly?: boolean;
}

const PAGE_SIZE = 20;

const formatOrderDate = (dateStr: string): string => {
  const datePart = dateStr.split('T')[0];
  const [year, month, day] = datePart.split('-');
  if (!year || !month || !day) return dateStr;
  return `${day}.${month}.${year}`;
};

const ORDER_STATUS_OPTIONS = [
  { value: '', label: 'Все активные' },
  { value: 'Unpaid', label: 'Unpaid' },
  { value: 'Processing', label: 'Processing' },
  { value: 'Paid', label: 'Paid' },
  { value: 'Shipped', label: 'Shipped' },
  { value: 'Delivered', label: 'Delivered' },
  { value: 'Cancelled', label: 'Cancelled' },
];

const AdminOrdersList: React.FC<Props> = ({ draftOnly }) => {
  const [searchParams, setSearchParams] = useSearchParams();
  const statusParam = searchParams.get('status');
  const [data, setData] = useState<PagedResult<AdminOrder> | null>(null);
  const [page, setPage] = useState(1);
  const [loading, setLoading] = useState(true);
  const [selected, setSelected] = useState<AdminOrder | null>(null);
  const [detailsOrderId, setDetailsOrderId] = useState<number | null>(null);

  const load = useCallback(async (p: number) => {
    setLoading(true);
    try {
      const url = draftOnly ? '/Admin/orders/draft' : '/Admin/orders';
      const params: Record<string, unknown> = { page: p, pageSize: PAGE_SIZE };
      if (!draftOnly && statusParam) params.status = statusParam;
      const res = await adminClient.get<PagedResult<AdminOrder>>(url, { params });
      setData(res.data);
    } finally {
      setLoading(false);
    }
  }, [draftOnly, statusParam]);

  useEffect(() => {
    load(page);
  }, [page, load]);

  const handleStatusFilterChange = (e: React.ChangeEvent<HTMLSelectElement>) => {
    const nextStatus = e.target.value;
    const nextParams = new URLSearchParams(searchParams);
    if (nextStatus) {
      nextParams.set('status', nextStatus);
    } else {
      nextParams.delete('status');
    }
    setPage(1);
    setSearchParams(nextParams);
  };

  const paginationProps = data
    ? {
        page,
        totalCount: data.totalCount,
        pageSize: data.pageSize,
        onPageChange: setPage,
        disabled: loading,
      }
    : null;

  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <h2>{draftOnly ? 'Черновики заказов (Draft)' : 'Заказы'}</h2>
        {!draftOnly && (
          <label style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', fontWeight: 600 }}>
            Статус
            <select value={statusParam ?? ''} onChange={handleStatusFilterChange} disabled={loading}>
              {ORDER_STATUS_OPTIONS.map((option) => (
                <option key={option.value || 'all'} value={option.value}>
                  {option.label}
                </option>
              ))}
            </select>
          </label>
        )}
      </div>
      {loading && !data && <p>Загрузка…</p>}
      {data && (
        <div className="admin-card">
          {paginationProps && <AdminPagination {...paginationProps} />}
          <table className="admin-table">
            <thead>
              <tr>
                <th>ID</th>
                <th>Дата</th>
                <th>Статус</th>
                <th>Клиент</th>
                <th>Сумма</th>
                <th>Позиций</th>
                <th>Действия</th>
              </tr>
            </thead>
            <tbody>
              {data.items.map((o) => {
                const statusLocked = isTerminalOrderStatus(o.orderStatus);
                return (
                  <tr key={o.orderId}>
                    <td>{o.orderId}</td>
                    <td>{formatOrderDate(o.orderDate)}</td>
                    <td>
                      {o.orderStatus}
                      {o.rawOrderStatus && o.rawOrderStatus !== o.orderStatus && (
                        <small style={{ display: 'block', color: '#6c757d' }}>{o.rawOrderStatus}</small>
                      )}
                    </td>
                    <td>{o.customerFullname}</td>
                    <td>{o.orderTotalAmount}</td>
                    <td>{o.itemsCount}</td>
                    <td className="admin-table-actions">
                      <button
                        type="button"
                        className="admin-btn admin-btn--secondary"
                        onClick={() => setDetailsOrderId(o.orderId)}
                      >
                        Подробнее
                      </button>
                      <span
                        className="admin-tooltip-wrap"
                        title={
                          statusLocked
                            ? 'Статус нельзя изменить для завершённых или отменённых заказов'
                            : undefined
                        }
                      >
                        <button
                          type="button"
                          className="admin-btn"
                          disabled={statusLocked}
                          onClick={() => !statusLocked && setSelected(o)}
                        >
                          Статус
                        </button>
                      </span>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
          {paginationProps && <AdminPagination {...paginationProps} />}
        </div>
      )}
      {selected && (
        <OrderStatusModal order={selected} onClose={() => setSelected(null)} onUpdated={() => load(page)} />
      )}
      {detailsOrderId !== null && (
        <OrderDetailsModal orderId={detailsOrderId} onClose={() => setDetailsOrderId(null)} />
      )}
    </div>
  );
};

export default AdminOrdersList;
