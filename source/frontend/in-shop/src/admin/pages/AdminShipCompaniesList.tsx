import React, { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import adminClient from '../api/adminClient';
import { ShipCompanyDto } from '../types/adminTypes';

const AdminShipCompaniesList: React.FC = () => {
  const [companies, setCompanies] = useState<ShipCompanyDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const load = async () => {
    setLoading(true);
    setError(null);
    try {
      const res = await adminClient.get<ShipCompanyDto[]>('/ShipCompany');
      setCompanies(res.data);
    } catch (e: unknown) {
      const msg =
        (e as { response?: { data?: { message?: string } } })?.response?.data?.message ||
        'Не удалось загрузить транспортные компании';
      setError(msg);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    load();
  }, []);

  const handleDelete = async (id: number) => {
    if (!window.confirm('Удалить транспортную компанию?')) return;
    setError(null);
    try {
      await adminClient.delete(`/ShipCompany/${id}`);
      await load();
    } catch (e: unknown) {
      const msg =
        (e as { response?: { data?: { message?: string } } })?.response?.data?.message ||
        'Не удалось удалить транспортную компанию';
      setError(msg);
    }
  };

  return (
    <div>
      <div className="admin-page-header">
        <h2>Транспортные компании</h2>
        <Link to="/admin/ship-companies/new" className="admin-btn">
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
              <th>Контакт</th>
              <th>Действия</th>
            </tr>
          </thead>
          <tbody>
            {companies.map((company) => (
              <tr key={company.shipCompanyId}>
                <td>{company.shipCompanyId}</td>
                <td>{company.shipCompanyName}</td>
                <td>{company.contact}</td>
                <td className="admin-table-actions">
                  <Link to={`/admin/ship-companies/${company.shipCompanyId}`}>Изменить</Link>
                  <button
                    type="button"
                    className="admin-btn--danger"
                    onClick={() => handleDelete(company.shipCompanyId)}
                  >
                    Удалить
                  </button>
                </td>
              </tr>
            ))}
            {!loading && companies.length === 0 && (
              <tr>
                <td colSpan={4}>Транспортные компании не найдены</td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
};

export default AdminShipCompaniesList;
