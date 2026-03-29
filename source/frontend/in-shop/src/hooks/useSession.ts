// ============================================
// Файл: src/hooks/useSession.ts
// ============================================

import { useState, useEffect, useCallback, useRef } from 'react';
import sessionService from '../services/SessionService.ts';
import { SessionState } from '../types/session.ts';

const STORAGE_KEY_ORDER_ID = 'currentOrderId';
const STORAGE_KEY_SESSION_ID = 'currentSessionId';

export const useSession = () => {
    const [state, setState] = useState<SessionState>({
        sessionId: null,
        orderId: null,
        expiresAt: null,
        isValid: false,
        isLoading: true,
        error: null,
    });
    
    const isInitialized = useRef(false);
    const isRecreating = useRef(false);

    /**
     * Инициализация сессии при загрузке приложения
     */
    const initializeSession = useCallback(async () => {
        if (isInitialized.current) return;
        
        try {
            setState(prev => ({ ...prev, isLoading: true, error: null }));
            
            // 1. Проверяем существующую сессию на бэкенде
            const isValid = await sessionService.isSessionActive();
            
            if (isValid) {
                // Сессия валидна - восстанавливаем из localStorage
                const storedOrderId = localStorage.getItem(STORAGE_KEY_ORDER_ID);
                const storedSessionId = localStorage.getItem(STORAGE_KEY_SESSION_ID);
                
                setState(prev => ({
                    ...prev,
                    isValid: true,
                    orderId: storedOrderId ? parseInt(storedOrderId) : null,
                    sessionId: storedSessionId ? parseInt(storedSessionId) : null,
                    isLoading: false,
                }));
                
                console.log('[Session] Existing session validated');
            } else {
                // 2. Сессия невалидна - создаём новую
                console.log('[Session] Creating new session...');
                const result = await sessionService.createSession();
                
                // Сохраняем в localStorage
                localStorage.setItem(STORAGE_KEY_ORDER_ID, result.orderId.toString());
                localStorage.setItem(STORAGE_KEY_SESSION_ID, result.sessionId.toString());
                
                setState(prev => ({
                    ...prev,
                    isValid: true,
                    sessionId: result.sessionId,
                    orderId: result.orderId,
                    expiresAt: new Date(result.expiresAt),
                    isLoading: false,
                }));
                
                console.log('[Session] New session created:', result);
            }
        } catch (error) {
            console.error('[Session] Initialization error:', error);
            
            setState(prev => ({
                ...prev,
                isLoading: false,
                error: error instanceof Error ? error.message : 'Failed to initialize session',
            }));
        } finally {
            isInitialized.current = true;
        }
    }, []);

    /**
     * Принудительное создание новой сессии
     */
    const recreateSession = useCallback(async () => {
        if (isRecreating.current) return;
        
        try {
            isRecreating.current = true;
            setState(prev => ({ ...prev, isLoading: true, error: null }));
            
            // Очищаем localStorage
            localStorage.removeItem(STORAGE_KEY_ORDER_ID);
            localStorage.removeItem(STORAGE_KEY_SESSION_ID);
            
            // Создаём новую сессию
            const result = await sessionService.createSession();
            
            localStorage.setItem(STORAGE_KEY_ORDER_ID, result.orderId.toString());
            localStorage.setItem(STORAGE_KEY_SESSION_ID, result.sessionId.toString());
            
            setState(prev => ({
                ...prev,
                isValid: true,
                sessionId: result.sessionId,
                orderId: result.orderId,
                expiresAt: new Date(result.expiresAt),
                isLoading: false,
                error: null,
            }));
            
            console.log('[Session] Session recreated');
            
            return result;
        } catch (error) {
            console.error('[Session] Recreation error:', error);
            
            setState(prev => ({
                ...prev,
                isLoading: false,
                error: error instanceof Error ? error.message : 'Failed to recreate session',
            }));
            
            throw error;
        } finally {
            isRecreating.current = false;
        }
    }, []);

    /**
     * Завершение сессии (logout)
     */
    const logout = useCallback(async () => {
        try {
            await sessionService.logout();
            
            // Очищаем localStorage
            localStorage.removeItem(STORAGE_KEY_ORDER_ID);
            localStorage.removeItem(STORAGE_KEY_SESSION_ID);
            
            setState({
                sessionId: null,
                orderId: null,
                expiresAt: null,
                isValid: false,
                isLoading: false,
                error: null,
            });
            
            console.log('[Session] Logged out');
        } catch (error) {
            console.error('[Session] Logout error:', error);
            
            // Даже если ошибка на бэкенде - очищаем локально
            localStorage.removeItem(STORAGE_KEY_ORDER_ID);
            localStorage.removeItem(STORAGE_KEY_SESSION_ID);
            
            setState(prev => ({
                ...prev,
                isValid: false,
                isLoading: false,
            }));
        }
    }, []);

    /**
     * Обновление данных сессии (например, после создания заказа)
     */
    const updateOrderId = useCallback((orderId: number) => {
        localStorage.setItem(STORAGE_KEY_ORDER_ID, orderId.toString());
        setState(prev => ({ ...prev, orderId }));
        console.log('[Session] OrderId updated:', orderId);
    }, []);

    // Инициализация при монтировании
    useEffect(() => {
        initializeSession();
    }, [initializeSession]);

    // Слушаем событие storage для синхронизации между вкладками
    useEffect(() => {
        const handleStorageChange = (event: StorageEvent) => {
            if (event.key === STORAGE_KEY_ORDER_ID || event.key === STORAGE_KEY_SESSION_ID) {
                console.log('[Session] Storage changed in another tab, revalidating...');
                initializeSession();
            }
        };
        
        window.addEventListener('storage', handleStorageChange);
        return () => window.removeEventListener('storage', handleStorageChange);
    }, [initializeSession]);

    return {
        // State
        sessionId: state.sessionId,
        orderId: state.orderId,
        expiresAt: state.expiresAt,
        isValid: state.isValid,
        isLoading: state.isLoading,
        error: state.error,
        
        // Actions
        recreateSession,
        logout,
        updateOrderId,
        refresh: initializeSession,
    };
};

export default useSession;