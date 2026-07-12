// src/components/Modal.tsx

import React from 'react';
import { createPortal } from 'react-dom';
import './Modal.css';

interface ModalProps {
  isOpen: boolean;
  onClose: () => void;
  children: React.ReactNode;
  title?: string;
  overlayClassName?: string;
  contentClassName?: string;
  bodyClassName?: string;
  footer?: React.ReactNode;
}

const Modal: React.FC<ModalProps> = ({
  isOpen,
  onClose,
  children,
  title,
  overlayClassName = '',
  contentClassName = '',
  bodyClassName = '',
  footer,
}) => {
  if (!isOpen) return null;

  return createPortal(
    <div
      className={`modal-overlay${overlayClassName ? ` ${overlayClassName}` : ''}`}
      onClick={onClose}
      role="presentation"
    >
      <div
        className={`modal-content${contentClassName ? ` ${contentClassName}` : ''}`}
        onClick={e => e.stopPropagation()}
        role="dialog"
        aria-modal="true"
        aria-labelledby={title ? 'modal-title' : undefined}
      >
        <div className="modal-header">
          {title && <h3 id="modal-title">{title}</h3>}
          <button type="button" className="modal-close" onClick={onClose} aria-label="Закрыть">
            &times;
          </button>
        </div>
        <div className={`modal-body${bodyClassName ? ` ${bodyClassName}` : ''}`}>
          {children}
        </div>
        {footer}
      </div>
    </div>,
    document.body
  );
};

export default Modal;
