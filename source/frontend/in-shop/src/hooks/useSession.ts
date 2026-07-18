import { useState, useEffect, useCallback, useRef } from 'react';
import sessionService from '../services/SessionService';
import { SessionState } from '../types/session';

const STORAGE_KEY_ORDER_ID = 'currentOrderId';
const STORAGE_KEY_SESSION_ID = 'currentSessionId';
const SESSION_UPDATED_EVENT = 'session:updated';
const isDevelopment = process.env.NODE_ENV === 'development';

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
    const isOperationRunning = useRef(false);
    const pendingRerun = useRef(false);
    const initGeneration = useRef(0);

    /**
     * Одна операция за раз. Повторные вызовы не abort'ят текущую
     * (иначе createSession может завершиться на сервере, но не сохраниться в state),
     * а ставятся в очередь на ещё один проход после завершения.
     */
    const runExclusive = useCallback(async (operation: () => Promise<void>) => {
        if (isOperationRunning.current) {
            pendingRerun.current = true;
            return;
        }

        isOperationRunning.current = true;
        try {
            do {
                pendingRerun.current = false;
                await operation();
            } while (pendingRerun.current);
        } finally {
            isOperationRunning.current = false;
        }
    }, []);

    const initializeSession = useCallback(async () => {
        await runExclusive(async () => {
            if (isInitialized.current) return;

            const generationAtStart = initGeneration.current;
            let initializedSuccessfully = false;

            try {
                setState(prev => ({ ...prev, isLoading: true, error: null }));

                const storedOrderId = localStorage.getItem(STORAGE_KEY_ORDER_ID);
                const storedSessionId = localStorage.getItem(STORAGE_KEY_SESSION_ID);
                let hasLocalSession = false;
                let localOrderId: number | null = null;
                let localSessionId: number | null = null;

                if (storedOrderId && storedSessionId) {
                    const orderId = parseInt(storedOrderId, 10);
                    const sessionId = parseInt(storedSessionId, 10);

                    if (!isNaN(orderId) && !isNaN(sessionId)) {
                        localOrderId = orderId;
                        localSessionId = sessionId;
                        hasLocalSession = true;
                        setState(prev => ({
                            ...prev,
                            isValid: true,
                            orderId,
                            sessionId,
                            isLoading: true,
                        }));
                    }
                }

                // Валидируем только при наличии локальных id (иначе лишний 401 и риск гонок)
                if (hasLocalSession) {
                    const validation = await sessionService.validateSession();

                    if (generationAtStart !== initGeneration.current) {
                        return;
                    }

                    const resolvedOrderId = validation.orderId ?? localOrderId;
                    const resolvedSessionId = validation.sessionId ?? localSessionId;

                    if (
                        validation.isValid &&
                        typeof resolvedOrderId === 'number' &&
                        typeof resolvedSessionId === 'number'
                    ) {
                        localStorage.setItem(STORAGE_KEY_ORDER_ID, resolvedOrderId.toString());
                        localStorage.setItem(STORAGE_KEY_SESSION_ID, resolvedSessionId.toString());

                        setState(prev => ({
                            ...prev,
                            isValid: true,
                            sessionId: resolvedSessionId,
                            orderId: resolvedOrderId,
                            expiresAt: validation.expiresAt ? new Date(validation.expiresAt) : prev.expiresAt,
                            isLoading: false,
                            error: null,
                        }));
                        initializedSuccessfully = true;
                    } else {
                        localStorage.removeItem(STORAGE_KEY_ORDER_ID);
                        localStorage.removeItem(STORAGE_KEY_SESSION_ID);
                    }
                }

                if (!initializedSuccessfully) {
                    if (generationAtStart !== initGeneration.current) {
                        return;
                    }

                    const result = await sessionService.createSession();

                    if (generationAtStart !== initGeneration.current) {
                        return;
                    }

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
                    initializedSuccessfully = true;
                }
            } catch (error) {
                if (generationAtStart !== initGeneration.current) {
                    return;
                }

                console.error('[Session] Initialization error:', error);

                setState(prev => ({
                    ...prev,
                    isLoading: false,
                    isValid: false,
                    error: error instanceof Error ? error.message : 'Failed to initialize session',
                }));
            } finally {
                // Не помечаем успех, если за время init пришёл invalidate (session:updated и т.п.)
                if (generationAtStart === initGeneration.current) {
                    isInitialized.current = initializedSuccessfully;
                }
            }
        });
    }, [runExclusive]);

    const recreateSession = useCallback(async () => {
        initGeneration.current += 1;
        isInitialized.current = false;

        await runExclusive(async () => {
            const generationAtStart = initGeneration.current;

            try {
                setState(prev => ({ ...prev, isLoading: true, error: null }));

                localStorage.removeItem(STORAGE_KEY_ORDER_ID);
                localStorage.removeItem(STORAGE_KEY_SESSION_ID);

                const result = await sessionService.createSession();

                if (generationAtStart !== initGeneration.current) {
                    return;
                }

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
                isInitialized.current = true;
            } catch (error) {
                if (generationAtStart !== initGeneration.current) {
                    return;
                }

                console.error('[Session] Recreation error:', error);

                setState(prev => ({
                    ...prev,
                    isLoading: false,
                    isValid: false,
                    error: error instanceof Error ? error.message : 'Failed to recreate session',
                }));

                throw error;
            }
        });
    }, [runExclusive]);

    const logout = useCallback(async () => {
        initGeneration.current += 1;
        isInitialized.current = false;

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

            if (isDevelopment) {
                console.log('[Session] Logged out');
            }
        }
    }, []);

    const updateOrderId = useCallback((orderId: number) => {
        localStorage.setItem(STORAGE_KEY_ORDER_ID, orderId.toString());
        setState(prev => ({ ...prev, orderId }));
        if (isDevelopment) {
            console.log('[Session] OrderId updated:', orderId);
        }
    }, []);

    const requestReinitialize = useCallback(() => {
        initGeneration.current += 1;
        isInitialized.current = false;
        initializeSession();
    }, [initializeSession]);

    useEffect(() => {
        initializeSession();
    }, [initializeSession]);

    useEffect(() => {
        const handleStorageChange = (event: StorageEvent) => {
            if (event.key === STORAGE_KEY_ORDER_ID || event.key === STORAGE_KEY_SESSION_ID) {
                if (isDevelopment) {
                    console.log('[Session] Storage changed in another tab, revalidating...');
                }
                requestReinitialize();
            }
        };

        window.addEventListener('storage', handleStorageChange);
        return () => window.removeEventListener('storage', handleStorageChange);
    }, [requestReinitialize]);

    useEffect(() => {
        const handleSessionUpdated = () => {
            if (isDevelopment) {
                console.log('[Session] Session updated event received, revalidating...');
            }
            requestReinitialize();
        };

        window.addEventListener(SESSION_UPDATED_EVENT, handleSessionUpdated);
        return () => window.removeEventListener(SESSION_UPDATED_EVENT, handleSessionUpdated);
    }, [requestReinitialize]);

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
        refresh: requestReinitialize,
    };
};

export default useSession;
