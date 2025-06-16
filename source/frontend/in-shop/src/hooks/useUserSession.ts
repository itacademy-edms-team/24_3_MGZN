import { useState, useEffect } from 'react';
import { createUserSession, getCurrentSessionId } from '../services/SessionService.ts';

export const useSession = () => {
  const [sessionId, setSessionId] = useState<number | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const initializeSession = async () => {
      try {
        setLoading(true);
        
        // Проверяем существующую сессию
        const existingSessionId = getCurrentSessionId();
        if (existingSessionId) {
          setSessionId(existingSessionId);
          return;
        }

        // Создаем новую сессию
        const newSessionId = await createUserSession();
        setSessionId(newSessionId);
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Session error');
      } finally {
        setLoading(false);
      }
    };

    initializeSession();
  }, []);

  return { sessionId, loading, error };
};