import React from 'react';
import '../layout/AdminLayout.css';

interface Props {
  /** Текст под спиннером. */
  message?: string;
}

/** Локальный оверлей загрузки поверх блока (форма, карточка). */
const AdminLoadingOverlay: React.FC<Props> = ({ message = 'Загрузка…' }) => (
  <div className="admin-loading-overlay" role="status" aria-live="polite" aria-busy="true">
    <div className="admin-spinner" aria-hidden="true" />
    <span className="admin-loading-overlay__message">{message}</span>
  </div>
);

export default AdminLoadingOverlay;
