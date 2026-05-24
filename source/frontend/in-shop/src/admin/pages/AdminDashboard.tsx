import React from 'react';
import { Link } from 'react-router-dom';

const AdminDashboard: React.FC = () => (
  <div>
    <h2>Dashboard</h2>
    <div className="admin-card">
      <p>Управление каталогом и заказами InShop.</p>
      <ul>
        <li>
          <Link to="/admin/products">Товары</Link> — CRUD, загрузка изображений (до 5 МБ)
        </li>
        <li>
          <Link to="/admin/orders">Заказы</Link> — смена статуса по конечному автомату
        </li>
        <li>
          <Link to="/admin/orders/drafts">Черновики</Link> — корзины в статусе Draft
        </li>
      </ul>
    </div>
  </div>
);

export default AdminDashboard;
