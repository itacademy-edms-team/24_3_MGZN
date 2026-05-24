import React, { useState } from 'react';
import { useForm } from 'react-hook-form';
import { useNavigate } from 'react-router-dom';
import { useAdminAuth } from '../auth/AdminAuthContext.tsx';
import '../layout/AdminLayout.css';

interface LoginForm {
  email: string;
  password: string;
}

const AdminLogin: React.FC = () => {
  const { login, isAuthenticated, loading } = useAdminAuth();
  const navigate = useNavigate();
  const [error, setError] = useState<string | null>(null);
  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<LoginForm>();

  React.useEffect(() => {
    if (!loading && isAuthenticated) {
      navigate('/admin', { replace: true });
    }
  }, [isAuthenticated, loading, navigate]);

  const onSubmit = async (data: LoginForm) => {
    setError(null);
    try {
      await login(data.email, data.password);
      navigate('/admin', { replace: true });
    } catch (e: unknown) {
      const msg =
        (e as { response?: { data?: { message?: string } } })?.response?.data?.message ||
        'Ошибка входа';
      setError(msg);
    }
  };

  return (
    <div className="admin-layout" style={{ justifyContent: 'center', alignItems: 'center' }}>
      <div className="admin-card" style={{ maxWidth: 400, width: '100%', margin: '4rem auto' }}>
        <h2 style={{ marginTop: 0 }}>InShop Admin</h2>
        <p style={{ color: '#6c757d', fontSize: '0.9rem' }}>Вход по корпоративному email</p>
        <form className="admin-form" onSubmit={handleSubmit(onSubmit)}>
          <label>Email</label>
          <input
            type="email"
            {...register('email', { required: 'Укажите email' })}
            autoComplete="username"
          />
          {errors.email && <p className="admin-error">{errors.email.message}</p>}

          <label>Пароль</label>
          <input
            type="password"
            {...register('password', { required: 'Укажите пароль', minLength: { value: 8, message: 'Мин. 8 символов' } })}
            autoComplete="current-password"
          />
          {errors.password && <p className="admin-error">{errors.password.message}</p>}

          {error && <p className="admin-error">{error}</p>}

          <button type="submit" className="admin-btn" disabled={isSubmitting}>
            {isSubmitting ? 'Вход…' : 'Войти'}
          </button>
        </form>
      </div>
    </div>
  );
};

export default AdminLogin;
