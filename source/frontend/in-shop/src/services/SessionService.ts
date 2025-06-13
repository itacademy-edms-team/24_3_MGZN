import axios from 'axios';

const API_BASE_URL = 'https://localhost:7275/api'; // Замените на ваш URL

export interface UserSessionDto {
  userIpaddress: string;
  createdAt: string; // Будем отправлять как строку в ISO формате
}

let isSessionCreationInProgress = false;

export const createUserSession = async (ipAddress: string): Promise<void> => {
    if (isSessionCreationInProgress) return;
    isSessionCreationInProgress = true;
    try {
    const requestData: UserSessionDto = {
      userIpaddress: ipAddress,
      createdAt: new Date().toISOString() // Преобразуем дату в ISO строку
    };

    await axios.post(`${API_BASE_URL}/UserSession`, requestData, {
      headers: {
        'Content-Type': 'application/json'
      }
    });
  } catch (error) {
    console.error('Ошибка при создании сессии:', error);
    throw error;
  }
};

export const getClientIp = async (): Promise<string> => {
  try {
    const response = await axios.get('https://api.ipify.org?format=json');
    return response.data.ip;
  } catch (error) {
    console.error('Ошибка при получении IP:', error);
    return '127.0.0.1'; // Fallback IP
  }
};