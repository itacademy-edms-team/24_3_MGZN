import React, { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import adminClient from '../api/adminClient.ts';
import AdminPagination from '../components/AdminPagination.tsx';
import { AdminProduct, PagedResult } from '../types/adminTypes.ts';

const AdminProductsList: React.FC = () => {
  const [data, setData] = useState<PagedResult<AdminProduct> | null>(null);
  const [page, setPage] = useState(1);
  const [loading, setLoading] = useState(true);

  const load = async (p: number) => {
    setLoading(true);
    try {
      const res = await adminClient.get<PagedResult<AdminProduct>>('/Admin/products', {
        params: { page: p, pageSize: 20 },
      });
      setData(res.data);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    load(page);
  }, [page]);

  const handleDelete = async (id: number) => {
    if (!window.confirm('Удалить товар?')) return;
    await adminClient.delete(`/Admin/products/${id}`);
    load(page);
  };

  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <h2>Товары</h2>
        <Link to="/admin/products/new" className="admin-btn" style={{ textDecoration: 'none' }}>
          + Добавить
        </Link>
      </div>
      {loading && <p>Загрузка…</p>}
      {data && (
        <div className="admin-card">
          <AdminPagination
            page={page}
            totalCount={data.totalCount}
            pageSize={data.pageSize}
            onPageChange={setPage}
            disabled={loading}
          />
          <table className="admin-table">
            <thead>
              <tr>
                <th>ID</th>
                <th>Название</th>
                <th>Цена</th>
                <th>Склад</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {data.items.map((p) => (
                <tr key={p.productId}>
                  <td>{p.productId}</td>
                  <td>{p.productName}</td>
                  <td>{p.productPrice}</td>
                  <td>
                    {p.productStockQuantity}
                  </td>
                  <td>
                    <Link to={`/admin/products/${p.productId}`}>Изменить</Link>
                    {' | '}
                    <button type="button" onClick={() => handleDelete(p.productId)} style={{ background: 'none', border: 'none', color: '#dc3545', cursor: 'pointer' }}>
                      Удалить
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
          <AdminPagination
            page={page}
            totalCount={data.totalCount}
            pageSize={data.pageSize}
            onPageChange={setPage}
            disabled={loading}
          />
        </div>
      )}
    </div>
  );
};

export default AdminProductsList;
