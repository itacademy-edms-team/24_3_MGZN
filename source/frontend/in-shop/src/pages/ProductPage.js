import React, { useEffect, useState } from 'react';
import { useParams } from 'react-router-dom';
import axios from 'axios';
import Breadcrumb from '../components/Breadcrumb'; // Импортируем Breadcrumb
import './ProductPage.css';

const ProductPage = () => {
    const { productId } = useParams();
    const [product, setProduct] = useState(null);
    const [loading, setLoading] = useState(true);

    useEffect(() => {
        // Загружаем данные о товаре
        axios.get(`https://localhost:7275/api/Products/${productId}`) 
            .then((response) => {
                const productData = response.data;
                console.log('Данные о товаре:', productData); // Логируем данные о товаре
                setProduct(productData);
                setLoading(false);
            })
            .catch((error) => {
                console.error('Ошибка загрузки товара:', error);
                setLoading(false);
            });
    }, [productId]);

    if (loading) {
        return (
            <div className="loader-container">
                <div className="loader"></div>
            </div>
        );
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
                    <img
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
                        <button className="add-to-cart-button">
                            {product.productStockQuantity > 0 ? 'Добавить в корзину' : 'Недоступно'}
                        </button>
                    </div>
                </div>
            </div>
        </div>
    );
};

export default ProductPage;