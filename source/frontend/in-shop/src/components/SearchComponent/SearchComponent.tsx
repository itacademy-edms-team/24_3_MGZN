// src/components/SearchComponent/SearchComponent.tsx
import React, { useState, useEffect, useRef, useCallback, useMemo } from 'react';
import { useNavigate, useSearchParams, useLocation } from 'react-router-dom';
import MiniProductCard from '../MiniProductCard/MiniProductCard.tsx';
import './SearchComponent.css';

interface ApiProductDto {
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

const SearchComponent: React.FC = () => {
  const [searchParams, setSearchParams] = useSearchParams();
  const location = useLocation();
  const navigate = useNavigate();
  
  const [query, setQuery] = useState<string>('');
  const [loading] = useState<boolean>(false);
  const [error] = useState<string | null>(null);
  const [showPreview, setShowPreview] = useState<boolean>(false);
  const [searchHistory, setSearchHistory] = useState<string[]>([]);
  const [randomSuggestions, setRandomSuggestions] = useState<ProductSearchResultDto[]>([]);

  const inputRef = useRef<HTMLInputElement>(null);
  const previewRef = useRef<HTMLDivElement>(null);
  const closeTimeoutRef = useRef<NodeJS.Timeout>();
  const isMountedRef = useRef(true);
  const isNavigatingRef = useRef(false);
  const isSyncingFromUrlRef = useRef(false);

  const API_BASE_URL = process.env.REACT_APP_API_BASE_URL || 'https://localhost:7275/api';

  const urlQuery = useMemo(() => searchParams.get('q') || '', [searchParams]);

  useEffect(() => {
    if (isSyncingFromUrlRef.current) {
      isSyncingFromUrlRef.current = false;
      return;
    }
    
    if (urlQuery && query !== urlQuery) {
      setQuery(urlQuery);
    }
  }, [urlQuery]);

  useEffect(() => {
    const savedHistory = localStorage.getItem('searchHistory');
    if (savedHistory) {
      try {
        const parsedHistory = JSON.parse(savedHistory);
        if (Array.isArray(parsedHistory)) {
          setSearchHistory(parsedHistory);
        }
      } catch (e) {
        console.error('Ошибка при чтении истории поиска:', e);
        setSearchHistory([]);
      }
    }
    
    return () => {
      isMountedRef.current = false;
    };
  }, []);

  useEffect(() => {
    const fetchRandomSuggestions = async () => {
      try {
        const response = await fetch(`${API_BASE_URL}/Products/random-products`, {
          method: 'GET',
          headers: { 'Content-Type': 'application/json' },
        });

        if (!response.ok) throw new Error(`Ошибка: ${response.status}`);
        if (response.status === 204) {
          setRandomSuggestions([]);
          return;
        }

        const rawData: ApiProductDto[] = await response.json();
        const convertedData: ProductSearchResultDto[] = rawData.map(item => ({
          id: item.productId,
          name: item.productName,
          price: item.productPrice,
          category: item.productCategoryName || '',
          description: item.productDescription || '',
          stockQuantity: item.productStockQuantity,
          isAvailable: item.productAvailability,
          imageUrl: item.imageUrl || '',
        }));

        if (isMountedRef.current) {
          setRandomSuggestions(convertedData);
        }
      } catch (err) {
        console.error('Ошибка загрузки подборки:', err);
        if (isMountedRef.current) {
          setRandomSuggestions([]);
        }
      }
    };

    fetchRandomSuggestions();
  }, [API_BASE_URL]);

  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      const isClickInsidePreview = previewRef.current?.contains(event.target as Node);
      const isClickOnInput = inputRef.current?.contains(event.target as Node);
      if (!isClickOnInput && !isClickInsidePreview && showPreview) {
        setShowPreview(false);
      }
    };
    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, [showPreview]);

  useEffect(() => {
    return () => {
      if (closeTimeoutRef.current) clearTimeout(closeTimeoutRef.current);
    };
  }, []);

  const safelyClosePreview = useCallback(() => {
    if (closeTimeoutRef.current) clearTimeout(closeTimeoutRef.current);
    closeTimeoutRef.current = setTimeout(() => {
      setShowPreview(false);
      inputRef.current?.blur();
    }, 100);
  }, []);

  const handleFocus = useCallback(() => setShowPreview(true), []);
  
  const handleBlur = useCallback((e: React.FocusEvent<HTMLInputElement>) => {
    if (previewRef.current?.contains(e.relatedTarget as Node)) return;
    setShowPreview(false);
  }, []);

  const handleChange = useCallback((e: React.ChangeEvent<HTMLInputElement>) => {
    setQuery(e.target.value);
  }, []);

  const updateSearchHistory = useCallback((newQuery: string) => {
    setSearchHistory(prev => {
      const updatedHistory = [
        newQuery,
        ...prev.filter(item => item.toLowerCase() !== newQuery.toLowerCase())
      ].slice(0, 10);
      
      try {
        localStorage.setItem('searchHistory', JSON.stringify(updatedHistory));
      } catch (e) {
        console.error('Ошибка сохранения истории:', e);
      }
      
      return updatedHistory;
    });
  }, []);

  const performSearchAndNavigate = useCallback((searchQuery: string) => {
    if (isNavigatingRef.current) return;
    
    isNavigatingRef.current = true;
    updateSearchHistory(searchQuery);
    
    const params = new URLSearchParams();
    params.set('q', searchQuery);
    
    const currentCategory = searchParams.get('category');
    if (currentCategory) {
      params.set('category', currentCategory);
    }
    
    isSyncingFromUrlRef.current = true;
    setSearchParams(params, { replace: true });
    
    navigate(`/search?q=${encodeURIComponent(searchQuery)}`, {
      replace: location.pathname === '/search',
    });
    
    setTimeout(() => {
      isNavigatingRef.current = false;
    }, 150);
  }, [location.pathname, navigate, setSearchParams, updateSearchHistory, searchParams]);

  const handleKeyDown = useCallback((e: React.KeyboardEvent<HTMLInputElement>) => {
    if (e.key === 'Enter' && query.trim() && !isNavigatingRef.current) {
      e.preventDefault();
      performSearchAndNavigate(query.trim());
      safelyClosePreview();
    }
  }, [query, safelyClosePreview, performSearchAndNavigate]);

  const handleSearchClick = useCallback(() => {
    if (query.trim() && !isNavigatingRef.current) {
      performSearchAndNavigate(query.trim());
      safelyClosePreview();
    }
  }, [query, safelyClosePreview, performSearchAndNavigate]);

  const handleHistoryItemClick = useCallback((historyQuery: string) => {
    if (isNavigatingRef.current) return;
    
    setQuery(historyQuery);
    updateSearchHistory(historyQuery);
    
    const params = new URLSearchParams();
    params.set('q', historyQuery);
    
    const currentCategory = searchParams.get('category');
    if (currentCategory) {
      params.set('category', currentCategory);
    }
    
    isSyncingFromUrlRef.current = true;
    setSearchParams(params, { replace: true });
    
    navigate(`/search?q=${encodeURIComponent(historyQuery)}`, { replace: true });
    safelyClosePreview();
  }, [navigate, setSearchParams, safelyClosePreview, updateSearchHistory, searchParams]);

  const handleProductClick = useCallback((productId: number) => {
    if (isNavigatingRef.current) return;
    navigate(`/product/${encodeURIComponent(productId)}`);
    safelyClosePreview();
  }, [navigate, safelyClosePreview]);

  const handleMouseDownOnPreview = useCallback((e: React.MouseEvent) => {
    e.preventDefault();
  }, []);

  const clearHistory = useCallback(() => {
    setSearchHistory([]);
    try {
      localStorage.setItem('searchHistory', JSON.stringify([]));
    } catch (e) {
      console.error('Ошибка сохранения истории:', e);
    }
  }, []);

  const removeFromHistory = useCallback((itemToRemove: string) => {
    setSearchHistory(prev => {
      const updatedHistory = prev.filter(item => item.toLowerCase() !== itemToRemove.toLowerCase());
      try {
        localStorage.setItem('searchHistory', JSON.stringify(updatedHistory));
      } catch (e) {
        console.error('Ошибка сохранения истории:', e);
      }
      return updatedHistory;
    });
  }, []);

  // 🔧 НОВАЯ ИКОНКА: Font Awesome Search
  const SearchIcon = useMemo(() => (
    <svg 
      xmlns="http://www.w3.org/2000/svg" 
      viewBox="0 0 512 512" 
      className="search-icon-svg"
      aria-hidden="true"
    >
      <path 
        fill="currentColor" 
        d="M416 208c0 45.9-14.9 88.3-40 122.7L502.6 457.4c12.5 12.5 12.5 32.8 0 45.3s-32.8 12.5-45.3 0L330.7 376c-34.4 25.2-76.8 40-122.7 40C93.1 416 0 322.9 0 208S93.1 0 208 0S416 93.1 416 208zM208 352a144 144 0 1 0 0-288 144 144 0 1 0 0 288z"
      />
    </svg>
  ), []);

  const CloseIcon = useMemo(() => (
    <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor" width="16px" height="16px" aria-hidden="true" className="close-icon-svg">
      <path d="M19 6.41L17.59 5 12 10.59 6.41 5 5 6.41 10.59 12 5 17.59 6.41 19 12 13.41 17.59 19 19 17.59 13.41 12z"/>
    </svg>
  ), []);

  const displayData = useMemo(() => {
    const displayCount = searchHistory.length > 0 ? 6 : 12;
    const gridColumns = searchHistory.length > 0 ? 'repeat(2, 1fr)' : 'repeat(4, 1fr)';
    const filteredRandomSuggestions = randomSuggestions.slice(0, displayCount);
    
    return { displayCount, gridColumns, filteredRandomSuggestions };
  }, [searchHistory.length, randomSuggestions]);

  return (
    <div className="search-component-wrapper">
      <div className="search-input-container">
        <input
          ref={inputRef}
          type="text"
          value={query}
          onChange={handleChange}
          onFocus={handleFocus}
          onBlur={handleBlur}
          onKeyDown={handleKeyDown}
          placeholder="Поиск товаров..."
          className="search-input"
        />
        <button onClick={handleSearchClick} className="search-button" aria-label="Выполнить поиск">
          {SearchIcon}
        </button>
      </div>

      {showPreview && (
        <div ref={previewRef} className="search-preview-dropdown" onMouseDown={handleMouseDownOnPreview}>
          {loading && <div className="loading-indicator">Поиск...</div>}
          {error && <div className="error-message">{error}</div>}

          {!loading && !error && searchHistory.length > 0 && (
            <div className="search-history-section">
              <div className="search-history-header">
                <h4>История поиска:</h4>
                <button type="button" className="clear-history-button" onClick={clearHistory} aria-label="Очистить историю">
                  Очистить всё
                </button>
              </div>
              <ul className="search-history-list">
                {searchHistory.map((item, index) => (
                  <li key={`${index}-${item}`} className="search-history-item">
                    <span onClick={() => handleHistoryItemClick(item)}>{item}</span>
                    <button
                      type="button"
                      className="remove-history-item-button"
                      onClick={(e) => {
                        e.stopPropagation();
                        removeFromHistory(item);
                      }}
                      aria-label={`Удалить "${item}" из истории`}
                    >
                      {CloseIcon}
                    </button>
                  </li>
                ))}
              </ul>
            </div>
          )}

          {!loading && !error && displayData.filteredRandomSuggestions.length > 0 && (
            <div className="random-suggestions-section">
              <h4>Вам может понравиться:</h4>
              <div className="suggestions-grid" style={{ '--grid-template-columns': displayData.gridColumns } as React.CSSProperties}>
                {displayData.filteredRandomSuggestions.map((product) => (
                  <MiniProductCard
                    key={`random-${product.id}`}
                    product={product}
                    onClick={(e) => {
                      e.preventDefault();
                      e.stopPropagation();
                      handleProductClick(product.id);
                    }}
                  />
                ))}
              </div>
            </div>
          )}
        </div>
      )}
    </div>
  );
};

export default SearchComponent;