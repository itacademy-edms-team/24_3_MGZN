import axios from 'axios';

interface SessionData {
  sessionId: number;
  userIpaddress: string;
  createdAt: string;
}

interface SessionCreationResult {
  sessionId: number;
  orderId: number; // Добавляем поле для OrderId
  message: string;
}

// Глобальное состояние
let sessionPromise: Promise<number> | null = null;

export const createUserSession = async (): Promise<number> => {
  // Если запрос уже в процессе - возвращаем существующий промис
  if (sessionPromise) {
    return sessionPromise;
  }

  sessionPromise = (async () => {
    try {
      // 1. Получаем IP клиента
      const ipResponse = await axios.get('https://api.ipify.org?format=json');
      const ip = ipResponse.data.ip || '127.0.0.1';

      // 2. Отправляем запрос на создание сессии
      const response = await axios.post<SessionCreationResult>('https://localhost:7275/api/UserSession', {
        userIpaddress: ip,
        createdAt: new Date().toISOString(),
      }, {
        headers: {
          'Content-Type': 'application/json',
          'Accept': 'application/json',
        },
      });

      // 3. Сохраняем данные сессии
      localStorage.setItem('sessionId', response.data.sessionId.toString());
      localStorage.setItem('orderId', response.data.orderId.toString()); // Сохраняем OrderId

      return response.data.sessionId;
    } catch (error) {
      console.error('Session creation failed:', error);
      throw error;
    } finally {
      sessionPromise = null;
    }
  })();

  return sessionPromise;
};

export const getCurrentSessionId = (): number | null => {
  const sessionId = localStorage.getItem('sessionId');
  return sessionId ? parseInt(sessionId) : null;
};

export const getCurrentOrderId = (): number | null => {
  const orderId = localStorage.getItem('orderId');
  return orderId ? parseInt(orderId) : null;
};