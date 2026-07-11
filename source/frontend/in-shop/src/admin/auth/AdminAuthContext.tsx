import React, { createContext, useCallback, useContext, useEffect, useState } from 'react';
import adminClient, { ADMIN_TOKEN_KEY } from '../api/adminClient';
import { AdminAuthResponse, AdminMe } from '../types/adminTypes';

interface AdminAuthContextValue {
  user: AdminMe | null;
  loading: boolean;
  login: (email: string, password: string) => Promise<void>;
  logout: () => Promise<void>;
  isAuthenticated: boolean;
}

const AdminAuthContext = createContext<AdminAuthContextValue | null>(null);

export const AdminAuthProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [user, setUser] = useState<AdminMe | null>(null);
  const [loading, setLoading] = useState(true);

  const loadMe = useCallback(async () => {
    const token = sessionStorage.getItem(ADMIN_TOKEN_KEY);
    if (!token) {
      setUser(null);
      setLoading(false);
      return;
    }
    try {
      const res = await adminClient.get<AdminMe>('/Admin/auth/me');
      setUser(res.data);
    } catch {
      sessionStorage.removeItem(ADMIN_TOKEN_KEY);
      setUser(null);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    loadMe();
  }, [loadMe]);

  const login = async (email: string, password: string) => {
    const res = await adminClient.post<AdminAuthResponse>('/Admin/auth/login', { email, password });
    sessionStorage.setItem(ADMIN_TOKEN_KEY, res.data.token);
    await loadMe();
  };

  const logout = async () => {
    try {
      await adminClient.post('/Admin/auth/logout', {});
    } catch {
      // Backend may not support token revocation yet; still clear client state.
    } finally {
      sessionStorage.removeItem(ADMIN_TOKEN_KEY);
      setUser(null);
    }
  };

  return (
    <AdminAuthContext.Provider
      value={{
        user,
        loading,
        login,
        logout,
        isAuthenticated: !!user,
      }}
    >
      {children}
    </AdminAuthContext.Provider>
  );
};

export const useAdminAuth = () => {
  const ctx = useContext(AdminAuthContext);
  if (!ctx) throw new Error('useAdminAuth вне AdminAuthProvider');
  return ctx;
};
