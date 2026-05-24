import React from 'react';

interface Props {
  page: number;
  totalCount: number;
  pageSize: number;
  onPageChange: (page: number) => void;
  disabled?: boolean;
}

/** Синхронизированная пагинация для списков админки (сверху и снизу таблицы). */
const AdminPagination: React.FC<Props> = ({ page, totalCount, pageSize, onPageChange, disabled }) => {
  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize));

  return (
    <div className="admin-pagination" style={{ display: 'flex', gap: '0.5rem', alignItems: 'center', margin: '0.75rem 0' }}>
      <button
        type="button"
        className="admin-btn admin-btn--secondary"
        disabled={disabled || page <= 1}
        onClick={() => onPageChange(page - 1)}
      >
        Назад
      </button>
      <span>
        Стр. {page} / {totalPages} ({totalCount} записей)
      </span>
      <button
        type="button"
        className="admin-btn admin-btn--secondary"
        disabled={disabled || page >= totalPages}
        onClick={() => onPageChange(page + 1)}
      >
        Вперёд
      </button>
    </div>
  );
};

export default AdminPagination;
