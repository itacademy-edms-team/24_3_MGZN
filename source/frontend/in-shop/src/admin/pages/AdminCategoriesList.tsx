import React, { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import adminClient from '../api/adminClient';
import { CategoryDto } from '../types/adminTypes';

const AdminCategoriesList: React.FC = () => {
  const [categories, setCategories] = useState<CategoryDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const load = async () => {
    setLoading(true);
    setError(null);
    try {
      const res = await adminClient.get<CategoryDto[]>('/Category');
      setCategories(res.data);
    } catch (e: unknown) {
      const msg =
        (e as { response?: { data?: { message?: string } } })?.response?.data?.message ||
        'Не удалось загрузить категории';
      setError(msg);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    load();
  }, []);

  const handleDelete = async (id: number) => {
    if (!window.confirm('Удалить категорию?')) return;
    setError(null);
    try {
      await adminClient.delete(`/Category/${id}`);
      await load();
    } catch (e: unknown) {
      const msg =
        (e as { response?: { data?: { message?: string } } })?.response?.data?.message ||
        'Не удалось удалить категорию';
      setError(msg);
    }
  };

  return (
    <div>
      <div className="admin-page-header">
        <h2>Категории</h2>
        <Link to="/admin/categories/new" className="admin-btn">
          + Добавить
        </Link>
      </div>
      {loading && <p className="admin-muted">Загрузка…</p>}
      {error && <p className="admin-error">{error}</p>}
      <div className="admin-card">
        <table className="admin-table">
          <thead>
            <tr>
              <th>ID</th>
              <th>Название</th>
              <th>Изображение</th>
              <th>Действия</th>
            </tr>
          </thead>
          <tbody>
            {categories.map((category) => (
              <tr key={category.categoryId}>
                <td>{category.categoryId}</td>
                <td>{category.categoryName}</td>
                <td>{category.imageURL || '—'}</td>
                <td className="admin-table-actions">
                  <Link to={`/admin/categories/${category.categoryId}`}>Изменить</Link>
                  <button
                    type="button"
                    className="admin-btn--danger"
                    onClick={() => handleDelete(category.categoryId)}
                  >
                    Удалить
                  </button>
                </td>
              </tr>
            ))}
            {!loading && categories.length === 0 && (
              <tr>
                <td colSpan={4}>Категории не найдены</td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
};

export default AdminCategoriesList;
