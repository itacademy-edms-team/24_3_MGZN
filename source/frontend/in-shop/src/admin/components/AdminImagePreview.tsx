import React, { useCallback, useEffect, useState } from 'react';
import '../layout/AdminLayout.css';

interface Props {
  src: string;
  alt: string;
  /** Подпись над миниатюрой, например «Текущее изображение». */
  label?: string;
}

/**
 * Миниатюра изображения с возможностью развернуть на весь экран.
 * Закрытие: кнопка ×, клик по фону, Escape.
 */
const AdminImagePreview: React.FC<Props> = ({ src, alt, label }) => {
  const [expanded, setExpanded] = useState(false);

  const close = useCallback(() => setExpanded(false), []);

  useEffect(() => {
    if (!expanded) return;

    const onKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Escape') close();
    };

    // Блокируем прокрутку страницы под оверлеем
    document.body.style.overflow = 'hidden';
    window.addEventListener('keydown', onKeyDown);

    return () => {
      document.body.style.overflow = '';
      window.removeEventListener('keydown', onKeyDown);
    };
  }, [expanded, close]);

  return (
    <>
      <div className="admin-image-preview">
        {label && <p className="admin-image-preview__label">{label}</p>}
        <button
          type="button"
          className="admin-image-preview__thumb-btn"
          onClick={() => setExpanded(true)}
          aria-label={`Увеличить: ${alt}`}
        >
          <img src={src} alt={alt} />
          <span className="admin-image-preview__hint">Нажмите для увеличения</span>
        </button>
      </div>

      {expanded && (
        <div
          className="admin-image-lightbox"
          role="dialog"
          aria-modal="true"
          aria-label={alt}
          onClick={close}
        >
          <button
            type="button"
            className="admin-image-lightbox__close"
            onClick={close}
            aria-label="Закрыть предпросмотр"
          >
            ×
          </button>
          <div className="admin-image-lightbox__content" onClick={(e) => e.stopPropagation()}>
            <img src={src} alt={alt} />
          </div>
        </div>
      )}
    </>
  );
};

export default AdminImagePreview;
