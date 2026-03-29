// ============================================
// Файл: src/components/SessionHandler.tsx
// ============================================

import React from 'react';
import useSession from '../hooks/useSession.ts';

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
  const { isLoading, error, recreateSession } = useSession();

  if (isLoading) {
    return fallback ?? <div>Загрузка...</div>;
  }

  if (error) {
    return errorFallback ?? (
      <div>
        <p>Ошибка: {error}</p>
        <button onClick={recreateSession}>Повторить</button>
      </div>
    );
  }

  return <>{children}</>;
};

export default SessionHandler;