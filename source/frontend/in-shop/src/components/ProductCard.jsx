import { useNavigate } from 'react-router-dom';
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
        ? `https://localhost:7275${product.imageUrl.startsWith('/') ? '' : '/'}${product.imageUrl}`
        : null;

    // URL заглушки с бэкенда
    const placeholderSrc = 'https://localhost:7275/images/placeholder.svg';

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
