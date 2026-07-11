import React, { useEffect, useState } from 'react';
import { useForm } from 'react-hook-form';
import { useNavigate, useParams } from 'react-router-dom';
import adminClient from '../api/adminClient';
import AdminLoadingOverlay from '../components/AdminLoadingOverlay';
import AdminNoticeModal from '../components/AdminNoticeModal';
import { CategoryCreateDto, CategoryDto } from '../types/adminTypes';

interface CategoryFormValues {
  categoryName: string;
}

const AdminCategoryForm: React.FC = () => {
  const { id } = useParams();
  const isNew = !id || id === 'new';
  const navigate = useNavigate();
  const [error, setError] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);
  const [imageURL, setImageURL] = useState<string | undefined>();

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<CategoryFormValues>();

  useEffect(() => {
    if (!isNew && id) {
      adminClient
        .get<CategoryDto>(`/Category/${id}`)
        .then((r) => {
          reset({ categoryName: r.data.categoryName });
          setImageURL(r.data.imageURL);
        })
        .catch(() => setError('Не удалось загрузить категорию'));
    }
  }, [id, isNew, reset]);

  const onSubmit = async (values: CategoryFormValues) => {
    setError(null);
    try {
      if (isNew) {
        const body: CategoryCreateDto = { categoryName: values.categoryName };
        await adminClient.post('/Category', body);
        setSuccessMessage('Категория успешно создана.');
      } else {
        await adminClient.put('/Category', {
          categoryId: Number(id),
          categoryName: values.categoryName,
          imageURL,
        } satisfies CategoryDto);
        setSuccessMessage('Категория успешно сохранена.');
      }
    } catch (e: unknown) {
      const msg =
        (e as { response?: { data?: { message?: string } } })?.response?.data?.message ||
        'Ошибка сохранения категории';
      setError(msg);
    }
  };

  return (
    <div>
      <h2>{isNew ? 'Новая категория' : `Категория #${id}`}</h2>
      <form className="admin-card admin-form admin-form--overlay-host" onSubmit={handleSubmit(onSubmit)}>
        {isSubmitting && <AdminLoadingOverlay message="Сохранение категории…" />}
        <label>Название</label>
        <input
          {...register('categoryName', {
            required: 'Обязательно',
            minLength: { value: 3, message: 'Минимум 3 символа' },
            maxLength: { value: 50, message: 'Максимум 50 символов' },
          })}
        />
        {errors.categoryName && <p className="admin-error">{errors.categoryName.message}</p>}

        {!isNew && imageURL && (
          <>
            <label>Текущее изображение</label>
            <input value={imageURL} disabled />
          </>
        )}

        {error && <p className="admin-error">{error}</p>}

        <button type="submit" className="admin-btn" disabled={isSubmitting}>
          Сохранить
        </button>
        <button
          type="button"
          className="admin-btn admin-btn--secondary"
          style={{ marginLeft: 8 }}
          disabled={isSubmitting}
          onClick={() => navigate(-1)}
        >
          Отмена
        </button>
      </form>

      {successMessage && (
        <AdminNoticeModal
          title="Сохранено"
          message={successMessage}
          confirmLabel="К списку категорий"
          onClose={() => {
            setSuccessMessage(null);
            navigate('/admin/categories');
          }}
        />
      )}
    </div>
  );
};

export default AdminCategoryForm;
