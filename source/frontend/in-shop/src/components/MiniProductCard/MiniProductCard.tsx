import React from 'react';
import './MiniProductCard.css'; // Создайте соответствующий CSS файл

// Интерфейс для данных товара (может совпадать с ProductSearchResultDto)
interface ProductSearchResultDto {
  id: number;
  name: string;
  price: number;
  category: string;
  description: string;
  stockQuantity: number;
  isAvailable: boolean;
  imageUrl: string;
}

interface MiniProductCardProps {
  product: ProductSearchResultDto;
  onClick: () => void; // Обработчик клика на карточку
}

const MiniProductCard: React.FC<MiniProductCardProps> = ({ product, onClick }) => {
  // Проверяем, есть ли изображение и не пустой ли путь
  const hasValidImageUrl = product.imageUrl && product.imageUrl.trim() !== '';

  // Формируем правильный src
  const imageSrc = hasValidImageUrl
    ? `https://localhost:7275${product.imageUrl.startsWith('/') ? '' : '/'}${product.imageUrl}`
    : null;

  // URL заглушки с бэкенда
  const placeholderSrc = 'https://localhost:7275/images/placeholder.svg';

  return (
    <div className="mini-product-card" onClick={onClick}>
      <div className="mini-product-image-container">
        {hasValidImageUrl ? (
          <img
            src={imageSrc}
            alt={product.name}
            className="mini-product-image"
            loading="lazy"
          />
        ) : (
          <div className="mini-product-placeholder">
            <img
              src={placeholderSrc}
              alt="Нет изображения"
              className="mini-product-placeholder-img"
            />
          </div>
        )}
      </div>

      <div className="mini-product-info">
        <h4 className="mini-product-name">{product.name || 'Товар'}</h4>
        <p className="mini-product-price">{product.price?.toLocaleString('ru-RU')} ₽</p>
        <p className="mini-product-category">{product.category}</p>
      </div>
    </div>
  );
};

export default MiniProductCard;