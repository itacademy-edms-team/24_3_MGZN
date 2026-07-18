import React, { useCallback, useEffect, useRef, useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import adminClient from '../api/adminClient';
import AdminPagination from '../components/AdminPagination';
import OrderDetailsModal from '../components/OrderDetailsModal';
import OrderStatusModal from '../components/OrderStatusModal';
import { AdminOrder, AdminOrderDetail, PagedResult } from '../types/adminTypes';
import { isTerminalOrderStatus } from '../utils/adminUtils';

const detailToListOrder = (d: AdminOrderDetail): AdminOrder => ({
  orderId: d.orderId,
  orderStatus: d.orderStatus,
  rawOrderStatus: d.rawOrderStatus,
  orderDate: d.orderDate,
  customerFullname: d.customerFullname,
  customerEmail: d.customerEmail,
  customerPhoneNumber: d.customerPhoneNumber,
  orderTotalAmount: d.orderTotalAmount,
  payStatus: d.payStatus,
  itemsCount: d.items?.length ?? 0,
});

interface Props {
  draftOnly?: boolean;
}

const PAGE_SIZE = 20;
/** Интервал фонового обновления таблицы заказов. */
const POLL_INTERVAL_MS = 4000;
/** Сколько держать анимацию подсветки строки. */
const ROW_FLASH_MS = 1200;

type RowHighlight = 'new' | 'updated';

const formatOrderDate = (dateStr: string): string => {
  const datePart = dateStr.split('T')[0];
  const [year, month, day] = datePart.split('-');
  if (!year || !month || !day) return dateStr;
  return `${day}.${month}.${year}`;
};

const orderFingerprint = (o: AdminOrder): string =>
  [
    o.orderId,
    o.orderStatus,
    o.rawOrderStatus ?? '',
    o.payStatus,
    o.orderTotalAmount,
    o.itemsCount,
    o.customerFullname,
  ].join('|');

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
  const [rowHighlights, setRowHighlights] = useState<Record<number, RowHighlight>>({});
  const [orderIdInput, setOrderIdInput] = useState('');
  const [foundOrder, setFoundOrder] = useState<AdminOrder | null>(null);
  const [searchError, setSearchError] = useState<string | null>(null);
  const [searching, setSearching] = useState(false);

  const previousFingerprintsRef = useRef<Map<number, string>>(new Map());
  const flashTimeoutsRef = useRef<Map<number, ReturnType<typeof setTimeout>>>(new Map());
  const isInitialLoadRef = useRef(true);
  const searchMode = foundOrder !== null;

  const clearFlashTimeout = (orderId: number) => {
    const existing = flashTimeoutsRef.current.get(orderId);
    if (existing) {
      clearTimeout(existing);
      flashTimeoutsRef.current.delete(orderId);
    }
  };

  const scheduleHighlightClear = useCallback((orderId: number) => {
    clearFlashTimeout(orderId);
    const timeoutId = setTimeout(() => {
      setRowHighlights((prev) => {
        if (!(orderId in prev)) return prev;
        const next = { ...prev };
        delete next[orderId];
        return next;
      });
      flashTimeoutsRef.current.delete(orderId);
    }, ROW_FLASH_MS);
    flashTimeoutsRef.current.set(orderId, timeoutId);
  }, []);

  const applyOrderDiff = useCallback(
    (nextPage: PagedResult<AdminOrder>, animate: boolean) => {
      const nextMap = new Map<number, string>();
      const highlights: Record<number, RowHighlight> = {};

      for (const order of nextPage.items) {
        const fingerprint = orderFingerprint(order);
        nextMap.set(order.orderId, fingerprint);

        if (!animate) {
          continue;
        }

        const prevFingerprint = previousFingerprintsRef.current.get(order.orderId);
        if (prevFingerprint === undefined) {
          highlights[order.orderId] = 'new';
        } else if (prevFingerprint !== fingerprint) {
          highlights[order.orderId] = 'updated';
        }
      }

      previousFingerprintsRef.current = nextMap;
      setData(nextPage);

      if (!animate || Object.keys(highlights).length === 0) {
        return;
      }

      setRowHighlights((prev) => ({ ...prev, ...highlights }));

      for (const orderId of Object.keys(highlights).map(Number)) {
        scheduleHighlightClear(orderId);
      }
    },
    [scheduleHighlightClear]
  );

  const load = useCallback(
    async (p: number, options?: { silent?: boolean }) => {
      const silent = Boolean(options?.silent);
      if (!silent) {
        setLoading(true);
      }

      try {
        const url = draftOnly ? '/Admin/orders/draft' : '/Admin/orders';
        const params: Record<string, unknown> = { page: p, pageSize: PAGE_SIZE };
        if (!draftOnly && statusParam) params.status = statusParam;
        const res = await adminClient.get<PagedResult<AdminOrder>>(url, { params });

        const shouldAnimate = silent && !isInitialLoadRef.current;
        applyOrderDiff(res.data, shouldAnimate);
        isInitialLoadRef.current = false;
      } finally {
        if (!silent) {
          setLoading(false);
        }
      }
    },
    [applyOrderDiff, draftOnly, statusParam]
  );

  useEffect(() => {
    if (searchMode) return;
    isInitialLoadRef.current = true;
    previousFingerprintsRef.current = new Map();
    setRowHighlights({});
    void load(page);
  }, [page, load, searchMode]);

  // Фоновое обновление в реальном времени (polling), пока вкладка видима и нет модалки / поиска.
  useEffect(() => {
    if (searchMode) return;

    let cancelled = false;
    let intervalId: ReturnType<typeof setInterval> | null = null;
    const flashTimeouts = flashTimeoutsRef.current;

    const stop = () => {
      if (intervalId) {
        clearInterval(intervalId);
        intervalId = null;
      }
    };

    const tick = () => {
      if (cancelled || document.visibilityState === 'hidden') {
        return;
      }
      // Не дёргаем список, пока открыта смена статуса / детали.
      if (selected !== null || detailsOrderId !== null) {
        return;
      }
      void load(page, { silent: true });
    };

    const start = () => {
      stop();
      if (document.visibilityState === 'hidden') {
        return;
      }
      intervalId = setInterval(tick, POLL_INTERVAL_MS);
    };

    const onVisibility = () => {
      if (document.visibilityState === 'visible') {
        tick();
        start();
      } else {
        stop();
      }
    };

    start();
    document.addEventListener('visibilitychange', onVisibility);

    return () => {
      cancelled = true;
      stop();
      document.removeEventListener('visibilitychange', onVisibility);
      flashTimeouts.forEach((timeoutId) => clearTimeout(timeoutId));
      flashTimeouts.clear();
    };
  }, [detailsOrderId, load, page, searchMode, selected]);

  const handleStatusFilterChange = (e: React.ChangeEvent<HTMLSelectElement>) => {
    const nextStatus = e.target.value;
    const nextParams = new URLSearchParams(searchParams);
    if (nextStatus) {
      nextParams.set('status', nextStatus);
    } else {
      nextParams.delete('status');
    }
    setPage(1);
    setFoundOrder(null);
    setSearchError(null);
    setSearchParams(nextParams);
  };

  const handleSearchById = async (e: React.FormEvent) => {
    e.preventDefault();
    const trimmed = orderIdInput.trim();
    const id = Number(trimmed);
    if (!trimmed || !Number.isInteger(id) || id <= 0) {
      setSearchError('Введите корректный ID заказа');
      setFoundOrder(null);
      return;
    }

    setSearching(true);
    setSearchError(null);
    try {
      const res = await adminClient.get<AdminOrderDetail>(`/Admin/orders/${id}`);
      const detail = res.data;
      const isDraft =
        detail.orderStatus === 'Draft' ||
        detail.rawOrderStatus === 'Draft';

      if (draftOnly && !isDraft) {
        setFoundOrder(null);
        setSearchError(`Заказ #${id} не является черновиком`);
        return;
      }

      if (!draftOnly && isDraft) {
        setFoundOrder(null);
        setSearchError(`Заказ #${id} — черновик. Откройте раздел «Черновики»`);
        return;
      }

      setFoundOrder(detailToListOrder(detail));
    } catch (err: unknown) {
      setFoundOrder(null);
      const status = (err as { response?: { status?: number } })?.response?.status;
      setSearchError(status === 404 ? `Заказ #${id} не найден` : 'Не удалось найти заказ');
    } finally {
      setSearching(false);
    }
  };

  const clearSearch = () => {
    setFoundOrder(null);
    setOrderIdInput('');
    setSearchError(null);
    isInitialLoadRef.current = true;
    void load(page);
  };

  const refreshAfterStatusChange = async () => {
    if (foundOrder) {
      try {
        const res = await adminClient.get<AdminOrderDetail>(`/Admin/orders/${foundOrder.orderId}`);
        setFoundOrder(detailToListOrder(res.data));
      } catch {
        setFoundOrder(null);
        setSearchError('Заказ больше недоступен');
      }
      return;
    }
    void load(page);
  };

  const displayOrders: AdminOrder[] = foundOrder ? [foundOrder] : data?.items ?? [];
  const showTable = Boolean(foundOrder) || Boolean(data);

  const paginationProps =
    !searchMode && data
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
      <div className="admin-page-header">
        <h2>{draftOnly ? 'Черновики заказов (Draft)' : 'Заказы'}</h2>
        <div className="admin-page-header__tools">
          <form className="admin-order-search" onSubmit={handleSearchById}>
            <label className="admin-page-header__filters" htmlFor="admin-order-id-search">
              ID
              <input
                id="admin-order-id-search"
                type="text"
                inputMode="numeric"
                pattern="[0-9]*"
                placeholder="Напр. 42"
                value={orderIdInput}
                onChange={(e) => setOrderIdInput(e.target.value.replace(/\D/g, ''))}
                disabled={searching}
                autoComplete="off"
              />
            </label>
            <button type="submit" className="admin-btn" disabled={searching || !orderIdInput.trim()}>
              {searching ? 'Поиск…' : 'Найти'}
            </button>
            {searchMode && (
              <button type="button" className="admin-btn admin-btn--secondary" onClick={clearSearch}>
                К списку
              </button>
            )}
          </form>
          {!draftOnly && !searchMode && (
            <label className="admin-page-header__filters">
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
      </div>
      {searchError && <p className="admin-error">{searchError}</p>}
      {searchMode && (
        <p className="admin-muted">Результат поиска по ID #{foundOrder.orderId}</p>
      )}
      {loading && !data && !searchMode && <p className="admin-muted">Загрузка…</p>}
      {showTable && (
        <div className="admin-card">
          {paginationProps && <AdminPagination {...paginationProps} />}
          <table className="admin-table admin-table--live">
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
              {displayOrders.map((o) => {
                const statusLocked = isTerminalOrderStatus(o.orderStatus);
                const highlight = rowHighlights[o.orderId];
                return (
                  <tr
                    key={o.orderId}
                    className={[
                      highlight === 'new' ? 'admin-order-row--new' : '',
                      highlight === 'updated' ? 'admin-order-row--updated' : '',
                    ]
                      .filter(Boolean)
                      .join(' ')}
                  >
                    <td>{o.orderId}</td>
                    <td>{formatOrderDate(o.orderDate)}</td>
                    <td>
                      <span
                        className={
                          highlight === 'updated' ? 'admin-order-status--flash' : undefined
                        }
                      >
                        {o.orderStatus}
                      </span>
                      {o.rawOrderStatus && o.rawOrderStatus !== o.orderStatus && (
                        <small className="admin-muted admin-muted--block">{o.rawOrderStatus}</small>
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
              {!searchMode && data && data.items.length === 0 && (
                <tr>
                  <td colSpan={7}>Заказы не найдены</td>
                </tr>
              )}
            </tbody>
          </table>
          {paginationProps && <AdminPagination {...paginationProps} />}
        </div>
      )}
      {selected && (
        <OrderStatusModal
          order={selected}
          onClose={() => setSelected(null)}
          onUpdated={() => {
            void refreshAfterStatusChange();
          }}
        />
      )}
      {detailsOrderId !== null && (
        <OrderDetailsModal orderId={detailsOrderId} onClose={() => setDetailsOrderId(null)} />
      )}
    </div>
  );
};

export default AdminOrdersList;
