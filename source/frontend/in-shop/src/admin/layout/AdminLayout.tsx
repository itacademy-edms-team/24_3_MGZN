import React, { useState } from 'react';
import { NavLink, Outlet, useNavigate } from 'react-router-dom';
import { useAdminAuth } from '../auth/AdminAuthContext';
import './AdminLayout.css';

const AdminLayout: React.FC = () => {
  const { user, logout } = useAdminAuth();
  const navigate = useNavigate();
  const [menuOpen, setMenuOpen] = useState(true);

  const handleLogout = async () => {
    await logout();
    navigate('/admin/login');
  };

  return (
    <div className="admin-layout">
      <header className="admin-header">
        <div className="admin-header__left">
          <button
            type="button"
            className="admin-burger"
            aria-label={menuOpen ? 'Закрыть меню' : 'Открыть меню'}
            aria-expanded={menuOpen}
            aria-controls="admin-sidebar"
            onClick={() => setMenuOpen((v) => !v)}
          >
            {menuOpen ? '✕' : '☰'}
          </button>
          <h1 className="admin-header__title">InShop Admin</h1>
        </div>
        <div className="admin-header__right">
          <span className="admin-header__user">{user?.email}</span>
          <button type="button" className="admin-btn admin-btn--secondary" onClick={handleLogout}>
            Выйти
          </button>
        </div>
      </header>

      <div className="admin-body">
        <nav
          id="admin-sidebar"
          className={`admin-sidebar ${menuOpen ? '' : 'admin-sidebar--hidden'}`}
          aria-hidden={!menuOpen}
        >
          <NavLink to="/admin/products">Товары</NavLink>
          <NavLink to="/admin/categories">Категории</NavLink>
          <NavLink to="/admin/ship-companies">Транспортные компании</NavLink>
          <NavLink to="/admin/orders">Заказы</NavLink>
          <NavLink to="/admin/orders/drafts">Черновики Заказов (Корзины)</NavLink>
        </nav>
        <main className="admin-content">
          <Outlet />
        </main>
      </div>
    </div>
  );
};

export default AdminLayout;
