import React, { useCallback, useEffect, useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import adminClient from '../api/adminClient.ts';
import AdminPagination from '../components/AdminPagination.tsx';
import OrderDetailsModal from '../components/OrderDetailsModal.tsx';
import OrderStatusModal from '../components/OrderStatusModal.tsx';
import { AdminOrder, PagedResult } from '../types/adminTypes.ts';
import { isTerminalOrderStatus } from '../utils/adminUtils.ts';

interface Props {
  draftOnly?: boolean;
}

const PAGE_SIZE = 20;

const AdminOrdersList: React.FC<Props> = ({ draftOnly }) => {
  const [searchParams] = useSearchParams();
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
      <h2>{draftOnly ? 'Черновики заказов (Draft)' : 'Заказы'}</h2>
      {loading && !data && <p>Загрузка…</p>}
      {data && (
        <div className="admin-card">
          {paginationProps && <AdminPagination {...paginationProps} />}
          <table className="admin-table">
            <thead>
              <tr>
                <th>ID</th>
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
