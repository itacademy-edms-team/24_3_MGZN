import { useState, useEffect, useCallback, useRef } from 'react';
import sessionService from '../services/SessionService.ts';
import { SessionState } from '../types/session.ts';

const STORAGE_KEY_ORDER_ID = 'currentOrderId';
const STORAGE_KEY_SESSION_ID = 'currentSessionId';
const SESSION_UPDATED_EVENT = 'session:updated';

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
    const isOperationRunning = useRef(false); // <-- Новый флаг
    const abortControllerRef = useRef<AbortController | null>(null); // <-- Для отмены

    const runWithLock = useCallback(
        async (operation: (signal: AbortSignal) => Promise<void>) => {
            if (isOperationRunning.current) {
                // Отменяем предыдущую операцию, если она была
                abortControllerRef.current?.abort('New operation started');
                abortControllerRef.current = new AbortController();
            }

            isOperationRunning.current = true;
            const controller = new AbortController();
            abortControllerRef.current = controller;

            try {
                await operation(controller.signal);
            } catch (error) {
                if (error instanceof Error && error.name === 'AbortError') {
                    console.log('[Session] Operation aborted');
                } else {
                    throw error;
                }
            } finally {
                if (abortControllerRef.current === controller) {
                    abortControllerRef.current = null;
                }
                isOperationRunning.current = false;
            }
        },
        []
    );

    const initializeSession = useCallback(async () => {
        if (isInitialized.current) return;

        await runWithLock(async (signal) => {
            if (signal.aborted) return;

            try {
                setState(prev => ({ ...prev, isLoading: true, error: null }));

                const storedOrderId = localStorage.getItem(STORAGE_KEY_ORDER_ID);
                const storedSessionId = localStorage.getItem(STORAGE_KEY_SESSION_ID);

                if (storedOrderId && storedSessionId) {
                    const orderId = parseInt(storedOrderId);
                    const sessionId = parseInt(storedSessionId);

                    if (!isNaN(orderId) && !isNaN(sessionId)) {
                        setState(prev => ({
                            ...prev,
                            isValid: true,
                            orderId,
                            sessionId,
                            isLoading: true,
                        }));
                    }
                }

                if (signal.aborted) return;

                const isValid = await sessionService.isSessionActive();

                if (signal.aborted) return;

                if (isValid) {
                    setState(prev => ({
                        ...prev,
                        isValid: true,
                        isLoading: false,
                        error: null,
                    }));
                } else {
                    localStorage.removeItem(STORAGE_KEY_ORDER_ID);
                    localStorage.removeItem(STORAGE_KEY_SESSION_ID);

                    if (signal.aborted) return;

                    const result = await sessionService.createSession();

                    if (signal.aborted) return;

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
                }
            } catch (error) {
                if (signal.aborted) return;

                console.error('[Session] Initialization error:', error);

                setState(prev => ({
                    ...prev,
                    isLoading: false,
                    isValid: false,
                    error: error instanceof Error ? error.message : 'Failed to initialize session',
                }));
            } finally {
                isInitialized.current = true;
            }
        });
    }, [runWithLock]);

    const recreateSession = useCallback(async () => {
        await runWithLock(async (signal) => {
            if (signal.aborted) return;

            try {
                setState(prev => ({ ...prev, isLoading: true, error: null }));

                localStorage.removeItem(STORAGE_KEY_ORDER_ID);
                localStorage.removeItem(STORAGE_KEY_SESSION_ID);

                if (signal.aborted) return;

                const result = await sessionService.createSession();

                if (signal.aborted) return;

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
            } catch (error) {
                if (signal.aborted) return;

                console.error('[Session] Recreation error:', error);

                setState(prev => ({
                    ...prev,
                    isLoading: false,
                    error: error instanceof Error ? error.message : 'Failed to recreate session',
                }));

                throw error;
            }
        });
    }, [runWithLock]);

    const logout = useCallback(async () => {
        try {
            await sessionService.logout();
        } catch (error) {
            console.error('[Session] Logout backend error:', error);
        } finally {
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
        }
    }, []);

    const updateOrderId = useCallback((orderId: number) => {
        localStorage.setItem(STORAGE_KEY_ORDER_ID, orderId.toString());
        setState(prev => ({ ...prev, orderId }));
        console.log('[Session] OrderId updated:', orderId);
    }, []);

    useEffect(() => {
        initializeSession();
    }, [initializeSession]);

    useEffect(() => {
        const handleStorageChange = (event: StorageEvent) => {
            if (event.key === STORAGE_KEY_ORDER_ID || event.key === STORAGE_KEY_SESSION_ID) {
                console.log('[Session] Storage changed in another tab, revalidating...');
                isInitialized.current = false; // <-- Сброс флага
                initializeSession();
            }
        };

        window.addEventListener('storage', handleStorageChange);
        return () => window.removeEventListener('storage', handleStorageChange);
    }, [initializeSession]);

    // Когда интерцептор пересоздаёт сессию в этой же вкладке, события storage не будет.
    // Поэтому слушаем кастомный сигнал и повторно подтягиваем состояние.
    useEffect(() => {
        const handleSessionUpdated = () => {
            console.log('[Session] Session updated event, revalidating...');
            isInitialized.current = false;
            initializeSession();
        };

        window.addEventListener(SESSION_UPDATED_EVENT, handleSessionUpdated);
        return () => window.removeEventListener(SESSION_UPDATED_EVENT, handleSessionUpdated);
    }, [initializeSession]);

    return {
        sessionId: state.sessionId,
        orderId: state.orderId,
        expiresAt: state.expiresAt,
        isValid: state.isValid,
        isLoading: state.isLoading,
        error: state.error,

        recreateSession,
        logout,
        updateOrderId,
        refresh: initializeSession,
    };
};

export default useSession;