import { useNavigate } from 'react-router-dom';
import { resolveAssetUrl, PRODUCT_PLACEHOLDER_URL } from '../config/api.js';
import './ProductCard.css'; // Импортируем стили компонента

const ProductCard = ({ product }) => {
    const navigate = useNavigate(); // useNavigate теперь внутри компонента

    const handleClick = () => {
        navigate(`/product/${encodeURIComponent(product.productId)}`);
    };

    // Проверяем, есть ли изображение и не пустой ли путь
    const hasValidImageUrl = product.imageUrl && product.imageUrl.trim() !== '';

    // Формируем правильный src
    const imageSrc = hasValidImageUrl
        ? resolveAssetUrl(product.imageUrl)
        : null;

    // URL заглушки с бэкенда
    const placeholderSrc = PRODUCT_PLACEHOLDER_URL;

    return (
        <div className="product-card" onClick={handleClick}>
            <div className="product-image-container">
                {hasValidImageUrl ? (
                    // Показываем реальное изображение
                    <img
                        src={imageSrc}
                        alt={product.productName}
                        className="product-image"
                        loading="lazy"
                    />
                ) : (
                    // Показываем заглушку с бэкенда
                    <div className="product-placeholder">
                        <img
                            src={placeholderSrc}
                            alt="Нет изображения"
                            className="product-placeholder-img"
                        />
                    </div>
                )}
            </div>

            <div className="product-info">
                <h3>{product.productName || 'Товар'}</h3>
                <p className="price">{product.productPrice?.toLocaleString('ru-RU')} ₽</p>
            </div>
        </div>
    );
};

export default ProductCard;
