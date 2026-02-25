import React, { useState, useEffect, useCallback, useRef } from 'react';
import { useSearchParams, useNavigate } from 'react-router-dom';
import ProductCard from '../../components/ProductCard.jsx';
import FiltersPanel from '../../components/FiltersPanel/FiltersPanel.tsx';
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

const SearchResultsPage: React.FC = () => {
  const [searchParams, setSearchParams] = useSearchParams();
  const navigate = useNavigate();
  
  // Используем ref для отслеживания первого рендера
  const isFirstRender = useRef(true);

  // Получаем параметры из URL
  const queryFromUrl = searchParams.get('q') || '';
  const minPriceFromUrl = searchParams.get('minPrice') || '';
  const maxPriceFromUrl = searchParams.get('maxPrice') || '';
  const categoryFromUrl = searchParams.get('category') || '';

  const [results, setResults] = useState<ProductSearchResultDto[]>([]);
  const [loading, setLoading] = useState<boolean>(false);
  const [error, setError] = useState<string | null>(null);

  // Состояние для фильтров (инициализируем из URL)
  const [currentFilters, setCurrentFilters] = useState({
    minPrice: minPriceFromUrl,
    maxPrice: maxPriceFromUrl,
    category: categoryFromUrl,
  });

  const API_BASE_URL = process.env.REACT_APP_API_BASE_URL || 'https://localhost:7275/api';

  // Обработчик изменения фильтров (из FiltersPanel)
  const handleFiltersChange = useCallback((filters: { minPrice: string; maxPrice: string; category: string }) => {
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

    // Сохраняем поисковый запрос
    if (queryFromUrl) {
      newParams.set('q', queryFromUrl);
    }

    // Обновляем URL (это вызовет эффект с searchParams)
    navigate(`?${newParams.toString()}`, { replace: true });
  }, [currentFilters, queryFromUrl, navigate, searchParams]);

  // Синхронизируем currentFilters с URL при изменении searchParams
  useEffect(() => {
    setCurrentFilters({
      minPrice: minPriceFromUrl,
      maxPrice: maxPriceFromUrl,
      category: categoryFromUrl,
    });
  }, [minPriceFromUrl, maxPriceFromUrl, categoryFromUrl]);

  // Эффект для поиска
  useEffect(() => {
    // Пропускаем первый рендер, если нет параметров поиска
    if (isFirstRender.current) {
      isFirstRender.current = false;
      if (!queryFromUrl && !minPriceFromUrl && !maxPriceFromUrl && !categoryFromUrl) {
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

        // Если нет параметров, не выполняем поиск
        if (apiParams.toString() === '') {
          setResults([]);
          setError('Введите поисковый запрос или примените фильтры.');
          setLoading(false);
          return;
        }

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
  }, [queryFromUrl, minPriceFromUrl, maxPriceFromUrl, categoryFromUrl, API_BASE_URL]);

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
            onFiltersChange={handleFiltersChange}
            hideCategory={false}
          />
          <div className="apply-filters-section-search">
            <button onClick={handleApplyFilters} className="apply-filters-button-search">
              Применить фильтры
            </button>
          </div>
        </aside>

        <main className="search-results-main-content">
          {loading && <div className="loading-indicator"><LoadingSpinner /></div>}
          {error && <div className="error-message">{error}</div>}

          {!loading && !error && results.length > 0 && (
            <div className="search-results-grid">
              <h3>Найдено {results.length} товаров:</h3>
              <div className="products-grid">
                {results.map((apiProduct) => {
                  const adaptedProduct = adaptApiResultToProductCardProps(apiProduct);
                  return (
                    <ProductCard key={apiProduct.id} product={adaptedProduct} />
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