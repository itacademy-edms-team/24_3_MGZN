import React, { FormEvent, useEffect, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import './CatalogPage.css';
import LoadingSpinner from '../components/LoadingSpinner';
import ProductCard from '../components/ProductCard.jsx';
import { apiClient } from '../api/client';
import { resolveAssetUrl } from '../config/api.js';
import { validateNumberRange } from '../utils/filters';

type CategoryItem = {
  categoryId: number;
  categoryName: string;
  imageURL?: string | null;
};

type PickProduct = {
  productId: number;
  productName: string;
  productPrice: number;
  imageUrl?: string | null;
  productCategoryId: number;
  productCategoryName: string;
  productStockQuantity?: number;
  productAvailability?: boolean;
  averageRating?: number;
  reviewsCount?: number;
};

const CatalogPage: React.FC = () => {
  const navigate = useNavigate();
  const [categories, setCategories] = useState<CategoryItem[]>([]);
  const [picks, setPicks] = useState<PickProduct[]>([]);
  const [loading, setLoading] = useState(true);
  const [picksLoading, setPicksLoading] = useState(true);
  const [query, setQuery] = useState('');
  const [minPrice, setMinPrice] = useState('');
  const [maxPrice, setMaxPrice] = useState('');
  const [inStock, setInStock] = useState(false);
  const [priceError, setPriceError] = useState<string | null>(null);

  useEffect(() => {
    apiClient
      .get('/Category')
      .then((response) => {
        setCategories(response.data ?? []);
        setLoading(false);
      })
      .catch((error) => {
        console.error('Ошибка загрузки категорий:', error);
        setLoading(false);
      });
  }, []);

  useEffect(() => {
    let cancelled = false;

    const loadPicks = () => {
      apiClient
        .get('/Products/picks-for-you')
        .then((response) => {
          if (!cancelled) {
            setPicks(response.data ?? []);
          }
        })
        .catch((error) => {
          console.error('Ошибка загрузки подборки:', error);
        })
        .finally(() => {
          if (!cancelled) {
            setPicksLoading(false);
          }
        });
    };

    loadPicks();
    const intervalId = window.setInterval(loadPicks, 60_000);

    return () => {
      cancelled = true;
      window.clearInterval(intervalId);
    };
  }, []);

  const handleSearch = (event: FormEvent) => {
    event.preventDefault();

    const range = validateNumberRange(minPrice || null, maxPrice || null);
    if (!range.valid) {
      setPriceError(range.error ?? 'Некорректный диапазон цены');
      return;
    }
    setPriceError(null);

    const params = new URLSearchParams();
    const trimmed = query.trim();
    if (trimmed) params.set('q', trimmed);
    if (minPrice) params.set('minPrice', minPrice);
    if (maxPrice) params.set('maxPrice', maxPrice);
    if (inStock) params.set('inStock', 'true');

    const qs = params.toString();
    navigate(qs ? `/search?${qs}` : '/search');
  };

  if (loading) {
    return <LoadingSpinner message="Загрузка каталога..." />;
  }

  return (
    <div className="catalog-page">
      <section className="catalog-hero" aria-label="Поиск по витрине">
        <div className="catalog-hero__inner">
          <p id="catalog-hero-brand" className="catalog-hero__brand">.InShop</p>
          <p className="catalog-hero__tagline">Каталог без суеты</p>

          <form className="catalog-hero__form" onSubmit={handleSearch}>
            <input
              type="search"
              className="catalog-hero__query"
              value={query}
              onChange={(e) => setQuery(e.target.value)}
              placeholder="Что ищете?"
              aria-label="Поисковый запрос"
              autoComplete="off"
            />

            <div className="catalog-hero__price" role="group" aria-label="Диапазон цены">
              <label className="catalog-hero__price-field">
                <span>От</span>
                <input
                  type="number"
                  inputMode="decimal"
                  min={0}
                  step="1"
                  value={minPrice}
                  onChange={(e) => {
                    setMinPrice(e.target.value);
                    setPriceError(null);
                  }}
                  placeholder="0"
                  aria-label="Минимальная цена"
                />
              </label>
              <label className="catalog-hero__price-field">
                <span>До</span>
                <input
                  type="number"
                  inputMode="decimal"
                  min={0}
                  step="1"
                  value={maxPrice}
                  onChange={(e) => {
                    setMaxPrice(e.target.value);
                    setPriceError(null);
                  }}
                  placeholder="∞"
                  aria-label="Максимальная цена"
                />
              </label>
            </div>

            <label className="catalog-hero__stock">
              <span>В наличии</span>
              <input
                type="checkbox"
                checked={inStock}
                onChange={(e) => setInStock(e.target.checked)}
                aria-label="Только в наличии"
              />
            </label>

            <button type="submit" className="catalog-hero__submit">
              Найти
            </button>
          </form>

          {priceError && (
            <p className="catalog-hero__error" role="alert">
              {priceError}
            </p>
          )}
        </div>
      </section>

      <section className="catalog-categories page-reveal-block" aria-labelledby="catalog-categories-title">
        <h2 id="catalog-categories-title" className="catalog-categories__title">
          Категории
        </h2>

        <div className="categories-container">
          {categories.length === 0 ? (
            <p className="catalog-categories__empty">Нет доступных категорий.</p>
          ) : (
            <ul className="categories-list page-reveal-stagger">
              {categories.map((category) => (
                <li key={category.categoryId} className="category-card">
                  <Link to={`/category/${encodeURIComponent(category.categoryName)}`}>
                    <img
                      src={resolveAssetUrl(category.imageURL) || '/placeholder-image.jpg'}
                      alt={category.categoryName}
                      onError={(e) => {
                        e.currentTarget.src = '/placeholder-image.jpg';
                      }}
                      loading="lazy"
                    />
                    <p>{category.categoryName}</p>
                  </Link>
                </li>
              ))}
            </ul>
          )}
        </div>
      </section>

      {!picksLoading && picks.length > 0 && (
        <section
          className="catalog-picks page-reveal-block"
          aria-labelledby="catalog-picks-title"
        >
          <h2 id="catalog-picks-title" className="catalog-picks__title">
            Подобрано для вас
          </h2>
          <p className="catalog-picks__subtitle">По одному товару из каждой категории</p>

          <ul className="catalog-picks__list page-reveal-stagger">
            {picks.map((product) => (
              <li key={product.productId} className="catalog-picks__item">
                <div className="catalog-picks__card">
                  <Link
                    to={`/category/${encodeURIComponent(product.productCategoryName)}`}
                    className="catalog-picks__category"
                    onClick={(e) => e.stopPropagation()}
                  >
                    {product.productCategoryName}
                  </Link>
                  <ProductCard product={product} />
                </div>
              </li>
            ))}
          </ul>
        </section>
      )}
    </div>
  );
};

export default CatalogPage;
