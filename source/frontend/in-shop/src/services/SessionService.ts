// ============================================
// Файл: src/services/sessionService.ts
// ============================================

import apiClient from '../api/client';
import axios from 'axios';
import { UserSessionDto, SessionCreationResult, SessionValidationResult } from '../types/session';

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
        try {
            const response = await apiClient.get<SessionValidationResult>('/UserSession/validate');
            return response.data;
        } catch (error) {
            if (axios.isAxiosError(error) && error.response?.status === 401) {
                return {
                    isValid: false,
                    message: error.response.data?.message || 'Session is invalid',
                };
            }

            throw error;
        }
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