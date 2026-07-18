// ============================================
// Файл: src/components/SessionHandler.tsx
// ============================================

import React from 'react';
import { useSessionContext } from '../context/SessionContext';

interface SessionHandlerProps {
  children: React.ReactNode;
  fallback?: React.ReactNode;
  errorFallback?: React.ReactNode;
}

const SessionHandler: React.FC<SessionHandlerProps> = ({
  children,
  fallback,
  errorFallback,
}) => {
  const { isLoading, isValid, error, recreateSession } = useSessionContext();

  if (isLoading) {
    return <>{fallback ?? <div>Загрузка...</div>}</>;
  }

  if (error || !isValid) {
    return (
      <>
        {errorFallback ?? (
          <div>
            <p>Ошибка: {error || 'Сессия не активна'}</p>
            <button type="button" onClick={() => { void recreateSession(); }}>
              Повторить
            </button>
          </div>
        )}
      </>
    );
  }

  return <>{children}</>;
};

export default SessionHandler;