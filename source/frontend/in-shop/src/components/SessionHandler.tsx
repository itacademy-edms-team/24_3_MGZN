import React from 'react';
import { useUserSession } from '../hooks/useUserSession.ts';

const SessionHandler: React.FC = () => {
  const { loading, error } = useUserSession();

  if (loading) {
    return <div>Инициализация сессии...</div>;
  }

  if (error) {
    return (
      <div className="error-message">
        Ошибка: {error}
        <br />
        <small>Используется резервный IP</small>
      </div>
    );
  }

  return null; // Или любой другой JSX, если нужно отобразить информацию о сессии
};

export default SessionHandler;