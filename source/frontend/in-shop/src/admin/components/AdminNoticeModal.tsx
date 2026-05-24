import React, { useCallback, useEffect } from 'react';
import '../layout/AdminLayout.css';

interface Props {
  /** Заголовок модального окна. */
  title?: string;
  /** Текст уведомления. */
  message: string;
  /** Подпись кнопки подтверждения. */
  confirmLabel?: string;
  onClose: () => void;
}

/**
 * Модальное уведомление с размытым фоном.
 * Закрытие: кнопка, клик по фону, Escape.
 */
const AdminNoticeModal: React.FC<Props> = ({
  title = 'Готово',
  message,
  confirmLabel = 'OK',
  onClose,
}) => {
  const handleClose = useCallback(() => onClose(), [onClose]);

  useEffect(() => {
    const onKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Escape') handleClose();
    };

    document.body.style.overflow = 'hidden';
    window.addEventListener('keydown', onKeyDown);

    return () => {
      document.body.style.overflow = '';
      window.removeEventListener('keydown', onKeyDown);
    };
  }, [handleClose]);

  return (
    <div className="admin-notice-overlay" role="dialog" aria-modal="true" aria-labelledby="admin-notice-title" onClick={handleClose}>
      <div className="admin-notice-modal" onClick={(e) => e.stopPropagation()}>
        <h3 id="admin-notice-title" className="admin-notice-modal__title">
          {title}
        </h3>
        <p className="admin-notice-modal__message">{message}</p>
        <button type="button" className="admin-btn admin-notice-modal__btn" onClick={handleClose}>
          {confirmLabel}
        </button>
      </div>
    </div>
  );
};

export default AdminNoticeModal;
