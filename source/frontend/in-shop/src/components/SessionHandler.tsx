import React from 'react';
import { useSession } from '../hooks/useUserSession.ts';

const SessionHandler: React.FC = () => {
  const { sessionId, loading, error } = useSession();

  if (loading) {
    return <div>Создание сессии...</div>;
  }

  if (error) {
    return (
      <div className="error">
        Ошибка: {error}
        <button onClick={() => window.location.reload()}>Повторить</button>
      </div>
    );
  }
};

export default SessionHandler;