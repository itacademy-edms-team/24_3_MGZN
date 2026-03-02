import React, { useState, useEffect, useCallback, useRef } from 'react';
import { useSearchParams, useNavigate } from 'react-router-dom';
import ProductCard from '../../components/ProductCard.jsx';
import FiltersPanel from '../../components/FiltersPanel/FiltersPanel.tsx';
import SortMenu from "../../components/SortMenu/SortMenu.tsx"; // <<<--- Добавлен импорт
import './SearchResultsPage.css';
import LoadingSpinner from '../../components/LoadingSpinner.js';

interface ProductSearchResultDto {
  id: number;
  name: string;
  price: number;
  category: string;
  description: string;
  stockQuantity: number;
  isAvailable: boolean;
  imageUrl: string;
}

interface ProductCardPropsFormat {
  productId: number;
  productName: string;
  productPrice: number;
  imageUrl: string;
}

interface FiltersState {
  minPrice: string;
  maxPrice: string;
  category: string;
  inStock: boolean | null; // null - все товары, true - только в наличии
}

// --- НОВОЕ: Интерфейс для состояния сортировки ---
interface SortState {
  option: string; // Например, 'name-asc', 'price-desc', 'relevance'
}

const SearchResultsPage: React.FC = () => {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();

  // Используем ref для отслеживания первого рендера
  const isFirstRender = useRef(true);
  const isUpdatingFromUrl = useRef(false);

  // Получаем параметры из URL
  const queryFromUrl = searchParams.get('q') || '';
  const minPriceFromUrl = searchParams.get('minPrice') || '';
  const maxPriceFromUrl = searchParams.get('maxPrice') || '';
  const categoryFromUrl = searchParams.get('category') || '';
  const inStockFromUrl = searchParams.get('inStock'); // 'true' или null
  // --- НОВОЕ: Получаем параметр сортировки из URL ---
  const sortFromUrl = searchParams.get('sort') || 'relevance'; // Значение по умолчанию для поисковой страницы

  const [results, setResults] = useState<ProductSearchResultDto[]>([]);
  const [loading, setLoading] = useState<boolean>(false);
  const [error, setError] = useState<string | null>(null);

  // Состояние для фильтров (инициализируем из URL)
  const [currentFilters, setCurrentFilters] = useState<FiltersState>({
    minPrice: minPriceFromUrl,
    maxPrice: maxPriceFromUrl,
    category: categoryFromUrl,
    inStock: inStockFromUrl === 'true' ? true : null,
  });

  // --- НОВОЕ: Состояние для сортировки (инициализируем из URL) ---
  const [currentSort, setCurrentSort] = useState<SortState>({
    option: sortFromUrl,
  });

  const API_BASE_URL = process.env.REACT_APP_API_BASE_URL || 'https://localhost:7275/api';

  // --- НОВОЕ: Обработчик изменения сортировки ---
  const handleSortOptionChange = useCallback((newSortOption: string) => {
    setCurrentSort({ option: newSortOption });

    // Обновляем URL при изменении сортировки
    const newParams = new URLSearchParams(searchParams);

    // Обновляем параметр сортировки
    if (newSortOption === 'relevance') {
        // Если выбрана "релевантность", удаляем параметр sort из URL (значение по умолчанию)
        newParams.delete('sort');
    } else {
        // Иначе устанавливаем новое значение
        newParams.set('sort', newSortOption);
    }

    // Устанавливаем флаг, чтобы не обновлять сортировку из URL при навигации
    isUpdatingFromUrl.current = true;

    // Обновляем URL (это вызовет эффект с searchParams)
    navigate(`?${newParams.toString()}`, { replace: true });
  }, [navigate, searchParams]);

  // Обработчик изменения фильтров (из FiltersPanel)
  const handleFiltersChange = useCallback((filters: {
    minPrice: string;
    maxPrice: string;
    category: string;
    inStock: boolean | null;
  }) => {
    setCurrentFilters(filters);
  }, []);

  // Обработчик кнопки "Применить фильтры"
  const handleApplyFilters = useCallback(() => {
    const newParams = new URLSearchParams(searchParams);

    // Обновляем параметры фильтров
    if (currentFilters.minPrice) {
      newParams.set('minPrice', currentFilters.minPrice);
    } else {
      newParams.delete('minPrice');
    }

    if (currentFilters.maxPrice) {
      newParams.set('maxPrice', currentFilters.maxPrice);
    } else {
      newParams.delete('maxPrice');
    }

    if (currentFilters.category) {
      newParams.set('category', currentFilters.category);
    } else {
      newParams.delete('category');
    }

    // Обновляем параметр наличия
    if (currentFilters.inStock === true) {
      newParams.set('inStock', 'true');
    } else {
      newParams.delete('inStock');
    }

    // --- СОРТИРОВКА: Сохраняем текущую сортировку ---
    if (currentSort.option !== 'relevance') { // Не сохраняем 'relevance', если это значение по умолчанию
        newParams.set('sort', currentSort.option);
    } else {
        newParams.delete('sort'); // Удаляем, если 'relevance'
    }

    // Сохраняем поисковый запрос
    if (queryFromUrl) {
      newParams.set('q', queryFromUrl);
    }

    // Устанавливаем флаг, чтобы не обновлять фильтры из URL при навигации
    isUpdatingFromUrl.current = true;

    // Обновляем URL (это вызовет эффект с searchParams)
    navigate(`?${newParams.toString()}`, { replace: true });
  }, [currentFilters, currentSort, queryFromUrl, navigate, searchParams]);

  // --- НОВОЕ: Синхронизируем currentSort с URL при изменении searchParams ---
  useEffect(() => {
    // Если обновление происходит из handleApplyFilters или handleSortOptionChange, пропускаем
    if (isUpdatingFromUrl.current) {
      isUpdatingFromUrl.current = false;
      return;
    }

    // Извлекаем сортировку из URL
    const urlSort = searchParams.get('sort') || 'relevance';

    // Обновляем состояние сортировки, только если она изменилась
    if (urlSort !== currentSort.option) {
        setCurrentSort({ option: urlSort });
    }
  }, [searchParams, currentSort.option]); // Зависит от searchParams и текущего состояния сортировки

  // Синхронизируем currentFilters с URL при изменении searchParams
  useEffect(() => {
    // Если обновление происходит из handleApplyFilters, пропускаем
    if (isUpdatingFromUrl.current) {
      isUpdatingFromUrl.current = false;
      return;
    }

    setCurrentFilters({
      minPrice: minPriceFromUrl,
      maxPrice: maxPriceFromUrl,
      category: categoryFromUrl,
      inStock: inStockFromUrl === 'true' ? true : null,
    });
  }, [minPriceFromUrl, maxPriceFromUrl, categoryFromUrl, inStockFromUrl]);

  // Эффект для поиска
  useEffect(() => {
    // Пропускаем первый рендер, если нет параметров поиска
    if (isFirstRender.current) {
      isFirstRender.current = false;
      if (!queryFromUrl && !minPriceFromUrl && !maxPriceFromUrl && !categoryFromUrl && !inStockFromUrl && currentSort.option === 'relevance') { // <<<--- ДОБАВЛЕНО currentSort.option
        setResults([]);
        setError('Введите поисковый запрос или примените фильтры.');
        return;
      }
    }

    const performSearch = async () => {
      setLoading(true);
      setError(null);
      setResults([]);

      try {
        const apiParams = new URLSearchParams();
        if (queryFromUrl) apiParams.set('q', queryFromUrl);
        if (minPriceFromUrl) apiParams.set('minPrice', minPriceFromUrl);
        if (maxPriceFromUrl) apiParams.set('maxPrice', maxPriceFromUrl);
        if (categoryFromUrl) apiParams.set('category', categoryFromUrl);
        if (inStockFromUrl === 'true') apiParams.set('inStock', 'true');

        // --- ИСПРАВЛЕНО: Отправляем параметры сортировки в формате, ожидаемом бэкендом ---
        if (currentSort.option && currentSort.option !== 'relevance') {
            let apiSortBy = 'relevance';
            let apiSortOrder = 'desc';

            switch (currentSort.option) {
                case 'name-asc':
                    apiSortBy = 'name';
                    apiSortOrder = 'asc';
                    break;
                case 'name-desc':
                    apiSortBy = 'name';
                    apiSortOrder = 'desc';
                    break;
                case 'price-asc':
                    apiSortBy = 'price';
                    apiSortOrder = 'asc';
                    break;
                case 'price-desc':
                    apiSortBy = 'price';
                    apiSortOrder = 'desc';
                    break;
                default:
                    // 'relevance' или любое другое значение -> по умолчанию relevance, desc
                    apiSortBy = 'relevance';
                    apiSortOrder = 'desc';
            }

            // Отправляем параметры сортировки только если sortBy не 'relevance'
            if (apiSortBy !== 'relevance') {
                apiParams.set('sortBy', apiSortBy);
                apiParams.set('sortOrder', apiSortOrder);
            }
        }
        // ---

        // Если нет параметров, не выполняем поиск
        if (apiParams.toString() === '') {
          setResults([]);
          setError('Введите поисковый запрос или примените фильтры.');
          setLoading(false);
          return;
        }

        console.log('Отправка запроса к API:', `${API_BASE_URL}/search/search?${apiParams}`);

        const response = await fetch(`${API_BASE_URL}/search/search?${apiParams}`, {
          method: 'GET',
          headers: {
            'Content-Type': 'application/json',
          },
        });

        if (!response.ok) {
          let errorMessage = `Ошибка поиска: ${response.status} ${response.statusText}`;
          try {
            const errorData = await response.json();
            if (errorData && typeof errorData === 'object' && errorData.detail) {
              errorMessage = `Ошибка поиска: ${errorData.detail}`;
            } else if (typeof errorData === 'string') {
              errorMessage = `Ошибка поиска: ${errorData}`;
            }
          } catch (e) {
            console.error('Не удалось распарсить ошибку:', e);
          }
          throw new Error(errorMessage);
        }

        const data: ProductSearchResultDto[] = await response.json();
        console.log('Получены результаты:', data);
        setResults(data);

        if (data.length === 0) {
          setError('По вашему запросу и фильтрам ничего не найдено.');
        }
      } catch (err) {
        console.error('Ошибка при выполнении поиска:', err);
        setError(err instanceof Error ? err.message : 'Произошла неизвестная ошибка при поиске.');
        setResults([]);
      } finally {
        setLoading(false);
      }
    };

    performSearch();
  }, [queryFromUrl, minPriceFromUrl, maxPriceFromUrl, categoryFromUrl, inStockFromUrl, currentSort.option, API_BASE_URL]); // <<<--- ДОБАВЛЕНО currentSort.option

  const adaptApiResultToProductCardProps = (apiProduct: ProductSearchResultDto): ProductCardPropsFormat => {
    return {
      productId: apiProduct.id,
      productName: apiProduct.name,
      productPrice: apiProduct.price,
      imageUrl: apiProduct.imageUrl,
    };
  };

  return (
    <div className="search-results-page">
      <h2>
        Результаты поиска по запросу: "{queryFromUrl}"
        {!queryFromUrl && <span> (все товары)</span>}
      </h2>

      <div className="search-results-layout">
        <aside className="search-filters-sidebar">
          <FiltersPanel
            initialMinPrice={currentFilters.minPrice}
            initialMaxPrice={currentFilters.maxPrice}
            initialCategory={currentFilters.category}
            initialInStock={currentFilters.inStock}
            onFiltersChange={handleFiltersChange}
            hideCategory={false}
            hideInStock={false}
          />
          <div className="apply-filters-section-search">
            <button onClick={handleApplyFilters} className="apply-filters-button-search">
              Применить фильтры
            </button>
          </div>
        </aside>

        <main className="search-results-main-content">
          {/* --- ДОБАВЛЕНО: Компонент сортировки --- */}
          <div className="sort-menu__container">
            <SortMenu
              currentSortOption={currentSort.option} // Передаём текущую опцию сортировки
              onSortOptionChange={handleSortOptionChange} // Передаём обработчик изменения сортировки
            />
          </div>
          {/* --- КОНЕЦ: Компонент сортировки --- */}

          {loading && <div className="loading-indicator"><LoadingSpinner /></div>}
          {error && <div className="error-message">{error}</div>}

          {!loading && !error && results.length > 0 && (
            <div className="search-results-grid">
              <h3>Найдено {results.length} товаров:</h3>
              <div className="products-grid">
                {results.map((apiProduct) => {
                  const adaptedProduct = adaptApiResultToProductCardProps(apiProduct);
                  return (
                    <div key={apiProduct.id} className="product-card-container">
                      <ProductCard product={adaptedProduct} />
                    </div>
                  );
                })}
              </div>
            </div>
          )}
        </main>
      </div>
    </div>
  );
};

export default SearchResultsPage;