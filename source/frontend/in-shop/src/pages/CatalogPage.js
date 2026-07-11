import React, { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import './CatalogPage.css';
import LoadingSpinner from '../components/LoadingSpinner';
import { apiClient } from '../api/client';
import { resolveAssetUrl } from '../config/api.js';

const CatalogPage = () => {
    const [categories, setCategories] = useState([]);
    const [loading, setLoading] = useState(true);

    useEffect(() => {
        // Загрузка категорий из API
        apiClient.get('/Category') 
            .then((response) => {
                setCategories(response.data);
                setLoading(false);
            })
            .catch((error) => {
                console.error('Ошибка загрузки категорий:', error);
                setLoading(false);
            });
    }, []);

    if (loading) {
        return <LoadingSpinner message="Загрузка каталога..." />;
    }

    return (
        <div className="catalog-page">
            {/* Заголовок "Каталог товаров" */}
            <h2 className="catalog-title">Каталог товаров</h2>

            {/* Контейнер для списка категорий */}
            <div className="categories-container">
                {categories.length === 0 ? (
                    <p>Нет доступных категорий.</p>
                ) : (
                    <ul className="categories-list">
                        {categories.map((category) => (
                            <li key={category.categoryId} className="category-card">
                                <Link to={`/category/${encodeURIComponent(category.categoryName)}`}>
                                    <img
                                        src={resolveAssetUrl(category.imageURL)} 
                                        alt={category.categoryName}
                                        onError={(e) => {
                                            e.target.src = '/placeholder-image.jpg';
                                        }}
                                        loading="lazy"
                                    />
                                    <p>{category.categoryName}</p>
                                </Link>
                            </li>
                        ))}
                    </ul>
                )}
            </div>
        </div>
    );
};

export default CatalogPage;