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
                    <img
                        src={`https://localhost:7275${product.imageUrl}`} 
                        alt={product.productName}
                        onError={(e) => {
                            e.target.src = '/placeholder-image.jpg';
                        }}
                        loading="lazy"
                    />
                    <h2>{product.productName}</h2>
                    <p>Цена: {product.productPrice} ₽</p>
                    <p>{product.productDescription}</p>
                </div>
            </div>
        </div>
    );
};

export default ProductPage;