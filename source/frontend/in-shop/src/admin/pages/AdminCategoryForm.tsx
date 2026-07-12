import React, { useEffect, useState } from 'react';
import { useForm } from 'react-hook-form';
import { useNavigate, useParams } from 'react-router-dom';
import adminClient from '../api/adminClient';
import AdminImagePreview from '../components/AdminImagePreview';
import AdminLoadingOverlay from '../components/AdminLoadingOverlay';
import AdminNoticeModal from '../components/AdminNoticeModal';
import { CategoryCreateDto, CategoryDto } from '../types/adminTypes';
import { resolveCategoryImageUrl } from '../utils/adminUtils';
import '../layout/AdminLayout.css';

interface CategoryFormValues {
  categoryName: string;
}

const AdminCategoryForm: React.FC = () => {
  const { id } = useParams();
  const isNew = !id || id === 'new';
  const navigate = useNavigate();
  const [error, setError] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);
  const [imageBase64, setImageBase64] = useState<string | null>(null);
  /** Текущий URL изображения при редактировании — для предпросмотра до загрузки нового файла. */
  const [existingImageUrl, setExistingImageUrl] = useState<string | null>(null);
  /** Пользователь открепил изображение — при сохранении уйдёт removeImage на API. */
  const [removeImage, setRemoveImage] = useState(false);
  const fileInputRef = React.useRef<HTMLInputElement>(null);

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
          setExistingImageUrl(resolveCategoryImageUrl(r.data.imageURL));
          setRemoveImage(false);
          setImageBase64(null);
        })
        .catch(() => setError('Не удалось загрузить категорию'));
    }
  }, [id, isNew, reset]);

  const handleDetachImage = () => {
    setRemoveImage(true);
    setExistingImageUrl(null);
    setImageBase64(null);
    setError(null);
    if (fileInputRef.current) {
      fileInputRef.current.value = '';
    }
  };

  const onFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;
    if (file.size > 5 * 1024 * 1024) {
      setError('Файл больше 5 МБ');
      return;
    }
    const reader = new FileReader();
    reader.onload = () => {
      setImageBase64(reader.result as string);
      setRemoveImage(false);
      setError(null);
    };
    reader.readAsDataURL(file);
  };

  const onSubmit = async (values: CategoryFormValues) => {
    setError(null);
    try {
      if (isNew) {
        const body: CategoryCreateDto = {
          categoryName: values.categoryName,
          imageBase64: imageBase64 || undefined,
        };
        await adminClient.post('/Category', body);
        setSuccessMessage('Категория успешно создана.');
      } else {
        await adminClient.put('/Category', {
          categoryId: Number(id),
          categoryName: values.categoryName,
          imageBase64: imageBase64 || undefined,
          removeImage,
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

        <label>Изображение (JPEG/PNG/WebP, до 5 МБ)</label>
        {existingImageUrl && !imageBase64 && (
          <AdminImagePreview src={existingImageUrl} alt="Текущее фото категории" label="Текущее изображение" />
        )}
        {imageBase64 && (
          <AdminImagePreview src={imageBase64} alt="Новое фото" label="Новое изображение (предпросмотр)" />
        )}
        {(existingImageUrl || imageBase64) && (
          <button
            type="button"
            className="admin-btn admin-btn--secondary admin-btn--detach-image"
            onClick={handleDetachImage}
            disabled={isSubmitting}
          >
            Открепить изображение
          </button>
        )}
        <input
          ref={fileInputRef}
          type="file"
          accept="image/jpeg,image/png,image/webp"
          onChange={onFileChange}
        />

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
