import React, { useEffect, useState } from 'react';
import { useParams, Link } from 'react-router-dom';
import axios from 'axios';
import Breadcrumb from '../components/Breadcrumb'; // Импортируем Breadcrumb
import './CategoryPage.css';

const CategoryPage = () => {
    const { categoryName } = useParams(); // Получаем параметр categoryName
    const [products, setProducts] = useState([]);
    const [loading, setLoading] = useState(true);

    console.log('Параметр categoryName:', categoryName); // Логируем categoryName

    useEffect(() => {
        axios.get(`https://localhost:7275/api/Products/products-by-category?categoryName=${encodeURIComponent(categoryName)}`)
            .then((response) => {
                setProducts(response.data);
                setLoading(false);
            })
            .catch((error) => {
                console.error('Ошибка загрузки товаров:', error);
                setLoading(false);
            });
    }, [categoryName]);

    if (loading) {
        return (
            <div className="loader-container">
                <div className="loader"></div>
            </div>
        );
    }

    return (
        <div className="category-page">
            <h2>{decodeURIComponent(categoryName || '')}</h2> {/* Отображаем название категории */}
            <Breadcrumb categoryName={categoryName} /> {/* Передаем categoryName */}
            <ul className="products-list">
                {products.map((product) => (
                    <li key={product.productId} className="product-card">
                        <Link to={`/product/${encodeURIComponent(product.productId)}`}> {/* Добавляем Link */}
                            <img
                                src={`https://localhost:7275${product.imageUrl}`} 
                                alt={product.productName}
                                onError={(e) => {
                                    e.target.src = 'https://localhost:7275/images/placeholder.png';
                                }}
                                loading="lazy"
                            />
                            <h3>{product.productName}</h3>
                            <p>{product.productPrice} ₽</p>
                        </Link>
                    </li>
                ))}
            </ul>
        </div>
    );
};

export default CategoryPage;