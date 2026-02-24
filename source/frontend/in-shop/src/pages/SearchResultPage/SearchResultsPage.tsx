import React, { useState, useEffect } from 'react';
import { useSearchParams } from 'react-router-dom';
import ProductCard from '../../components/ProductCard.jsx';
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
  const [searchParams] = useSearchParams();
  const query = searchParams.get('q') || '';

  const [results, setResults] = useState<ProductSearchResultDto[]>([]);
  const [loading, setLoading] = useState<boolean>(false);
  const [error, setError] = useState<string | null>(null);

  const API_BASE_URL = process.env.REACT_APP_API_BASE_URL || 'https://localhost:7275/api';

  useEffect(() => {
    const performSearch = async () => {
      if (!query.trim()) {
        setError('Не указан поисковый запрос.');
        setResults([]);
        return;
      }

      setLoading(true);
      setError(null);
      setResults([]);

      try {
        const searchParams = new URLSearchParams({
          q: query.trim(),
        });

        const response = await fetch(`${API_BASE_URL}/search/search?${searchParams}`, {
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
          setError('По вашему запросу ничего не найдено.');
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

  }, [query, API_BASE_URL]);

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
        Результаты поиска по запросу: "{query}"
        {!query && <span> (все товары)</span>}
      </h2>

      {loading && <LoadingSpinner />}
      {error && <div className="error-message">{error}</div>}

      {!loading && !error && results.length > 0 && (
        <div className="search-results-grid">
          <h3>Найдено {results.length} товаров:</h3>
          {results.map((apiProduct) => {
            const adaptedProduct = adaptApiResultToProductCardProps(apiProduct);
            return (
              <ProductCard key={apiProduct.id} product={adaptedProduct} />
            );
          })}
        </div>
      )}
    </div>
  );
};

export default SearchResultsPage;