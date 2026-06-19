// src/components/SearchResultsPage/SearchResultsPage.tsx
import React, { useState, useEffect, useCallback, useMemo, useRef, memo } from 'react';
import { useSearchParams } from 'react-router-dom';
import { useDebounce } from '../../hooks/useDebounce.ts';
import { useProductSearch } from '../../hooks/useProductSearch.ts';
import { parseFiltersFromUrl } from '../../utils/filters.ts';
import { FiltersState, SearchRequestDto } from '../../types/search.ts';
import FiltersPanel from '../../components/FiltersPanel/FiltersPanel.tsx';
import ActiveFiltersBar from '../../components/ActiveFiltersBar.tsx';
import ProductCard from '../../components/ProductCard.jsx';
import LoadingSpinner from '../../components/LoadingSpinner.tsx';
import SortMenu, { SortOption } from '../../components/SortMenu/SortMenu.tsx';
import { API_BASE_URL } from '../../config/api.js';

// Swiper imports
import { Swiper, SwiperSlide } from 'swiper/react';
import { Pagination } from 'swiper/modules';
import 'swiper/css';
import 'swiper/css/pagination';

import './SearchResultsPage.css';

interface SearchResultsPageProps {
  forcedCategory?: string;
  hideSearchQuery?: boolean;
  pageTitleOverride?: string;
}

type SpecFilterValue = string | number | { Min?: number; Max?: number } | null;

const PAGE_SIZE = 12;
const DEBOUNCE_DELAY = 400;

// 🔧 FIX: Выносим блок рекомендаций в отдельный мемоизированный компонент
const RecommendationsSection = memo<{
  products: Array<{ productId: string; productName: string; productPrice: number; imageUrl?: string }>;
}>(({ products }) => {
  const [swiperInstance, setSwiperInstance] = useState<any>(null);
  const [showNavigationButtons, setShowNavigationButtons] = useState(false);

  useEffect(() => {
    const checkNavigationNeed = () => {
      if (!swiperInstance) {
        setShowNavigationButtons(false);
        return;
      }

      const windowWidth = window.innerWidth;
      let slidesPerView = 1;

      if (windowWidth >= 1024) {
        slidesPerView = 4;
      } else if (windowWidth >= 768) {
        slidesPerView = 3;
      } else if (windowWidth >= 640) {
        slidesPerView = 2;
      } else {
        slidesPerView = 1;
      }

      setShowNavigationButtons(products.length > slidesPerView);
    };

    checkNavigationNeed();
    window.addEventListener('resize', checkNavigationNeed);
    return () => window.removeEventListener('resize', checkNavigationNeed);
  }, [swiperInstance, products.length]);

  const handlePrevClick = useCallback(() => {
    if (swiperInstance) {
      swiperInstance.slidePrev();
    }
  }, [swiperInstance]);

  const handleNextClick = useCallback(() => {
    if (swiperInstance) {
      swiperInstance.slideNext();
    }
  }, [swiperInstance]);

  // 🔧 FIX: Используем useMemo для стабилизации слайдов
  const slides = useMemo(() => {
    return products.map(product => (
      <SwiperSlide key={`rec-${product.productId}`} className="recommendation-slide">
        <ProductCard product={product} />
      </SwiperSlide>
    ));
  }, [products]);

  if (products.length === 0) return null;

  return (
    <div className="recommendations-section">
      <h3 className="recommendations-title">Рекомендуем также</h3>
      
      <div className="recommendations-slider-wrapper">
        {showNavigationButtons && (
          <button 
            className="recommendations-swiper-button-prev" 
            onClick={handlePrevClick}
            aria-label="Предыдущий слайд"
          >
            &lt;
          </button>
        )}

        <div className="recommendations-swiper-container">
          <Swiper
            onSwiper={setSwiperInstance}
            modules={[Pagination]}
            spaceBetween={20}
            slidesPerView={1}
            pagination={{ clickable: true }}
            breakpoints={{
              640: {
                slidesPerView: 2,
                spaceBetween: 20,
              },
              768: {
                slidesPerView: 3,
                spaceBetween: 30,
              },
              1024: {
                slidesPerView: 4,
                spaceBetween: 32,
              },
            }}
            className="recommendations-swiper"
          >
            {slides}
          </Swiper>
        </div>

        {showNavigationButtons && (
          <button 
            className="recommendations-swiper-button-next" 
            onClick={handleNextClick}
            aria-label="Следующий слайд"
          >
            &gt;
          </button>
        )}
      </div>
    </div>
  );
});

