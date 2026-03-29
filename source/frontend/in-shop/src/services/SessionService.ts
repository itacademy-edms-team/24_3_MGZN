// ============================================
// Файл: src/services/sessionService.ts
// ============================================

import apiClient from '../api/client.ts';
import { UserSessionDto, SessionCreationResult, SessionValidationResult } from '../types/session.ts';

export const sessionService = {
    /**
     * Создание новой сессии
     */
    createSession: async (dto?: UserSessionDto): Promise<SessionCreationResult> => {
        const response = await apiClient.post<SessionCreationResult>('/UserSession', dto || {});
        return response.data;
    },
    
    /**
     * Валидация текущей сессии
     */
    validateSession: async (): Promise<SessionValidationResult> => {
        const response = await apiClient.get<SessionValidationResult>('/UserSession/validate');
        return response.data;
    },
    
    /**
     * Завершение сессии (logout)
     */
    logout: async (): Promise<void> => {
        await apiClient.post('/UserSession/logout', {});
    },
    
    /**
     * Проверка, активна ли сессия (без выбрасывания ошибки)
     */
    isSessionActive: async (): Promise<boolean> => {
        try {
            const result = await sessionService.validateSession();
            return result.isValid;
        } catch {
            return false;
        }
    },
};

export default sessionService;