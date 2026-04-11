import React, { createContext, useContext } from 'react';
import useSession from '../hooks/useSession.ts';

type SessionContextValue = ReturnType<typeof useSession>;

const SessionContext = createContext<SessionContextValue | null>(null);

export const SessionProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
    const value = useSession();
    return <SessionContext.Provider value={value}>{children}</SessionContext.Provider>;
};

export function useSessionContext(): SessionContextValue {
    const ctx = useContext(SessionContext);
    if (!ctx) {
        throw new Error('useSessionContext must be used within SessionProvider');
    }
    return ctx;
}
