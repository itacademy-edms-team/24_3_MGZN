import React, { useEffect, useState } from 'react';
import { useForm } from 'react-hook-form';
import { useNavigate, useParams } from 'react-router-dom';
import adminClient from '../api/adminClient';
import AdminLoadingOverlay from '../components/AdminLoadingOverlay';
import AdminNoticeModal from '../components/AdminNoticeModal';
import { ShipCompanyCreateDto, ShipCompanyDto } from '../types/adminTypes';

interface ShipCompanyFormValues {
  shipCompanyName: string;
  contact: string;
}

const AdminShipCompanyForm: React.FC = () => {
  const { id } = useParams();
  const isNew = !id || id === 'new';
  const navigate = useNavigate();
  const [error, setError] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<ShipCompanyFormValues>();

  useEffect(() => {
    if (!isNew && id) {
      adminClient
        .get<ShipCompanyDto>(`/ShipCompany/${id}`)
        .then((r) => {
          reset({
            shipCompanyName: r.data.shipCompanyName,
            contact: r.data.contact,
          });
        })
        .catch(() => setError('Не удалось загрузить транспортную компанию'));
    }
  }, [id, isNew, reset]);

  const onSubmit = async (values: ShipCompanyFormValues) => {
    setError(null);
    try {
      if (isNew) {
        const body: ShipCompanyCreateDto = values;
        await adminClient.post('/ShipCompany', body);
        setSuccessMessage('Транспортная компания успешно создана.');
      } else {
        await adminClient.put('/ShipCompany', {
          shipCompanyId: Number(id),
          ...values,
        } satisfies ShipCompanyDto);
        setSuccessMessage('Транспортная компания успешно сохранена.');
      }
    } catch (e: unknown) {
      const msg =
        (e as { response?: { data?: { message?: string } } })?.response?.data?.message ||
        'Ошибка сохранения транспортной компании';
      setError(msg);
    }
  };

  return (
    <div>
      <h2>{isNew ? 'Новая транспортная компания' : `Транспортная компания #${id}`}</h2>
      <form className="admin-card admin-form admin-form--overlay-host" onSubmit={handleSubmit(onSubmit)}>
        {isSubmitting && <AdminLoadingOverlay message="Сохранение транспортной компании…" />}
        <label>Название</label>
        <input {...register('shipCompanyName', { required: 'Обязательно', minLength: 2 })} />
        {errors.shipCompanyName && <p className="admin-error">{errors.shipCompanyName.message}</p>}

        <label>Контакт</label>
        <input {...register('contact', { required: 'Обязательно', minLength: 2 })} />
        {errors.contact && <p className="admin-error">{errors.contact.message}</p>}

        {error && <p className="admin-error">{error}</p>}

        <div className="admin-form-actions">
          <button type="submit" className="admin-btn" disabled={isSubmitting}>
            Сохранить
          </button>
          <button
            type="button"
            className="admin-btn admin-btn--secondary"
            disabled={isSubmitting}
            onClick={() => navigate(-1)}
          >
            Отмена
          </button>
        </div>
      </form>

      {successMessage && (
        <AdminNoticeModal
          title="Сохранено"
          message={successMessage}
          confirmLabel="К списку транспортных компаний"
          onClose={() => {
            setSuccessMessage(null);
            navigate('/admin/ship-companies');
          }}
        />
      )}
    </div>
  );
};

export default AdminShipCompanyForm;
