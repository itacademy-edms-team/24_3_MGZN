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
import './SearchResultsPage.css';

interface SearchResultsPageProps {
  forcedCategory?: string;
  hideSearchQuery?: boolean;
  pageTitleOverride?: string;
}

type SpecFilterValue = string | number | { Min?: number; Max?: number } | null;

const DEFAULT_LIMIT = 50;
const DEBOUNCE_DELAY = 400;
const API_BASE_URL = process.env.REACT_APP_API_BASE_URL || 'https://localhost:7275/api';

const SearchResultsPage = memo<SearchResultsPageProps>(({
  forcedCategory,
  hideSearchQuery = false,
  pageTitleOverride,
}) => {
  const [searchParams, setSearchParams] = useSearchParams();
  const { results, loading, error, search, clear } = useProductSearch(API_BASE_URL);
  
  const isInitialMount = useRef(true);
  const prevForcedCategory = useRef(forcedCategory);
  const isUpdatingFromUrl = useRef(false);
  const lastSearchKeyRef = useRef<string>('');
  const lastAppliedSpecsParamRef = useRef<string | null>(null);
  const prevUrlQueryRef = useRef<string>('');

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
  
  const [sort, setSort] = useState<{ option: string; order: 'asc' | 'desc' }>(() => ({
    option: searchParams.get('sort') || 'relevance',
    order: (searchParams.get('order') as 'asc' | 'desc') || 'desc',
  }));

  const debouncedFilters = useDebounce(filters, DEBOUNCE_DELAY);
  const debouncedSort = useDebounce(sort, DEBOUNCE_DELAY);
  const debouncedSpecFilters = useDebounce(specFilters, DEBOUNCE_DELAY);

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
        clear();
      }
    }
  }, [forcedCategory, filters.category, clear]);

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

  // 🔧 FIX: Добавлен results.length в зависимости для соответствия правилам eslint
  useEffect(() => {
    const hasSearchCriteria =
      debouncedFilters.query?.trim() ||
      debouncedFilters.category ||
      debouncedFilters.minPrice ||
      debouncedFilters.maxPrice ||
      debouncedFilters.inStock != null ||
      (debouncedSpecFilters && Object.keys(debouncedSpecFilters).length > 0);

    if (!hasSearchCriteria) {
      if (results.length > 0) {
        clear();
      }
      lastSearchKeyRef.current = '';
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
      limit: DEFAULT_LIMIT,
      category: debouncedFilters.category || null,
      minPrice: minPrice,
      maxPrice: maxPrice,
      inStock: debouncedFilters.inStock,
      specFilters: debouncedSpecFilters,
      sortBy: debouncedSort.option,
      sortOrder: debouncedSort.order,
    };

    search(request);
  }, [debouncedFilters, debouncedSort, debouncedSpecFilters, search, clear, results.length]);

  const handleBasicFilterChange = useCallback((changes: Partial<FiltersState>) => {
    setFilters(prev => ({ ...prev, ...changes }));
  }, []);

  const handleSpecFilterChange = useCallback((specName: string, value: SpecFilterValue) => {
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
    setSpecFiltersState(null);
  }, []);

  const handleRemoveBasicFilter = useCallback((key: keyof FiltersState) => {
    if (key === 'category' && forcedCategory !== undefined) return;
    setFilters(prev => ({ ...prev, [key]: key === 'inStock' ? null : '' }));
  }, [forcedCategory]);

  const handleRemoveSpecFilter = useCallback((specName: string) => {
    setSpecFiltersState(prev => {
      if (!prev) return null;
      const next = { ...prev };
      delete next[specName];
      return Object.keys(next).length > 0 ? next : null;
    });
  }, []);

  const handleClearAllFilters = useCallback(() => {
    setFilters({
      query: '',
      minPrice: '',
      maxPrice: '',
      category: forcedCategory !== undefined ? forcedCategory : '',
      inStock: null,
    });
    setSpecFiltersState(null);
    setSort({ option: 'relevance', order: 'desc' });

    const params = new URLSearchParams();
    if (forcedCategory !== undefined) params.set('category', forcedCategory);
    setSearchParams(params, { replace: true });
    clear();
    lastSearchKeyRef.current = '';
  }, [forcedCategory, setSearchParams, clear]);

  const handleSortMenuChange = useCallback((newSortOption: SortOption) => {
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

  const adaptedProducts = useMemo(() => {
    return results.map(p => ({
      productId: p.id,
      productName: p.name,
      productPrice: p.price,
      imageUrl: p.imageUrl,
    }));
  }, [results]);

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
      return `По запросу "${filters.query}" ничего не найдено`;
    }
    return 'По выбранным фильтрам ничего не найдено';
  }, [forcedCategory, filters.query, filters.category, filters.minPrice, filters.maxPrice, specFilters]);

  const activeFiltersCount = useMemo(() => {
    let count = 0;
    if (filters.query) count++;
    if (filters.minPrice) count++;
    if (filters.maxPrice) count++;
    if (filters.category && forcedCategory === undefined) count++;
    if (filters.inStock !== null) count++;
    if (specFilters) count += Object.keys(specFilters).length;
    return count;
  }, [filters, specFilters, forcedCategory]);

  return (
    <div className="search-results-page">
      <div className="search-results-header">
        <h2>{getPageTitle()}</h2>
      </div>

      <ActiveFiltersBar
        filters={filters}
        specFilters={specFilters}
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
        />

        <main className="search-results-main">
          <div className="sort-menu__container">
            <SortMenu
              currentSortOption={getCurrentSortOptionForMenu()}
              onSortOptionChange={handleSortMenuChange}
              className="search-results__sort"
            />
          </div>

          {loading && (
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

          {!loading && !error && adaptedProducts.length === 0 && (
            <div className="empty-state">
              <p>{getEmptyStateMessage()}</p>
              <button 
                onClick={handleClearAllFilters}
                className="clear-filters-button"
              >
                Сбросить фильтры
              </button>
            </div>
          )}

          {!loading && !error && adaptedProducts.length > 0 && (
            <>
              <div className="results-header">
                <p className="results-count">
                  Найдено товаров: <strong>{adaptedProducts.length}</strong>
                </p>
                {adaptedProducts.length === DEFAULT_LIMIT && (
                  <p className="results-limit-warning">
                    Показаны первые {DEFAULT_LIMIT} результатов
                  </p>
                )}
              </div>
              <div className="products-grid">
                {adaptedProducts.map(product => (
                  <ProductCard 
                    key={product.productId} 
                    product={product}
                  />
                ))}
              </div>
            </>
          )}
        </main>
      </div>
    </div>
  );
});

SearchResultsPage.displayName = 'SearchResultsPage';

export default SearchResultsPage;