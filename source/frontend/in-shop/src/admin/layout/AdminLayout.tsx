import React, { useState } from 'react';
import { NavLink, Outlet, useNavigate } from 'react-router-dom';
import { useAdminAuth } from '../auth/AdminAuthContext.tsx';
import './AdminLayout.css';

const AdminLayout: React.FC = () => {
  const { user, logout } = useAdminAuth();
  const navigate = useNavigate();
  const [menuOpen, setMenuOpen] = useState(true);

  const handleLogout = () => {
    logout();
    navigate('/admin/login');
  };

  return (
    <div className="admin-layout">
      <header className="admin-header">
        <div style={{ display: 'flex', alignItems: 'center' }}>
          <button
            type="button"
            className="admin-burger"
            aria-label="Меню"
            onClick={() => setMenuOpen((v) => !v)}
          >
            ☰
          </button>
          <h1 className="admin-header__title">InShop Admin</h1>
        </div>
        <div style={{ display: 'flex', alignItems: 'center', gap: '1rem' }}>
          <span className="admin-header__user">{user?.email}</span>
          <button type="button" className="admin-btn admin-btn--secondary" onClick={handleLogout}>
            Выйти
          </button>
        </div>
      </header>

      <div className="admin-body">
        <nav className={`admin-sidebar ${menuOpen ? '' : 'admin-sidebar--hidden'}`}>
          <NavLink to="/admin" end>
            Dashboard
          </NavLink>
          <NavLink to="/admin/products">Товары</NavLink>
          <NavLink to="/admin/orders">Заказы</NavLink>
          <NavLink to="/admin/orders/drafts">Черновики</NavLink>
        </nav>
        <main className="admin-content">
          <Outlet />
        </main>
      </div>
    </div>
  );
};

export default AdminLayout;
