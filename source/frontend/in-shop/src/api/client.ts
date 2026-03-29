// ============================================
// Файл: src/api/client.ts
// ============================================

import axios, { AxiosInstance, AxiosError, InternalAxiosRequestConfig } from 'axios';

// ✅ Бэкенд на HTTPS
const API_BASE_URL = 'https://localhost:7275/api';

// Создаём инстанс axios
export const apiClient: AxiosInstance = axios.create({
    baseURL: API_BASE_URL,
    
    // ✅ КРИТИЧНО: отправлять HttpOnly cookie с кросс-доменными запросами
    withCredentials: true,
    
    headers: {
        'Content-Type': 'application/json',
    },
    timeout: 10000,
});

// Request interceptor - логирование запросов
apiClient.interceptors.request.use(
    (config: InternalAxiosRequestConfig) => {
        console.log(`[API Request] ${config.method?.toUpperCase()} ${config.url}`);
        // Для отладки куки (в консоли видно только не-HttpOnly куки):
        // console.log('[API] Cookies:', document.cookie);
        return config;
    },
    (error: AxiosError) => {
        console.error('[API Request Error]', error);
        return Promise.reject(error);
    }
);

// Response interceptor - обработка ошибок и авто-повтор сессии
apiClient.interceptors.response.use(
    (response) => {
        console.log(`[API Response] ${response.status} ${response.config.url}`);
        return response;
    },
    async (error: AxiosError) => {
        const originalRequest = error.config as InternalAxiosRequestConfig & { _retry?: boolean };
        
        // 401 - сессия истекла или невалидна
        if (error.response?.status === 401) {
            console.warn('[API] Session expired (401)');
            
            // Защита от бесконечного цикла повторных попыток
            if (originalRequest._retry) {
                console.error('[API] Retry limit reached, reloading page');
                window.location.reload();
                return Promise.reject(error);
            }
            
            // Если это не повторная попытка — пробуем пересоздать сессию
            if (!originalRequest._retry) {
                originalRequest._retry = true;
                
                try {
                    console.log('[API] Attempting to recreate session...');
                    
                    // Создаём новую сессию (кука установится автоматически)
                    await apiClient.post('/UserSession', {}, {
                        // Важно: не добавляем withCredentials здесь — он уже в инстансе
                    });
                    
                    console.log('[API] Session recreated, retrying original request');
                    
                    // Повторяем оригинальный запрос с обновлённой кукой
                    return apiClient(originalRequest);
                    
                } catch (retryError) {
                    console.error('[API] Failed to recreate session', retryError);
                    
                    // Если не удалось создать сессию — перезагружаем страницу
                    window.location.reload();
                    return Promise.reject(retryError);
                }
            }
        }
        
        // 500 - серверная ошибка
        if (error.response?.status === 500) {
            console.error('[API] Server error (500)', error.response?.data);
        }
        
        return Promise.reject(error);
    }
);

export default apiClient;