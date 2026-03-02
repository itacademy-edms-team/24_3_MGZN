import React, { useState, useEffect, useCallback, useRef } from 'react';
import { useParams, useSearchParams, useNavigate } from 'react-router-dom';
import Breadcrumb from '../components/Breadcrumb';
import ProductCard from '../components/ProductCard';
import FiltersPanel from '../components/FiltersPanel/FiltersPanel.tsx';
import LoadingSpinner from '../components/LoadingSpinner';
import SortMenu from "../components/SortMenu/SortMenu.tsx"; // Убедитесь, что путь правильный
import './CategoryPage.css';

// Интерфейсы для типизации
interface ProductDto {
    productId: number;
    productName: string;
    productDescription: string;
    productPrice: number;
    productAvailability: boolean;
    productStockQuantity: number;
    imageUrl: string | null;
    productCategoryId: number;
    productCategoryName: string | null;
}

interface FiltersState {
    minPrice: string;
    maxPrice: string;
    category: string;
    inStock: boolean | null; // null - все товары, true - только в наличии
}

const CategoryPage: React.FC = () => {
    const { categoryName: initialCategoryNameFromUrl } = useParams<{ categoryName: string }>();
    const [searchParams] = useSearchParams();
    const navigate = useNavigate();

    // Ref для отслеживания обновлений из URL
    const isUpdatingFromUrl = useRef(false);

    // --- Состояния ---
    const [products, setProducts] = useState<ProductDto[]>([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);

    // --- Состояния для фильтров (синхронизированы с URL) ---
    const [currentFilters, setCurrentFilters] = useState<FiltersState>({
        minPrice: searchParams.get('minPrice') || '',
        maxPrice: searchParams.get('maxPrice') || '',
        category: initialCategoryNameFromUrl || '',
        inStock: searchParams.get('inStock') === 'true' ? true : null,
    });

    // --- Состояние для сортировки (синхронизировано с URL) ---
    const [sortOption, setSortOption] = useState(() => {
        const urlSort = searchParams.get('sort');
        if (['name-asc', 'name-desc', 'price-asc', 'price-desc'].includes(urlSort || '')) {
            return urlSort as string;
        }
        return 'name-asc';
    });

    const API_BASE_URL = process.env.REACT_APP_API_BASE_URL || 'https://localhost:7275/api';

    // --- Обработчик изменения фильтров (из FiltersPanel) ---
    const handleFiltersChange = useCallback((filters: {
        minPrice: string;
        maxPrice: string;
        category: string;
        inStock: boolean | null;
    }) => {
        setCurrentFilters(prev => ({
            ...prev,
            minPrice: filters.minPrice,
            maxPrice: filters.maxPrice,
            inStock: filters.inStock,
            // category не обновляем, так как hideCategory=true в FiltersPanel
        }));
    }, []);

    // --- Обработчик кнопки "Применить фильтры" ---
    const handleApplyFilters = useCallback(() => {
        if (!initialCategoryNameFromUrl) {
            setError('Категория не указана.');
            return;
        }

        const newParams = new URLSearchParams();

        // Добавляем фильтры по цене
        if (currentFilters.minPrice) {
            newParams.set('minPrice', currentFilters.minPrice);
        }
        if (currentFilters.maxPrice) {
            newParams.set('maxPrice', currentFilters.maxPrice);
        }

        // Добавляем фильтр по наличию
        if (currentFilters.inStock === true) {
            newParams.set('inStock', 'true');
        }

        // Добавляем сортировку
        if (sortOption !== 'name-asc') {
            newParams.set('sort', sortOption);
        }

        // Устанавливаем флаг, чтобы не обновлять фильтры из URL при навигации
        isUpdatingFromUrl.current = true;

        // Навигация с новыми параметрами
        navigate(`/category/${encodeURIComponent(initialCategoryNameFromUrl)}?${newParams.toString()}`, { replace: true });
    }, [currentFilters, sortOption, initialCategoryNameFromUrl, navigate]);

    // --- Синхронизируем currentFilters с URL при изменении searchParams ---
    useEffect(() => {
        // Если обновление происходит из handleApplyFilters, пропускаем
        if (isUpdatingFromUrl.current) {
            isUpdatingFromUrl.current = false;
            return;
        }

        setCurrentFilters(prev => ({
            ...prev,
            minPrice: searchParams.get('minPrice') || '',
            maxPrice: searchParams.get('maxPrice') || '',
            inStock: searchParams.get('inStock') === 'true' ? true : null,
        }));
    }, [searchParams]);

    // --- Эффект: Загрузка товаров при изменении URL (включая сортировку) ---
    useEffect(() => {
        const loadProducts = async () => {
            if (!initialCategoryNameFromUrl) {
                setError('Категория не указана.');
                setLoading(false);
                return;
            }

            try {
                setLoading(true);
                setError(null);

                // Получаем параметры из URL
                const minPriceParam = searchParams.get('minPrice');
                const maxPriceParam = searchParams.get('maxPrice');
                const inStockParam = searchParams.get('inStock');
                const sortParam = searchParams.get('sort');

                // Определяем эффективную сортировку
                let effectiveSortOption = sortParam;
                if (!['name-asc', 'name-desc', 'price-asc', 'price-desc'].includes(effectiveSortOption || '')) {
                    effectiveSortOption = 'name-asc';
                }

                // Обновляем состояние сортировки, если она изменилась в URL
                if (effectiveSortOption !== sortOption) {
                    setSortOption(effectiveSortOption || 'name-asc');
                }

                // Формируем параметры для API-запроса
                const apiParams = new URLSearchParams({
                    categoryName: initialCategoryNameFromUrl,
                });

                // Добавляем фильтры по цене
                if (minPriceParam) apiParams.append('minPrice', minPriceParam);
                if (maxPriceParam) apiParams.append('maxPrice', maxPriceParam);

                // Добавляем фильтр по наличию
                if (inStockParam === 'true') {
                    apiParams.append('inStock', 'true');
                }

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
                    default: // 'name-asc'
                        sortBy = 'ProductName';
                        sortOrder = 'asc';
                }

                apiParams.append('sortBy', sortBy);
                apiParams.append('sortOrder', sortOrder);

                console.log('Запрос к API:', `${API_BASE_URL}/Products/products-by-category?${apiParams}`);

                // Выполняем запрос
                const response = await fetch(`${API_BASE_URL}/Products/products-by-category?${apiParams}`, {
                    method: 'GET',
                    headers: {
                        'Content-Type': 'application/json',
                    },
                });

                if (!response.ok) {
                    if (response.status === 404) {
                        setProducts([]);
                        return;
                    }
                    throw new Error(`Ошибка загрузки товаров: ${response.status} ${response.statusText}`);
                }

                if (response.status === 204) {
                    setProducts([]);
                    return;
                }

                const data: ProductDto[] = await response.json();
                console.log('Получены товары:', data);
                setProducts(data);
            } catch (err) {
                console.error('Ошибка при загрузке товаров:', err);
                setError(err instanceof Error ? err.message : 'Произошла ошибка при загрузке товаров.');
                setProducts([]);
            } finally {
                setLoading(false);
            }
        };

        loadProducts();
    }, [searchParams, initialCategoryNameFromUrl, API_BASE_URL]); // Убрали sortOption из зависимостей, так как он теперь обновляется внутри useEffect

    // --- Обработчик изменения сортировки ---
    const handleSortOptionChange = (newSortOption: string) => { // <<<--- ИЗМЕНЕНО: принимает строку, а не ChangeEvent
        // Обновляем URL при изменении сортировки
        const newParams = new URLSearchParams(searchParams);

        // Сохраняем существующие фильтры
        if (currentFilters.minPrice) newParams.set('minPrice', currentFilters.minPrice);
        if (currentFilters.maxPrice) newParams.set('maxPrice', currentFilters.maxPrice);
        if (currentFilters.inStock === true) newParams.set('inStock', 'true');

        // Обновляем сортировку
        if (newSortOption === 'name-asc') {
            newParams.delete('sort'); // Удаляем параметр sort, если значение по умолчанию
        } else {
            newParams.set('sort', newSortOption);
        }

        // Обновляем состояние *после* обновления URL, чтобы триггерить useEffect
        setSortOption(newSortOption);
        // Навигация с новыми параметрами
        navigate(`?${newParams.toString()}`, { replace: true });
    };

    // --- УДАЛЕНО: toggleMenu, isMenuOpen ---

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
                        initialCategory={initialCategoryNameFromUrl}
                        initialInStock={currentFilters.inStock}
                        onFiltersChange={handleFiltersChange}
                        hideCategory={true}
                        hideInStock={false}
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

                    {/* --- Блок сортировки (компонент) --- */}
                    <SortMenu
                        currentSortOption={sortOption}
                        onSortOptionChange={handleSortOptionChange} // <<<--- ПЕРЕДАЁМ НОВУЮ ФУНКЦИЮ
                    />

                    {/* --- Список товаров --- */}
                    {!error && (
                        <>
                            {products.length === 0 ? (
                                <div className="no-products-message">
                                    {currentFilters.inStock === true 
                                        ? 'В этой категории нет товаров в наличии.'
                                        : 'В этой категории нет товаров.'}
                                </div>
                            ) : (
                                <ul className="products-list">
                                    {products.map((product) => (
                                        <li key={product.productId} className="product-card-wrapper">
                                            <ProductCard product={product} />
                                        </li>
                                    ))}
                                </ul>
                            )}
                        </>
                    )}
                </main>
            </div>
        </div>
    );
};

export default CategoryPage;