// ============================================
// Файл: src/api/client.ts
// ============================================

import axios, { AxiosInstance, AxiosError, AxiosRequestConfig, InternalAxiosRequestConfig } from 'axios';
import { API_BASE_URL } from '../config/api.js';

const STORAGE_KEY_ORDER_ID = 'currentOrderId';
const STORAGE_KEY_SESSION_ID = 'currentSessionId';
const SESSION_UPDATED_EVENT = 'session:updated';
const isDevelopment = process.env.NODE_ENV === 'development';

type RetriableRequestConfig = InternalAxiosRequestConfig & {
    _retry?: boolean;
    _skipSessionRetry?: boolean;
};

type SessionRetryRequestConfig = AxiosRequestConfig & {
    _skipSessionRetry?: boolean;
};

let sessionRecreatePromise: Promise<void> | null = null;

const isUserSessionEndpoint = (url?: string) => Boolean(url?.includes('/UserSession'));

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
        if (isDevelopment) {
            console.log(`[API Request] ${config.method?.toUpperCase()} ${config.url}`);
        }
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
        if (isDevelopment) {
            console.log(`[API Response] ${response.status} ${response.config.url}`);
        }
        return response;
    },
    async (error: AxiosError) => {
        const originalRequest = error.config as RetriableRequestConfig | undefined;
        
        // 401 - сессия истекла или невалидна
        if (
            error.response?.status === 401 &&
            originalRequest &&
            !originalRequest._skipSessionRetry &&
            !isUserSessionEndpoint(originalRequest.url)
        ) {
            if (isDevelopment) {
                console.warn('[API] Session expired (401)');
            }
            
            // Защита от бесконечного цикла повторных попыток
            if (originalRequest._retry) {
                console.error('[API] Retry limit reached');
                return Promise.reject(error);
            }
            
            // Если это не повторная попытка — пробуем пересоздать сессию
            if (!originalRequest._retry) {
                originalRequest._retry = true;
                
                try {
                    if (isDevelopment) {
                        console.log('[API] Attempting to recreate session...');
                    }
                    
                    if (!sessionRecreatePromise) {
                        sessionRecreatePromise = apiClient
                            .post('/UserSession', {}, { _skipSessionRetry: true } as SessionRetryRequestConfig)
                            .then((recreateResponse) => {
                                const data = recreateResponse?.data as
                                    | { orderId?: number; sessionId?: number }
                                    | undefined;

                                if (typeof data?.orderId === 'number') {
                                    localStorage.setItem(STORAGE_KEY_ORDER_ID, data.orderId.toString());
                                }
                                if (typeof data?.sessionId === 'number') {
                                    localStorage.setItem(STORAGE_KEY_SESSION_ID, data.sessionId.toString());
                                }

                                window.dispatchEvent(new Event(SESSION_UPDATED_EVENT));
                            })
                            .finally(() => {
                                sessionRecreatePromise = null;
                            });
                    }

                    await sessionRecreatePromise;
                    
                    if (isDevelopment) {
                        console.log('[API] Session recreated, retrying original request');
                    }
                    
                    // Повторяем оригинальный запрос с обновлённой кукой
                    return apiClient(originalRequest);
                    
                } catch (retryError) {
                    console.error('[API] Failed to recreate session', retryError);
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