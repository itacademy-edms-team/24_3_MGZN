import React, { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import axios from 'axios';
import './CatalogPage.css';

const CatalogPage = () => {
    const [categories, setCategories] = useState([]);
    const [loading, setLoading] = useState(true);

    useEffect(() => {
        // Загрузка категорий из API
        axios.get('https://localhost:7275/api/Category') 
            .then((response) => {
                console.log('Ответ от сервера:', response.data); // Логируем данные
                setCategories(response.data);
                setLoading(false);
            })
            .catch((error) => {
                console.error('Ошибка загрузки категорий:', error);
                setLoading(false);
            });
    }, []);

    if (loading) {
        return (
            <div className="loader-container">
                <div className="loader"></div>
            </div>
        );
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
                                        src={`https://localhost:7275${category.imageURL}`} 
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