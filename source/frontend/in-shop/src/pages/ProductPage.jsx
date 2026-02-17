import React, { useEffect, useState, useContext } from 'react';
import { useParams} from 'react-router-dom';
import axios from 'axios';
import Breadcrumb from '../components/Breadcrumb';
import { CartContext } from '../components/CartContext';
import './ProductPage.css';
import ProductCard from '../components/ProductCard';
import LoadingSpinner from '../components/LoadingSpinner';

const ProductPage = () => {
    const { productId } = useParams();
    const [product, setProduct] = useState(null);
    const [relatedProducts, setRelatedProducts] = useState([]);
    const [loading, setLoading] = useState(true);

    // Получаем функцию добавления товара из контекста корзины
    const { addToCart } = useContext(CartContext);

    useEffect(() => {
        // Загружаем данные о товаре
        axios.get(`https://localhost:7275/api/Products/${productId}`)
            .then((response) => {
                const productData = response.data;
                console.log('Данные о товаре:', productData); // Логируем данные о товаре
                setProduct(productData);
                setLoading(false);

                // Загружаем другие товары из той же категории
                if (productData.productCategoryName) {
                    axios.get(`https://localhost:7275/api/Products/products-by-category?categoryName=${encodeURIComponent(productData.productCategoryName)}`)
                        .then((response) => {
                            const relatedProductsData = response.data.filter(
                                (p) => p.productId !== productId // Исключаем текущий товар
                            );
                            setRelatedProducts(relatedProductsData);
                        })
                        .catch((error) => {
                            console.error('Ошибка загрузки связанных товаров:', error);
                        });
                }
            })
            .catch((error) => {
                console.error('Ошибка загрузки товара:', error);
                setLoading(false);
            });
    }, [productId]);

    if (loading) {
        return <LoadingSpinner message="Загрузка товаров..." />;
    }

    if (!product) {
        return <p>Товар не найден.</p>;
    }

    return (
        <div>
            {/* Передаем categoryName и productName в Breadcrumb */}
            <Breadcrumb
                currentPage={product.productName} // Название товара как текущая страница
                categoryName={product.productCategoryName} // Название категории
            />
            <div className="product-page">
                <div className="product-details">
                    {/* Изображение товара */}
                    <img class="product-page__img"
                        src={`https://localhost:7275${product.imageUrl}`}
                        alt={product.productName}
                        onError={(e) => {
                            e.target.src = 'https://localhost:7275/images/placeholder.svg';
                        }}
                        loading="lazy"
                    />

                    {/* Информация о товаре */}
                    <div className="product-info">
                        <h2>{product.productName}</h2>

                        {/* Условное отображение наличия товара */}
                        {product.productStockQuantity > 0 ? (
                            <p>На складе: {product.productStockQuantity} шт.</p>
                        ) : (
                            <p className="out-of-stock">Нет в наличии</p>
                        )}

                        <p className="product-price">Цена: {product.productPrice} ₽</p>
                        <p className="product-description">{product.productDescription}</p>

                        {/* Кнопка "Добавить в корзину" */}
                        {product.productStockQuantity > 0 ? (
                            <button
                                className="add-to-cart-button"
                               onClick={() => addToCart(product)} // Добавляем товар в корзину
                            >
                                Добавить в корзину
                            </button>
                        ) : null} {/* Если товара нет, кнопка не отображается */}
                    </div>
                </div>

                {/* Другие товары этой категории */}
                <div className="related-products">
                    <h3>Товары категории {product.productCategoryName}</h3>
                    <ul className="products-list"> {/* Используем класс из CategoryPage.css */}
                        {relatedProducts.map((relatedProduct) => (
                            <li key={relatedProduct.productId} className="product-card-wrapper"> {/* Используем класс из CategoryPage.css */}
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