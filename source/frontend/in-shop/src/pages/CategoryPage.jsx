import React, { useEffect, useState } from 'react';
import { useParams, Link } from 'react-router-dom';
import axios from 'axios';
import Breadcrumb from '../components/Breadcrumb';
import './CategoryPage.css';

const CategoryPage = () => {
    const { categoryName } = useParams();
    const [products, setProducts] = useState([]);
    const [loading, setLoading] = useState(true);

    const [sortOption, setSortOption] = useState('name-asc');

    // --- ОБНОВЛЕНО: Состояние для открытия меню ---
    const [isMenuOpen, setIsMenuOpen] = useState(false);
    // --- /ОБНОВЛЕНО ---

    useEffect(() => {
        let sortBy = 'ProductName';
        let sortOrder = 'asc';

        switch (sortOption) {
            case 'name-asc':
                sortBy = 'ProductName';
                sortOrder = 'asc';
                break;
            case 'name-desc':
                sortBy = 'ProductName';
                sortOrder = 'desc';
                break;
            case 'price-asc':
                sortBy = 'Price';
                sortOrder = 'asc';
                break;
            case 'price-desc':
                sortBy = 'Price';
                sortOrder = 'desc';
                break;
            default:
                sortBy = 'ProductName';
                sortOrder = 'asc';
        }

        const params = new URLSearchParams({
            categoryName,
            sortBy,
            sortOrder
        });

        axios.get(`https://localhost:7275/api/Products/products-by-category?${params}`)
            .then((response) => {
                setProducts(response.data);
                setLoading(false);
            })
            .catch((error) => {
                console.error('Ошибка загрузки товаров:', error);
                setLoading(false);
            });
    }, [categoryName, sortOption]);

    const handleSortOptionChange = (event) => {
        setSortOption(event.target.value);
    };

    // --- ОБНОВЛЕНО: Обработчик клика ---
    const toggleMenu = () => {
        setIsMenuOpen(!isMenuOpen);
    };
    // --- /ОБНОВЛЕНО ---

    if (loading) {
        return (
            <div className="loader-container">
                <div className="loader"></div>
            </div>
        );
    }

    return (
        <div className="category-page">
            {/* Хлебные крошки */}
            <Breadcrumb categoryName={categoryName} />

            {/* Заголовок категории */}
            <h2>{decodeURIComponent(categoryName || '')}</h2>

            <div className="div__sort-menu">
                <div className={`sort-menu ${isMenuOpen ? 'open' : ''}`}>
                    <div
                        className="sort-menu-header"
                        onClick={toggleMenu} // Только onClick
                    >
                        Сортировка
                        <span className={`sort-arrow ${isMenuOpen ? 'up' : 'down'}`}></span>
                    </div>
                    <div className="sort-menu-dropdown">
                        <label className="sort-option">
                            <input
                                type="radio"
                                name="sort"
                                value="name-asc"
                                checked={sortOption === 'name-asc'}
                                onChange={handleSortOptionChange}
                            />
                            <span className="sort-label">Название товара ↓</span>
                        </label>
                        <label className="sort-option">
                            <input
                                type="radio"
                                name="sort"
                                value="name-desc"
                                checked={sortOption === 'name-desc'}
                                onChange={handleSortOptionChange}
                            />
                            <span className="sort-label">Название товара ↑</span>
                        </label>
                        <label className="sort-option">
                            <input
                                type="radio"
                                name="sort"
                                value="price-asc"
                                checked={sortOption === 'price-asc'}
                                onChange={handleSortOptionChange}
                            />
                            <span className="sort-label">Цена ↑</span>
                        </label>
                        <label className="sort-option">
                            <input
                                type="radio"
                                name="sort"
                                value="price-desc"
                                checked={sortOption === 'price-desc'}
                                onChange={handleSortOptionChange}
                            />
                            <span className="sort-label">Цена ↓</span>
                        </label>
                    </div>
                </div>
            </div>
            

            {/* Список товаров */}
            <ul className="products-list">
                {products.map((product) => (
                    <li key={product.productId} className="product-card">
                        <Link to={`/product/${encodeURIComponent(product.productId)}`} className="product-link">
                            <div className="product-content">
                                <img
                                    src={`https://localhost:7275${product.imageUrl}`}
                                    alt={product.productName}
                                    onError={(e) => {
                                        e.target.src = 'https://localhost:7275/images/placeholder.svg';
                                    }}
                                    loading="lazy"
                                />
                            </div>
                            <div className="product-info">
                                <h3>{product.productName}</h3>
                                <p>{product.productPrice} ₽</p>
                            </div>
                        </Link>
                    </li>
                ))}
            </ul>
        </div>
    );
};

export default CategoryPage;