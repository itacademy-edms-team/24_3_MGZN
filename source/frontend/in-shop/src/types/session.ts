
export interface UserSessionDto {
    userIpAddress?: string;
    userAgent?: string;
}

export interface SessionCreationResult {
    sessionId: number;
    orderId: number;
    expiresAt: string;
    message: string;
}

export interface SessionValidationResult {
    isValid: boolean;
    sessionId?: number;
    expiresAt?: string;
    message?: string;
}

export interface SessionState {
    sessionId: number | null;
    orderId: number | null;
    expiresAt: Date | null;
    isValid: boolean;
    isLoading: boolean;
    error: string | null;
}