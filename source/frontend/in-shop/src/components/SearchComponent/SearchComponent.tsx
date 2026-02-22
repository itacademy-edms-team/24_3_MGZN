import React, { useState, useEffect, useRef } from 'react';
import { useNavigate } from 'react-router-dom';
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
  const [query, setQuery] = useState<string>('');
  const [loading] = useState<boolean>(false);
  const [error] = useState<string | null>(null);
  const [showPreview, setShowPreview] = useState<boolean>(false);
  const [searchHistory, setSearchHistory] = useState<string[]>([]);
  const [randomSuggestions, setRandomSuggestions] = useState<ProductSearchResultDto[]>([]);

  const navigate = useNavigate();
  const inputRef = useRef<HTMLInputElement>(null);
  const previewRef = useRef<HTMLDivElement>(null);
  const closeTimeoutRef = useRef<NodeJS.Timeout>();

  const API_BASE_URL = process.env.REACT_APP_API_BASE_URL || 'https://localhost:7275/api';

  // Загрузка истории поиска из localStorage
  useEffect(() => {
    const savedHistory = localStorage.getItem('searchHistory');
    if (savedHistory) {
      try {
        const parsedHistory = JSON.parse(savedHistory);
        if (Array.isArray(parsedHistory)) {
          setSearchHistory(parsedHistory);
        }
      } catch (e) {
        console.error('Ошибка при чтении истории поиска из localStorage:', e);
        setSearchHistory([]);
      }
    }
  }, []);

  // Загрузка случайных предложений
  useEffect(() => {
    const fetchRandomSuggestions = async () => {
      try {
        const response = await fetch(`${API_BASE_URL}/Products/random-products`, {
          method: 'GET',
          headers: {
            'Content-Type': 'application/json',
          },
        });

        if (!response.ok) {
          throw new Error(`Ошибка загрузки подборки: ${response.status} ${response.statusText}`);
        }

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

        setRandomSuggestions(convertedData);
      } catch (err) {
        console.error('Ошибка при загрузке случайной подборки:', err);
        setRandomSuggestions([]);
      }
    };

    fetchRandomSuggestions();
  }, [API_BASE_URL]);

  // Закрытие при клике вне компонента
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      const isClickInsidePreview = previewRef.current?.contains(event.target as Node);
      const isClickOnInput = inputRef.current?.contains(event.target as Node);

      if (!isClickOnInput && !isClickInsidePreview && showPreview) {
        setShowPreview(false);
      }
    };

    document.addEventListener('mousedown', handleClickOutside);
    return () => {
      document.removeEventListener('mousedown', handleClickOutside);
    };
  }, [showPreview]);

  // Очистка таймаута при размонтировании
  useEffect(() => {
    return () => {
      if (closeTimeoutRef.current) {
        clearTimeout(closeTimeoutRef.current);
      }
    };
  }, []);

  // Безопасное закрытие с задержкой для завершения обработчиков
  const safelyClosePreview = () => {
    if (closeTimeoutRef.current) {
      clearTimeout(closeTimeoutRef.current);
    }
    closeTimeoutRef.current = setTimeout(() => {
      setShowPreview(false);
      if (inputRef.current) {
        inputRef.current.blur();
      }
    }, 100);
  };

  const handleFocus = () => {
    setShowPreview(true);
  };

  const handleBlur = (e: React.FocusEvent<HTMLInputElement>) => {
    if (previewRef.current?.contains(e.relatedTarget as Node)) {
      return;
    }
    setShowPreview(false);
  };

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setQuery(e.target.value);
  };

  const handleKeyDown = (e: React.KeyboardEvent<HTMLInputElement>) => {
    if (e.key === 'Enter') {
      e.preventDefault();
      if (query.trim()) {
        performSearchAndNavigate(query.trim());
        safelyClosePreview();
      }
    }
  };

  const handleSearchClick = () => {
    if (query.trim()) {
      performSearchAndNavigate(query.trim());
      safelyClosePreview();
    }
  };

  const handleHistoryItemClick = (historyQuery: string) => {
    setQuery(historyQuery);
    updateSearchHistory(historyQuery);
    navigate(`/search?q=${encodeURIComponent(historyQuery)}`);
    safelyClosePreview();
  };


  const handleProductClick = (productId: number) => {
    navigate(`/product/${encodeURIComponent(productId)}`);
    safelyClosePreview();
  };

  // Предотвращает потерю фокуса инпутом при клике на preview
  const handleMouseDownOnPreview = (e: React.MouseEvent) => {
    e.preventDefault();
  };

  const performSearchAndNavigate = async (searchQuery: string) => {
    updateSearchHistory(searchQuery);
    navigate(`/search?q=${encodeURIComponent(searchQuery)}`);
  };

  const updateSearchHistory = (newQuery: string) => {
    const updatedHistory = [newQuery, ...searchHistory.filter(item => item.toLowerCase() !== newQuery.toLowerCase())];
    const limitedHistory = updatedHistory.slice(0, 10);
    setSearchHistory(limitedHistory);
    saveHistoryToLocalStorage(limitedHistory);
  };

  const saveHistoryToLocalStorage = (newHistory: string[]) => {
    try {
      localStorage.setItem('searchHistory', JSON.stringify(newHistory));
    } catch (e) {
      console.error('Ошибка при сохранении истории поиска в localStorage:', e);
    }
  };

  const clearHistory = () => {
    setSearchHistory([]);
    saveHistoryToLocalStorage([]);
  };

  const removeFromHistory = (itemToRemove: string) => {
    const updatedHistory = searchHistory.filter(item => item.toLowerCase() !== itemToRemove.toLowerCase());
    setSearchHistory(updatedHistory);
    saveHistoryToLocalStorage(updatedHistory);
  };

  const SearchIcon = () => (
    <svg
      xmlns="http://www.w3.org/2000/svg"
      viewBox="0 0 24 24"
      fill="#6c757d"
      width="20px"
      height="20px"
      aria-hidden="true"
    >
      <path d="M15.5 14h-.79l-.28-.27C15.41 12.59 16 11.11 16 9.5 16 5.91 13.09 3 9.5 3S3 5.91 3 9.5 5.91 16 9.5 16c1.61 0 3.09-.59 4.23-1.57l.27.28v.79l5 4.99L20.49 19l-4.99-5zm-6 0C7.01 14 5 11.99 5 9.5S7.01 5 9.5 5 14 7.01 14 9.5 11.99 14 9.5 14z"/>
    </svg>
  );

  const CloseIcon = () => (
    <svg
      xmlns="http://www.w3.org/2000/svg"
      viewBox="0 0 24 24"
      fill="#6c757d"
      width="16px"
      height="16px"
      aria-hidden="true"
      className="close-icon-svg"
    >
      <path d="M19 6.41L17.59 5 12 10.59 6.41 5 5 6.41 10.59 12 5 17.59 6.41 19 12 13.41 17.59 19 19 17.59 13.41 12z"/>
    </svg>
  );

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
        <button
          onClick={handleSearchClick}
          className="search-button"
          aria-label="Выполнить поиск"
        >
          <SearchIcon />
        </button>
      </div>

      {showPreview && (
        <div 
          ref={previewRef} 
          className="search-preview-dropdown"
          onMouseDown={handleMouseDownOnPreview}
        >
          {loading && <div className="loading-indicator">Поиск...</div>}
          {error && <div className="error-message">{error}</div>}

          {!loading && !error && searchHistory.length > 0 && (
            <div className="search-history-section">
              <div className="search-history-header">
                <h4>История поиска:</h4>
                <button
                  type="button"
                  className="clear-history-button"
                  onClick={clearHistory}
                  aria-label="Очистить историю поиска"
                >
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
                      <CloseIcon />
                    </button>
                  </li>
                ))}
              </ul>
            </div>
          )}

          {!loading && !error && (
            <>
              {(() => {
                const displayCount = searchHistory.length > 0 ? 6 : 12;
                const gridColumns = searchHistory.length > 0 ? 'repeat(2, 1fr)' : 'repeat(4, 1fr)';
                const filteredRandomSuggestions = randomSuggestions.slice(0, displayCount);

                if (filteredRandomSuggestions.length === 0) {
                  return null;
                }

                return (
                  <div className="random-suggestions-section">
                    <h4>Вам может понравиться:</h4>
                    <div 
                      className="suggestions-grid" 
                      style={{ '--grid-template-columns': gridColumns } as React.CSSProperties}
                    >
                      {filteredRandomSuggestions.map((product) => (
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
                );
              })()}
            </>
          )}
        </div>
      )}
    </div>
  );
};

export default SearchComponent;