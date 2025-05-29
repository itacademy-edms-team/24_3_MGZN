import React, { useState, useEffect } from 'react';
import './App.css'; // Импортируем CSS для стилей

function App() {
    // Состояние для хранения категорий
    const [categories, setCategories] = useState([]);
    const [loading, setLoading] = useState(true); // Состояние загрузки
    const [error, setError] = useState(null); // Состояние ошибки

    // Состояние для хранения товаров по категориям
    const [productsByCategory, setProductsByCategory] = useState({}); // Объект для хранения товаров по категориям

    // Состояние для управления видимостью выпадающих списков
    const [expandedCategories, setExpandedCategories] = useState({});

    // Функция для загрузки категорий из API
    const fetchCategories = async () => {
        try {
            const response = await fetch('https://localhost:7275/api/Category');
            if (!response.ok) {
                throw new Error(`Ошибка ${response.status}: ${response.statusText}`);
            }
            const data = await response.json();
            setCategories(data);
        } catch (err) {
            setError(err.message || 'Неизвестная ошибка');
        } finally {
            setLoading(false);
        }
    };

    // Функция для загрузки товаров по категории
    const fetchProductsByCategory = async (categoryName) => {
        try {
            const response = await fetch(
                `https://localhost:7275/api/Products/products-by-category?categoryName=${encodeURIComponent(categoryName)}`
            );

            if (!response.ok) {
                throw new Error(`Ошибка ${response.status}: ${response.statusText}`);
            }

            const data = await response.json();

            // Сохраняем товары в объект productsByCategory
            setProductsByCategory((prev) => ({
                ...prev,
                [categoryName]: data,
            }));

            // Развертываем выпадающий список для этой категории
            setExpandedCategories((prev) => ({
                ...prev,
                [categoryName]: true,
            }));
        } catch (err) {
            setError(err.message || 'Неизвестная ошибка');
        }
    };

    // Загружаем категории при монтировании компонента
    useEffect(() => {
        fetchCategories();
    }, []);

    return (
        <div className="App">
            {/* Шапка страницы */}
            <header>
                <h1>InShop</h1>
                <img src="logo.png" alt="Логотип магазина" className='logo' />
            </header>

            {/* Основное содержимое */}
            <main>
                <h2>Категории товаров</h2>

                {/* Индикатор загрузки */}
                {loading && <p>Загрузка...</p>}

                {/* Сообщение об ошибке */}
                {error && <p style={{ color: 'red' }}>Ошибка: {error}</p>}

                {/* Список категорий */}
                {!loading && !error && categories.length > 0 && (
                    <ul className="categories">
                        {categories.map((category) => (
                            <li key={category.categoryId} className="category-item">
                                {/* Заголовок категории */}
                                <div
                                    className="category-title"
                                    onClick={() => {
                                        if (!productsByCategory[category.categoryName]) {
                                            fetchProductsByCategory(category.categoryName);
                                        } else {
                                            // Переключаем видимость выпадающего списка
                                            setExpandedCategories((prev) => ({
                                                ...prev,
                                                [category.categoryName]: !prev[category.categoryName],
                                            }));
                                        }
                                    }}
                                >
                                    {category.categoryName}
                                </div>

                                {/* Выпадающий список товаров */}
                                {expandedCategories[category.categoryName] && (
                                    <ul className="products-list">
                                        {productsByCategory[category.categoryName]?.map((product) => (
                                            <li key={product.productId} className="product-card">
                                                <img
                                                    src={`https://localhost:7275${product.imageUrl}`}
                                                    alt={product.productName}
                                                    className="product-image"
                                                    onError={(e) => {
                                                        e.target.src = 'placeholder-image.jpg'; // Плейсхолдер, если изображение не загружается
                                                    }}
                                                />
                                                <div className="product-name">{product.productName}</div>
                                                <p>Цена: {product.productPrice} ₽</p>
                                            </li>
                                        ))}
                                    </ul>
                                )}
                            </li>
                        ))}
                    </ul>
                )}

                {/* Если категорий нет */}
                {!loading && !error && categories.length === 0 && <p>Нет доступных категорий.</p>}
            </main>

            {/* Футер */}
            <footer>
                <p>&copy; 2025 InShop. Все права защищены.</p>
                <ul>
                    <li>
                        <a href="#">О нас</a>
                    </li>
                    <li>
                        <a href="#">Контакты</a>
                    </li>
                    <li>
                        <a href="#">Политика конфиденциальности</a>
                    </li>
                </ul>
            </footer>
        </div>
    );
}

export default App;