import React, { useEffect, useState } from 'react';
import { useForm } from 'react-hook-form';
import { useNavigate, useParams } from 'react-router-dom';
import axios from 'axios';
import adminClient from '../api/adminClient.ts';
import AdminImagePreview from '../components/AdminImagePreview.tsx';
import AdminNoticeModal from '../components/AdminNoticeModal.tsx';
import { AdminProduct, CategoryDto } from '../types/adminTypes.ts';
import { resolveProductImageUrl } from '../utils/adminUtils.ts';
import '../layout/AdminLayout.css';

interface ProductFormValues {
  productName: string;
  productDescription: string;
  productPrice: number;
  productAvailability: boolean;
  productCategoryId: number;
  productStockQuantity: number;
}

const API_PUBLIC = 'https://localhost:7275/api';

const AdminProductForm: React.FC = () => {
  const { id } = useParams();
  const isNew = !id || id === 'new';
  const navigate = useNavigate();
  const [categories, setCategories] = useState<CategoryDto[]>([]);
  const [imageBase64, setImageBase64] = useState<string | null>(null);
  /** Текущий URL изображения при редактировании — для предпросмотра до загрузки нового файла. */
  const [existingImageUrl, setExistingImageUrl] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  /** Сообщение об успешном сохранении — показывается в модальном окне перед переходом к списку. */
  const [successMessage, setSuccessMessage] = useState<string | null>(null);

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<ProductFormValues>({
    defaultValues: {
      productAvailability: true,
      productStockQuantity: 0,
      productPrice: 0,
      productCategoryId: 0,
    },
  });

  useEffect(() => {
    axios.get<CategoryDto[]>(`${API_PUBLIC}/Category`).then((r) => setCategories(r.data));
  }, []);

  useEffect(() => {
    if (!isNew && id) {
      adminClient.get<AdminProduct>(`/Admin/products/${id}`).then((r) => {
        const p = r.data;
        reset({
          productName: p.productName,
          productDescription: p.productDescription || '',
          productPrice: p.productPrice,
          productAvailability: p.productAvailability,
          productCategoryId: p.productCategoryId,
          productStockQuantity: p.productStockQuantity,
        });
        setExistingImageUrl(resolveProductImageUrl(p.imageUrl));
      });
    }
  }, [id, isNew, reset]);

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
      setError(null);
    };
    reader.readAsDataURL(file);
  };

  const onSubmit = async (values: ProductFormValues) => {
    setError(null);
    const body = {
      ...values,
      imageBase64: imageBase64 || undefined,
    };
    try {
      if (isNew) {
        await adminClient.post('/Admin/products', body);
        setSuccessMessage('Товар успешно создан.');
      } else {
        await adminClient.put(`/Admin/products/${id}`, body);
        setSuccessMessage('Данные товара успешно сохранены.');
      }
    } catch (e: unknown) {
      const msg =
        (e as { response?: { data?: { message?: string } } })?.response?.data?.message ||
        'Ошибка сохранения';
      setError(msg);
    }
  };

  return (
    <div>
      <h2>{isNew ? 'Новый товар' : `Товар #${id}`}</h2>
      <form className="admin-card admin-form" onSubmit={handleSubmit(onSubmit)}>
        <label>Название</label>
        <input {...register('productName', { required: 'Обязательно', minLength: 2 })} />
        {errors.productName && <p className="admin-error">{errors.productName.message}</p>}

        <label>Описание</label>
        <textarea rows={4} {...register('productDescription')} />

        <label>Цена (₽)</label>
        {/* step=1 — удобнее менять цену целыми рублями, а не копейками */}
        <input type="number" step="1" min="0" {...register('productPrice', { required: true, min: 0, valueAsNumber: true })} />

        <label>Категория</label>
        <select {...register('productCategoryId', { required: true, valueAsNumber: true })}>
          <option value={0}>— выберите —</option>
          {categories.map((c) => (
            <option key={c.categoryId} value={c.categoryId}>
              {c.categoryName}
            </option>
          ))}
        </select>

        <label>Остаток на складе (свободный)</label>
        <input type="number" {...register('productStockQuantity', { min: 0, valueAsNumber: true })} />

        {/* Чекбокс выровнен по левому краю и увеличен через CSS (.admin-form-checkbox) */}
        <label className="admin-form-checkbox">
          <input type="checkbox" {...register('productAvailability')} />
          <span>В наличии (витрина)</span>
        </label>

        <label>Изображение (JPEG/PNG/WebP, до 5 МБ)</label>
        {/* Предпросмотр существующего фото при редактировании */}
        {existingImageUrl && !imageBase64 && (
          <AdminImagePreview src={existingImageUrl} alt="Текущее фото товара" label="Текущее изображение" />
        )}
        {imageBase64 && (
          <AdminImagePreview src={imageBase64} alt="Новое фото" label="Новое изображение (предпросмотр)" />
        )}
        <input type="file" accept="image/jpeg,image/png,image/webp" onChange={onFileChange} />

        {error && <p className="admin-error">{error}</p>}

        <button type="submit" className="admin-btn" disabled={isSubmitting}>
          Сохранить
        </button>
        <button type="button" className="admin-btn admin-btn--secondary" style={{ marginLeft: 8 }} onClick={() => navigate(-1)}>
          Отмена
        </button>
      </form>

      {successMessage && (
        <AdminNoticeModal
          title="Сохранено"
          message={successMessage}
          confirmLabel="К списку товаров"
          onClose={() => {
            setSuccessMessage(null);
            navigate('/admin/products');
          }}
        />
      )}
    </div>
  );
};

export default AdminProductForm;