RecommendationsSection.displayName = 'RecommendationsSection';

// 🔧 FIX: Выносим сетку товаров в отдельный компонент
const ProductsGrid = memo<{
  products: Array<{ productId: string; productName: string; productPrice: number; imageUrl?: string }>;
}>(({ products }) => {
  // 🔧 FIX: Стабилизируем продукты через useMemo
  const productElements = useMemo(() => {
    return products.map((product) => (
      <div 
        key={`main-${product.productId}`} 
        className="product-card-wrapper"
        style={{ animation: 'fadeInUp 0.4s ease-out forwards' }}
      >
        <ProductCard product={product} />
      </div>
    ));
  }, [products]);

  return (
    <div className="products-grid">
      {productElements}
    </div>
  );
});

ProductsGrid.displayName = 'ProductsGrid';

const SearchResultsPage = memo<SearchResultsPageProps>(({
  forcedCategory,
  hideSearchQuery = false,
  pageTitleOverride,
}) => {
  const [searchParams, setSearchParams] = useSearchParams();
  
  const { results, recommended, loading, error, hasMore, search, clear, loadMore } = useProductSearch(API_BASE_URL);
  
  const isInitialMount = useRef(true);
  const prevForcedCategory = useRef(forcedCategory);
  const isUpdatingFromUrl = useRef(false);
  const lastSearchKeyRef = useRef<string>('');
  const lastAppliedSpecsParamRef = useRef<string | null>(null);
  const prevUrlQueryRef = useRef<string>('');
  
  const isManualPagination = useRef(false);
  const lastSearchParamsRef = useRef<SearchRequestDto | null>(null);

  const urlFilters = useMemo(() => parseFiltersFromUrl(searchParams), [searchParams]);
  const urlQuery = searchParams.get('q') || '';
  
  const urlSpecFilters = useMemo(() => {
    const specsParam = searchParams.get('specs');
    if (specsParam) {
      try {
        return JSON.parse(specsParam);
      } catch (e) {
        console.error('Ошибка парсинга specFilters из URL:', e);
        return null;
      }
    }
    return null;
  }, [searchParams]);

  const [filters, setFilters] = useState<FiltersState>(() => ({
    query: urlQuery,
    minPrice: urlFilters.minPrice || '',
    maxPrice: urlFilters.maxPrice || '',
    category: forcedCategory !== undefined ? forcedCategory : (urlFilters.category || ''),
    inStock: urlFilters.inStock !== undefined ? urlFilters.inStock : null,
  }));

  const [specFiltersState, setSpecFiltersState] = useState<Record<string, SpecFilterValue> | null>(urlSpecFilters);
  const specFilters = useMemo(() => specFiltersState, [specFiltersState]);
  
  const [specDisplayNames, setSpecDisplayNames] = useState<Record<string, string>>({});
  
  const [sort, setSort] = useState<{ option: string; order: 'asc' | 'desc' }>(() => ({
    option: searchParams.get('sort') || 'relevance',
    order: (searchParams.get('order') as 'asc' | 'desc') || 'desc',
  }));

  const debouncedFilters = useDebounce(filters, DEBOUNCE_DELAY);
  const debouncedSort = useDebounce(sort, DEBOUNCE_DELAY);
  const debouncedSpecFilters = useDebounce(specFilters, DEBOUNCE_DELAY);

  // Синхронизация URL -> State
  useEffect(() => {
    if (isUpdatingFromUrl.current) {
      isUpdatingFromUrl.current = false;
      return;
    }

    const newFilters: FiltersState = {
      query: urlQuery,
      minPrice: urlFilters.minPrice || '',
      maxPrice: urlFilters.maxPrice || '',
      category: forcedCategory !== undefined ? forcedCategory : (urlFilters.category || ''),
      inStock: urlFilters.inStock !== undefined ? urlFilters.inStock : null,
    };
    
    let hasFilterChanges = false;
    setFilters(prev => {
      const hasChanges = Object.keys(newFilters).some(
        key => prev[key as keyof FiltersState] !== newFilters[key as keyof FiltersState]
      );
      hasFilterChanges = hasChanges;
      return hasChanges ? newFilters : prev;
    });
    
    const newSort = {
      option: searchParams.get('sort') || 'relevance',
      order: (searchParams.get('order') as 'asc' | 'desc') || 'desc',
    };
    
    let hasSortChanges = false;
    setSort(prev => {
      if (prev.option !== newSort.option || prev.order !== newSort.order) {
        hasSortChanges = true;
        return newSort;
      }
      return prev;
    });
    
    const currentSpecsParam = searchParams.get('specs');
    if (currentSpecsParam !== lastAppliedSpecsParamRef.current) {
      lastAppliedSpecsParamRef.current = currentSpecsParam;
      if (urlSpecFilters !== undefined) {
        setSpecFiltersState(urlSpecFilters);
      }
    }
    
    if (hasFilterChanges || hasSortChanges || urlSpecFilters) {
      clear();
    }
  }, [urlQuery, urlFilters, forcedCategory, searchParams, urlSpecFilters, clear]);

  useEffect(() => {
    if (isInitialMount.current) {
      return;
    }
    
    if (isUpdatingFromUrl.current) {
      return;
    }
    
    const queryChanged = prevUrlQueryRef.current !== urlQuery;
    
    if (queryChanged && urlQuery !== '') {
      if (forcedCategory === undefined) {
        setSpecFiltersState(null);
      }
    }
    
    prevUrlQueryRef.current = urlQuery;
  }, [urlQuery, forcedCategory]);

  useEffect(() => {
    if (prevForcedCategory.current !== forcedCategory) {
      prevForcedCategory.current = forcedCategory;
      
      if (forcedCategory !== undefined && filters.category !== forcedCategory) {
        isUpdatingFromUrl.current = true;
        setFilters(prev => ({ ...prev, category: forcedCategory }));
        setSpecFiltersState(null);
        setSpecDisplayNames({});
        clear();
      }
    }
  }, [forcedCategory, filters.category, clear]);

  // Синхронизация State -> URL
  useEffect(() => {
    if (isInitialMount.current) {
      isInitialMount.current = false;
      return;
    }

    const params = new URLSearchParams();

    if (debouncedFilters.query) params.set('q', debouncedFilters.query);
    if (debouncedFilters.minPrice) params.set('minPrice', debouncedFilters.minPrice);
    if (debouncedFilters.maxPrice) params.set('maxPrice', debouncedFilters.maxPrice);
    if (debouncedFilters.category && !forcedCategory) params.set('category', debouncedFilters.category);
    if (debouncedFilters.inStock === true) params.set('inStock', 'true');
    if (debouncedFilters.inStock === false) params.set('inStock', 'false');
    if (debouncedSort.option !== 'relevance') params.set('sort', debouncedSort.option);
    if (debouncedSort.order !== 'desc') params.set('order', debouncedSort.order);
    
    if (debouncedSpecFilters && Object.keys(debouncedSpecFilters).length > 0) {
      try {
        params.set('specs', JSON.stringify(debouncedSpecFilters));
      } catch (e) {
        console.error('Ошибка сериализации спецификаций:', e);
      }
    }

    const newParamsString = params.toString();
    const currentParamsString = searchParams.toString();
    
    if (currentParamsString !== newParamsString) {
      isUpdatingFromUrl.current = true;
      setSearchParams(params, { replace: true });
    }
  }, [debouncedFilters, debouncedSort, debouncedSpecFilters, forcedCategory, setSearchParams, searchParams]);

  // Основной эффект поиска (только при изменении фильтров/сортировки)
  useEffect(() => {
    if (isManualPagination.current) {
      return;
    }

    const hasSearchCriteria =
      debouncedFilters.query?.trim() ||
      debouncedFilters.category ||
      debouncedFilters.minPrice ||
      debouncedFilters.maxPrice ||
      debouncedFilters.inStock != null ||
      (debouncedSpecFilters && Object.keys(debouncedSpecFilters).length > 0);

    if (!hasSearchCriteria) {
      if (results.length > 0 || recommended.length > 0) {
        clear();
      }
      lastSearchKeyRef.current = '';
      lastSearchParamsRef.current = null;
      return;
    }

    const searchKey = JSON.stringify({
      query: debouncedFilters.query?.trim() ?? '',
      category: debouncedFilters.category,
      minPrice: debouncedFilters.minPrice,
      maxPrice: debouncedFilters.maxPrice,
      inStock: debouncedFilters.inStock,
      specFilters: debouncedSpecFilters,
      sortBy: debouncedSort.option,
      sortOrder: debouncedSort.order,
    });

    if (searchKey === lastSearchKeyRef.current) {
      return;
    }
    lastSearchKeyRef.current = searchKey;

    const minPrice = debouncedFilters.minPrice ? parseFloat(debouncedFilters.minPrice) : null;
    const maxPrice = debouncedFilters.maxPrice ? parseFloat(debouncedFilters.maxPrice) : null;
    
    if (minPrice !== null && maxPrice !== null && minPrice > maxPrice) {
      console.warn('Min price больше Max price');
      return;
    }

    const request: SearchRequestDto = {
      query: debouncedFilters.query?.trim() ?? '',
      limit: PAGE_SIZE,
      offset: 0,
      category: debouncedFilters.category || null,
      minPrice: minPrice,
      maxPrice: maxPrice,
      inStock: debouncedFilters.inStock,
      specFilters: debouncedSpecFilters,
      sortBy: debouncedSort.option,
      sortOrder: debouncedSort.order,
    };

    lastSearchParamsRef.current = request;
    
    search(request, false);
  }, [debouncedFilters, debouncedSort, debouncedSpecFilters, search, clear, results.length, recommended.length]);

  const handleLoadMore = useCallback(() => {
    if (!lastSearchParamsRef.current) return;
    
    isManualPagination.current = true;
    
    const request: SearchRequestDto = {
      ...lastSearchParamsRef.current,
      offset: results.length,
      limit: PAGE_SIZE,
    };
    
    loadMore(request);
    
    setTimeout(() => {
      isManualPagination.current = false;
    }, 0);
  }, [results.length, loadMore]);

  const handleBasicFilterChange = useCallback((changes: Partial<FiltersState>) => {
    isManualPagination.current = false;
    setFilters(prev => ({ ...prev, ...changes }));
  }, []);

  const handleSpecFilterChange = useCallback((specName: string, value: SpecFilterValue) => {
    isManualPagination.current = false;
    setSpecFiltersState(prev => {
      if (value == null) {
        if (!prev) return null;
        const { [specName]: _, ...rest } = prev;
        return Object.keys(rest).length > 0 ? rest : null;
      }
      
      const next = prev ? { ...prev } : {};
      
      const isEmpty = (val: SpecFilterValue): boolean => {
        if (val == null || val === '') return true;
        if (typeof val === 'object') {
          const hasValue = Object.values(val).some(v => v != null && v !== '');
          return !hasValue;
        }
        return false;
      };
      
      if (isEmpty(value)) {
        delete next[specName];
      } else {
        next[specName] = value;
      }
      
      return Object.keys(next).length > 0 ? next : null;
    });
  }, []);

  const handleClearSpecFilters = useCallback(() => {
    isManualPagination.current = false;
    setSpecFiltersState(null);
  }, []);

  const handleRemoveBasicFilter = useCallback((key: keyof FiltersState) => {
    if (key === 'category' && forcedCategory !== undefined) return;
    isManualPagination.current = false;
    setFilters(prev => ({ ...prev, [key]: key === 'inStock' ? null : '' }));
  }, [forcedCategory]);

  const handleRemoveSpecFilter = useCallback((specName: string) => {
    isManualPagination.current = false;
    setSpecFiltersState(prev => {
      if (!prev) return null;
      const next = { ...prev };
      delete next[specName];
      return Object.keys(next).length > 0 ? next : null;
    });
  }, []);

  const handleClearAllFilters = useCallback(() => {
    isManualPagination.current = false;
    setFilters(prev => ({
      query: prev.query, 
      minPrice: '',
      maxPrice: '',
      category: forcedCategory !== undefined ? forcedCategory : '',
      inStock: null,
    }));
    setSpecFiltersState(null);
    setSpecDisplayNames({});
    setSort({ option: 'relevance', order: 'desc' });

    const params = new URLSearchParams();
    if (filters.query) params.set('q', filters.query);
    if (forcedCategory !== undefined) params.set('category', forcedCategory);
    setSearchParams(params, { replace: true });
    clear();
    lastSearchKeyRef.current = '';
    lastSearchParamsRef.current = null;
  }, [forcedCategory, setSearchParams, clear, filters.query]);

  const handleSpecsLoaded = useCallback((specs: Array<{ name: string; displayName: string }>) => {
    const nameMap: Record<string, string> = {};
    specs.forEach(spec => {
      if (spec.name && spec.displayName) {
        nameMap[spec.name] = spec.displayName;
      }
    });
    setSpecDisplayNames(nameMap);
  }, []);

  const handleSortMenuChange = useCallback((newSortOption: SortOption) => {
    isManualPagination.current = false;
    let newOption = 'relevance';
    let newOrder: 'asc' | 'desc' = 'desc';

    switch (newSortOption) {
      case 'name-asc':
        newOption = 'name';
        newOrder = 'asc';
        break;
      case 'name-desc':
        newOption = 'name';
        newOrder = 'desc';
        break;
      case 'price-asc':
        newOption = 'price';
        newOrder = 'asc';
        break;
      case 'price-desc':
        newOption = 'price';
        newOrder = 'desc';
        break;
      case 'relevance':
        newOption = 'relevance';
        newOrder = 'desc';
        break;
    }

    setSort({ option: newOption, order: newOrder });
  }, []);

  const getCurrentSortOptionForMenu = useCallback((): SortOption => {
    if (sort.option === 'relevance') return 'relevance';
    return `${sort.option}-${sort.order}` as SortOption;
  }, [sort.option, sort.order]);

  // 🔧 FIX: Мемоизируем адаптированные продукты
  const adaptedProducts = useMemo(() => {
    return results.map(p => ({
      productId: p.id,
      productName: p.name,
      productPrice: p.price,
      imageUrl: p.imageUrl,
    }));
  }, [results]);

  // 🔧 FIX: Мемоизируем адаптированные рекомендации
  const adaptedRecommended = useMemo(() => {
    return recommended.map(p => ({
      productId: p.id,
      productName: p.name,
      productPrice: p.price,
      imageUrl: p.imageUrl,
    }));
  }, [recommended]);

  const getPageTitle = useCallback(() => {
    if (pageTitleOverride) return pageTitleOverride;
    if (forcedCategory !== undefined) return `Категория: ${forcedCategory}`;
    if (filters.query) return `Поиск: "${filters.query}"`;
    if (filters.category) return `Категория: ${filters.category}`;
    return 'Все товары';
  }, [pageTitleOverride, forcedCategory, filters.query, filters.category]);

  const getEmptyStateMessage = useCallback(() => {
    if (forcedCategory !== undefined) {
      return 'В этой категории нет товаров по выбранным фильтрам';
    }
    if (filters.query && !filters.category && !filters.minPrice && !filters.maxPrice && !specFilters) {
      return `По запросу «${filters.query}» ничего не найдено`;
    }
    return 'По выбранным фильтрам ничего не найдено';
  }, [forcedCategory, filters.query, filters.category, filters.minPrice, filters.maxPrice, specFilters]);

  /** Те же критерии, что и в эффекте поиска — для корректного empty state. */
  const hasSearchCriteria = useMemo(
    () =>
      Boolean(
        debouncedFilters.query?.trim() ||
          debouncedFilters.category ||
          debouncedFilters.minPrice ||
          debouncedFilters.maxPrice ||
          debouncedFilters.inStock != null ||
          (debouncedSpecFilters && Object.keys(debouncedSpecFilters).length > 0)
      ),
    [debouncedFilters, debouncedSpecFilters]
  );

  const showMainEmptyState = !loading && !error && hasSearchCriteria && adaptedProducts.length === 0;
  const showIdleHint = !loading && !error && !hasSearchCriteria;

  return (
    <div className="search-results-page">
      <div className="search-results-header">
        <h2>{getPageTitle()}</h2>
      </div>

      <ActiveFiltersBar
        filters={filters}
        specFilters={specFilters}
        specDisplayNames={specDisplayNames}
        onRemoveBasic={handleRemoveBasicFilter}
        onRemoveSpec={handleRemoveSpecFilter}
        onClearAll={handleClearAllFilters}
      />

      <div className="search-results-layout">
        <FiltersPanel
          filters={filters}
          specFilters={specFilters}
          onBasicFilterChange={handleBasicFilterChange}
          onSpecFilterChange={handleSpecFilterChange}
          onClearSpecFilters={handleClearSpecFilters}
          apiBaseUrl={API_BASE_URL}
          isCategoryForced={forcedCategory !== undefined}
          onSpecsLoaded={handleSpecsLoaded}
        />

        <main className="search-results-main">
          <div className="sort-menu__container">
            <SortMenu
              currentSortOption={getCurrentSortOptionForMenu()}
              onSortOptionChange={handleSortMenuChange}
              className="search-results__sort"
            />
          </div>

          {loading && results.length === 0 && (
            <div className="loading-container">
              <LoadingSpinner />
            </div>
          )}

          {error && (
            <div className="error-message">
              <span>⚠️</span>
              <p>{error}</p>
              <button onClick={() => window.location.reload()}>Повторить</button>
            </div>
          )}

          {showIdleHint && (
            <div className="empty-state empty-state--idle">
              <p>Введите запрос в поиске или выберите фильтры слева</p>
            </div>
          )}

          {showMainEmptyState && (
            <div className="empty-state">
              <p>{getEmptyStateMessage()}</p>
              {adaptedRecommended.length > 0 && (
                <p className="empty-state__hint">Ниже — похожие товары, которые могут вам подойти</p>
              )}
              <button
                type="button"
                onClick={handleClearAllFilters}
                className="clear-filters-button"
              >
                Сбросить фильтры
              </button>
            </div>
          )}

          {/* Основная выдача */}
          {!loading && !error && adaptedProducts.length > 0 && (
            <>
              <div className="results-header">
                <p className="results-count">
                  Показано товаров: <strong>{adaptedProducts.length}</strong>
                </p>
              </div>
              
              {/* 🔧 FIX: Используем мемоизированный компонент сетки */}
              <ProductsGrid products={adaptedProducts} />
              
              {/* Кнопка "Показать еще" */}
              {hasMore && (
                <div className="load-more-container">
                  <button 
                    onClick={handleLoadMore} 
                    className="load-more-btn"
                    disabled={loading}
                  >
                    {loading ? 'Загрузка...' : 'Показать еще'}
                  </button>
                </div>
              )}
            </>
          )}

          {/* 🔧 FIX: Блок рекомендаций теперь мемоизирован и не перерендеривается */}
          {!loading && !error && adaptedRecommended.length > 0 && (
            <RecommendationsSection products={adaptedRecommended} />
          )}
        </main>
      </div>
    </div>
  );
});

SearchResultsPage.displayName = 'SearchResultsPage';

export default SearchResultsPage;