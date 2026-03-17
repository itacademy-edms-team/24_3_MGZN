import React, { useEffect, useState, useContext } from 'react';
import { useParams } from 'react-router-dom';
import axios from 'axios';
import Breadcrumb from '../components/Breadcrumb';
import { CartContext } from '../components/CartContext';
import './ProductPage.css';
import ProductCard from '../components/ProductCard';
import LoadingSpinner from '../components/LoadingSpinner';

// Интерфейс для характеристики (если файл .tsx)
interface ProductSpecificationDto {
    specId: number;
    name: string;
    displayName: string;
    dataType: string;
    textValue: string | null;
    numberValue: number | null;
}

const ProductPage = () => {
    const { productId } = useParams();
    
    // Состояния
    const [product, setProduct] = useState<ProductSpecificationDto | null>(null); // Исправлен тип на любой или конкретный DTO товара
    const [relatedProducts, setRelatedProducts] = useState<any[]>([]);
    const [specifications, setSpecifications] = useState<ProductSpecificationDto[]>([]);
    const [loading, setLoading] = useState(true);
    const [specsLoading, setSpecsLoading] = useState(false);

    const { addToCart } = useContext(CartContext);

    // 1. Эффект для загрузки основного товара и характеристик
    useEffect(() => {
        if (!productId) return;

        setLoading(true);
        setSpecsLoading(true);

        const fetchProductData = async () => {
            try {
                // Загружаем товар
                const productRes = await axios.get(`https://localhost:7275/api/Products/${productId}`);
                const productData = productRes.data;
                
                console.log('Данные о товаре:', productData);
                setProduct(productData);

                // Сразу же загружаем характеристики
                const specsRes = await axios.get(`https://localhost:7275/api/Products/${productId}/specifications`);
                setSpecifications(specsRes.data || []);
            } catch (error) {
                console.error('Ошибка загрузки данных:', error);
            } finally {
                setSpecsLoading(false);
                // Не сбрасываем loading здесь, так как мы ждем еще и связанных товаров
            }
        };

        fetchProductData();
    }, [productId]); // Зависит только от ID

    // 2. Отдельный эффект для загрузки связанных товаров (когда product уже загружен)
    useEffect(() => {
        if (!product?.productCategoryName || !productId) return;

        const fetchRelatedProducts = async () => {
            try {
                const response = await axios.get(
                    `https://localhost:7275/api/Products/products-by-category?categoryName=${encodeURIComponent(product.productCategoryName)}`
                );
                
                const relatedProductsData = response.data.filter(
                    (p: any) => p.productId !== parseInt(productId)
                );
                setRelatedProducts(relatedProductsData);
            } catch (error) {
                console.error('Ошибка загрузки связанных товаров:', error);
            } finally {
                setLoading(false); // Теперь можно безопасно остановить спиннер загрузки всей страницы
            }
        };

        fetchRelatedProducts();
    }, [product, productId]); // Зависит от product (чтобы сработало после его загрузки) и productId

    if (loading) {
        return <LoadingSpinner message="Загрузка товаров..." />;
    }

    if (!product) {
        return <p>Товар не найден.</p>;
    }

    return (
        <div>
            <Breadcrumb
                currentPage={product.productName}
                categoryName={product.productCategoryName}
            />
            <div className="product-page">
                <div className="product-details">
                    <img 
                        className="product-page__img"
                        src={`https://localhost:7275${product.imageUrl}`}
                        alt={product.productName}
                        onError={(e) => {
                            e.target.src = 'https://localhost:7275/images/placeholder.svg';
                        }}
                        loading="lazy"
                    />

                    <div className="product-info">
                        <h2>{product.productName}</h2>

                        {product.productStockQuantity > 0 ? (
                            <p>На складе: {product.productStockQuantity} шт.</p>
                        ) : (
                            <p className="out-of-stock">Нет в наличии</p>
                        )}

                        <p className="product-price">Цена: {product.productPrice} ₽</p>
                        <p className="product-description">{product.productDescription}</p>

                        {product.productStockQuantity > 0 ? (
                            <button
                                className="add-to-cart-button"
                                onClick={() => addToCart(product)}
                            >
                                Добавить в корзину
                            </button>
                        ) : null}
                    </div>
                </div>

                {/* Блок характеристик */}
                {specifications.length > 0 && (
                    <div className="product-specifications-section">
                        <h3 className="specs-title">Характеристики</h3>
                        
                        {specsLoading ? (
                            <div className="specs-loading"><LoadingSpinner message="Загрузка характеристик..." /></div>
                        ) : (
                            <div className="specs-grid">
                                {specifications.map((spec) => {
                                    const value = spec.textValue ?? spec.numberValue;
                                    
                                    const displayValue = typeof value === 'number' 
                                        ? (Number.isInteger(value) ? value : parseFloat(value.toFixed(2))) 
                                        : value;

                                    return (
                                        <div key={spec.specId} className="spec-item">
                                            <span className="spec-name">{spec.displayName}:</span>
                                            <span className="spec-value">{displayValue}</span>
                                        </div>
                                    );
                                })}
                            </div>
                        )}
                    </div>
                )}

                {/* Другие товары этой категории */}
                <div className="related-products">
                    <h3>Товары категории {product.productCategoryName}</h3>
                    <ul className="products-list">
                        {relatedProducts.map((relatedProduct) => (
                            <li key={relatedProduct.productId} className="product-card-wrapper">
                                <ProductCard product={relatedProduct} />
                            </li>
                        ))}
                    </ul>
                </div>
            </div>
        </div>
    );
};

export default ProductPage;