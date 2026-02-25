import React, { useState, useEffect } from 'react';
import { useParams, useSearchParams, useNavigate } from 'react-router-dom';
import Breadcrumb from '../components/Breadcrumb'; // Убедитесь в правильном пути
import ProductCard from '../components/ProductCard'; // Убедитесь в правильном пути
import FiltersPanel from '../components/FiltersPanel/FiltersPanel.tsx'; // Убедитесь в правильном пути
import LoadingSpinner from '../components/LoadingSpinner'; // Убедитесь в правильном пути
import './CategoryPage.css'; // Убедитесь в правильном пути

const CategoryPage = () => {
    const { categoryName: initialCategoryNameFromUrl } = useParams(); // Получаем имя категории из URL
    const [searchParams, setSearchParams] = useSearchParams(); // Для чтения/записи параметров
    const navigate = useNavigate(); // Для обновления URL при применении фильтров

    // --- Состояния ---
    const [products, setProducts] = useState([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);

    // --- Состояния для фильтров (синхронизированы с URL) ---
    const [currentFilters, setCurrentFilters] = useState({
        minPrice: searchParams.get('minPrice') || '',
        maxPrice: searchParams.get('maxPrice') || '',
        category: initialCategoryNameFromUrl || '', // Категория фиксирована для этой страницы, но FiltersPanel может её обновлять, если hideCategory=false
    });

    // --- Состояния для сортировки ---
    const [sortOption, setSortOption] = useState(() => {
        // Извлекаем сортировку из URL при монтировании
        const urlSort = searchParams.get('sort');
        // Проверяем, является ли значение допустимым
        if (['name-asc', 'name-desc', 'price-asc', 'price-desc'].includes(urlSort)) {
            return urlSort;
        }
        return 'name-asc'; // Значение по умолчанию
    });
    const [isMenuOpen, setIsMenuOpen] = useState(false);

    // --- Обработчик изменения фильтров (из FiltersPanel) ---
    const handleFiltersChange = (filters) => {
        setCurrentFilters(prev => ({
            ...prev,
            minPrice: filters.minPrice,
            maxPrice: filters.maxPrice,
            // Категория может измениться, если FiltersPanel позволяет (hideCategory=false)
            // Если hideCategory=true, filters.category не изменится, и мы используем initialCategoryNameFromUrl
            category: filters.category,
        }));
    };

    // --- Обработчик кнопки "Применить фильтры" ---
    const handleApplyFilters = () => {
        // Формируем новые параметры
        const newParams = new URLSearchParams(searchParams); // Берём текущие параметры

        // Обновляем параметры фильтра
        if (currentFilters.minPrice) {
            newParams.set('minPrice', currentFilters.minPrice);
        } else {
            newParams.delete('minPrice'); // Удаляем, если пусто
        }
        if (currentFilters.maxPrice) {
            newParams.set('maxPrice', currentFilters.maxPrice);
        } else {
            newParams.delete('maxPrice'); // Удаляем, если пусто
        }
        // Категория на этой странице фиксирована в пути, но если FiltersPanel позволяет менять её (hideCategory=false),
        // и она изменилась, можно обновить путь. Но обычно категория не меняется на странице категории.
        // Мы не меняем 'categoryName' в URL-пути, но можем передать её в API, если нужно.
        // В данном случае, categoryName будет передаваться как path parameter, а фильтры - как query params.

        // Сортировка (остаётся без изменений, если не меняли)
        // Если вы хотите, чтобы сортировка тоже обновлялась при нажатии "Применить", можно её тоже обновить:
        // newParams.set('sort', sortOption); // <<<--- РАСКОММЕНТИРУЙТЕ, ЕСЛИ ХОТИТЕ СОРТИРОВКУ ТОЖЕ ЧЕРЕЗ КНОПКУ
        // Или оставляем как есть, и сортировка обновляется через onChange селекта.

        // Навигация с новыми параметрами
        navigate(`?${newParams.toString()}`, { replace: true }); // replace: true, чтобы не создавать новую запись в истории
    };

    // --- Эффект: Загрузка товаров при изменении URL (categoryName, filters, sort) ---
    useEffect(() => {
        // Извлекаем параметры из URL *внутри* эффекта
        const minPriceParam = searchParams.get('minPrice');
        const maxPriceParam = searchParams.get('maxPrice');
        const sortParam = searchParams.get('sort');
        // Извлекаем сортировку из URL
        let effectiveSortOption = sortParam;
        if (!['name-asc', 'name-desc', 'price-asc', 'price-desc'].includes(effectiveSortOption)) {
             effectiveSortOption = 'name-asc'; // Значение по умолчанию, если в URL ошибка
        }
        // Обновляем состояние сортировки, если она изменилась в URL
        if (effectiveSortOption !== sortOption) {
             setSortOption(effectiveSortOption);
        }

        const loadProducts = async () => {
            if (!initialCategoryNameFromUrl) {
                setError('Категория не указана.');
                setLoading(false);
                return;
            }

            try {
                setLoading(true);
                setError(null);

                // Формируем параметры для API-запроса
                const apiParams = new URLSearchParams({
                    categoryName: initialCategoryNameFromUrl, // Имя категории из пути URL
                });

                // Добавляем фильтры по цене из URL, если они есть
                if (minPriceParam) apiParams.append('minPrice', minPriceParam);
                if (maxPriceParam) apiParams.append('maxPrice', maxPriceParam);

                // Добавляем параметры сортировки
                let sortBy = 'ProductName';
                let sortOrder = 'asc';

                switch (effectiveSortOption) {
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
                    // 'name-asc' по умолчанию
                    default:
                        sortBy = 'ProductName';
                        sortOrder = 'asc';
                }

                apiParams.append('sortBy', sortBy);
                apiParams.append('sortOrder', sortOrder);

                // Выполняем запрос
                const response = await fetch(`https://localhost:7275/api/Products/products-by-category?${apiParams}`, {
                    method: 'GET',
                    headers: {
                        'Content-Type': 'application/json',
                    },
                });

                if (!response.ok) {
                    throw new Error(`Ошибка загрузки товаров: ${response.status} ${response.statusText}`);
                }

                if (response.status === 204) {
                    setProducts([]);
                    setLoading(false);
                    return;
                }

                const data = await response.json();
                setProducts(data);
            } catch (err) {
                console.error('Ошибка при загрузке товаров:', err);
                setError(err.message || 'Произошла ошибка при загрузке товаров.');
                setProducts([]);
            } finally {
                setLoading(false);
            }
        };

        loadProducts();
    }, [searchParams, initialCategoryNameFromUrl]); // Зависит от searchParams и начальной категории

    // --- Обработчик изменения сортировки (onChange селекта) ---
    const handleSortOptionChange = (event) => {
        const newSortOption = event.target.value;
        setSortOption(newSortOption);

        // Обновляем URL при изменении сортировки
        const newParams = new URLSearchParams(searchParams);
        newParams.set('sort', newSortOption);
        // Удаляем параметр, если он равен значению по умолчанию, чтобы не засорять URL
        if (newSortOption === 'name-asc') {
             newParams.delete('sort');
        }
        navigate(`?${newParams.toString()}`, { replace: true });
    };

    const toggleMenu = () => {
        setIsMenuOpen(!isMenuOpen);
    };

    if (loading) {
        return <LoadingSpinner message="Загрузка товаров..." />;
    }

    return (
        <div className="category-page">
            {/* Хлебные крошки */}
            <Breadcrumb categoryName={initialCategoryNameFromUrl} />

            {/* Заголовок категории */}
            <h2>{decodeURIComponent(initialCategoryNameFromUrl || '')}</h2>

            {/* --- Контейнер для фильтров и основного контента --- */}
            <div className="category-layout">
                {/* --- Боковая панель с фильтрами --- */}
                    <aside className="category-filters-sidebar">
                        <FiltersPanel
                            initialMinPrice={currentFilters.minPrice}
                            initialMaxPrice={currentFilters.maxPrice}
                            initialCategory={initialCategoryNameFromUrl} // Передаём начальную категорию
                            onFiltersChange={handleFiltersChange}
                            hideCategory={true} // Скрываем поле категории на странице категории
                        />
                        {/* --- Кнопка Применить фильтры --- */}
                        <div className="apply-filters-section-category">
                            <button onClick={handleApplyFilters} className="apply-filters-button-category">
                                Применить фильтры
                            </button>
                        </div>
                    </aside>

                {/* --- Основной контент --- */}
                <main className="category-main-content">
                    
                    {/* --- Отображение ошибки --- */}
                    {error && <div className="error-message">{error}</div>}

                    {/* Блок сортировки */}
                    <div className="div__sort-menu">
                        <div className={`sort-menu ${isMenuOpen ? 'open' : ''}`}>
                            <div
                                className="sort-menu-header"
                                onClick={toggleMenu}
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

                    {/* --- Список товаров --- */}
                    {!error && (
                        <ul className="products-list">
                            {products.map((product) => (
                                <li key={product.productId} className="product-card-wrapper">
                                    <ProductCard product={product} />
                                </li>
                            ))}
                        </ul>
                    )}
                </main>
            </div>
        </div>
    );
};

export default CategoryPage;