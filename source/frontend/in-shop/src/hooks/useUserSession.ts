import { useEffect, useState, useRef } from 'react';
import { createUserSession, getClientIp } from '../services/SessionService.ts';

export const useUserSession = () => {
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const isMounted = useRef(true); // Флаг монтирования компонента

  useEffect(() => {
    const controller = new AbortController(); // Для отмены запроса

    const initializeSession = async () => {
      try {
        setLoading(true);
        const ipAddress = await getClientIp();
        await createUserSession(ipAddress);
        
        if (!isMounted.current) return;
        setLoading(false);
      } catch (err) {
        if (!isMounted.current) return;
        setError(err instanceof Error ? err.message : 'Ошибка сессии');
        setLoading(false);
      }
    };

    initializeSession();

    return () => {
      isMounted.current = false;
      controller.abort();
    };
  }, []);

  return { loading, error };
};