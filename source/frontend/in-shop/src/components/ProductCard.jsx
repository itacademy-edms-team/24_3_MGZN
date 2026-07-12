import { useContext, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { resolveAssetUrl, PRODUCT_PLACEHOLDER_URL } from '../config/api.js';
import StarRating from './StarRating/StarRating';
import { CartContext } from './CartContext';
import './ProductCard.css';

const ProductCard = ({ product }) => {
    const navigate = useNavigate();
    const { addToCart } = useContext(CartContext);
    const [adding, setAdding] = useState(false);
    const [added, setAdded] = useState(false);

    const hasValidImageUrl = product.imageUrl && product.imageUrl.trim() !== '';
    const imageSrc = hasValidImageUrl ? resolveAssetUrl(product.imageUrl) : null;
    const placeholderSrc = PRODUCT_PLACEHOLDER_URL;

    const rating = Number(product.averageRating ?? 0);
    const reviewsCount = Number(product.reviewsCount ?? 0);
    const stockQuantity = Number(
        product.productStockQuantity ?? product.stockQuantity ?? 0
    );
    const inStock =
        product.productAvailability === true ||
        product.isAvailable === true ||
        (product.productAvailability !== false &&
            product.isAvailable !== false &&
            stockQuantity > 0);

    const handleClick = () => {
        navigate(`/product/${encodeURIComponent(product.productId)}`);
    };

    const handleAddToCart = async (event) => {
        event.preventDefault();
        event.stopPropagation();
        if (adding || !inStock) return;

        try {
            setAdding(true);
            await addToCart(product);
            setAdded(true);
            window.setTimeout(() => setAdded(false), 1600);
        } catch {
            // Ошибка уже пишется в CartContext
        } finally {
            setAdding(false);
        }
    };

    return (
        <div className="product-card" onClick={handleClick}>
            <div className="product-image-container">
                {hasValidImageUrl ? (
                    <img
                        src={imageSrc}
                        alt={product.productName}
                        className="product-image"
                        loading="lazy"
                        onError={(e) => {
                            e.currentTarget.src = placeholderSrc;
                        }}
                    />
                ) : (
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

                <div className="product-meta">
                    <div className="product-card-rating" title="Рейтинг и число отзывов">
                        <StarRating rating={rating} readOnly size="small" />
                        <span className="product-card-rating__value">
                            {rating > 0 ? rating.toFixed(1) : '—'}
                        </span>
                        <span className="product-card-rating__count">
                            ({reviewsCount})
                        </span>
                    </div>

                    <span
                        className={`product-meta__row ${
                            inStock ? '' : 'product-meta__row--muted'
                        }`}
                        title={inStock ? `Остаток на складе: ${stockQuantity}` : 'Нет в наличии'}
                    >
                        <span className="product-meta__key">
                            {inStock ? 'В наличии' : 'Наличие'}
                        </span>
                        <span className="product-meta__val">
                            {inStock ? `${stockQuantity} шт.` : 'Нет'}
                        </span>
                    </span>
                </div>

                <p className="price">{product.productPrice?.toLocaleString('ru-RU')} ₽</p>
            </div>

            {inStock && (
                <button
                    type="button"
                    className={`product-card-add${added ? ' product-card-add--done' : ''}`}
                    onClick={handleAddToCart}
                    disabled={adding}
                    aria-label="Добавить в корзину"
                >
                    {adding ? 'Добавляем…' : added ? 'В корзине' : 'Добавить в корзину'}
                </button>
            )}
        </div>
    );
};

export default ProductCard;
