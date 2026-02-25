// src/components/FiltersPanel/FiltersPanel.tsx
import React, { useState, useEffect } from 'react';
import './FiltersPanel.css';

interface FiltersPanelProps {
  initialMinPrice?: string;
  initialMaxPrice?: string;
  initialCategory?: string;
  onFiltersChange: (filters: { minPrice: string; maxPrice: string; category: string }) => void;
  hideCategory?: boolean;
}

// Интерфейс должен соответствовать тому, что приходит с сервера
interface CategoryDto {
  id: number;
  name: string;
  // Добавьте другие поля, если они есть
}

const FiltersPanel: React.FC<FiltersPanelProps> = ({
  initialMinPrice = '',
  initialMaxPrice = '',
  initialCategory = '',
  onFiltersChange,
  hideCategory = false,
}) => {
  const [minPrice, setMinPrice] = useState(initialMinPrice);
  const [maxPrice, setMaxPrice] = useState(initialMaxPrice);
  const [category, setCategory] = useState(initialCategory);
  const [categories, setCategories] = useState<any[]>([]); // Временно используем any для отладки
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const API_BASE_URL = process.env.REACT_APP_API_BASE_URL || 'https://localhost:7275/api';

  // Загрузка категорий при монтировании компонента
  useEffect(() => {
    const fetchCategories = async () => {
      if (hideCategory) return;
      
      setLoading(true);
      setError(null);
      
      try {
        console.log('Запрос категорий...');
        const response = await fetch(`${API_BASE_URL}/Category`, {
          method: 'GET',
          headers: {
            'Content-Type': 'application/json',
          },
        });

        console.log('Статус ответа:', response.status);

        if (!response.ok) {
          throw new Error(`Ошибка загрузки категорий: ${response.status}`);
        }

        const data = await response.json();
        console.log('Полученные данные (сырые):', data);
        
        // Обрабатываем данные в зависимости от их структуры
        let processedCategories = [];
        
        if (Array.isArray(data)) {
          processedCategories = data;
          console.log('Это массив, длина:', data.length);
          if (data.length > 0) {
            console.log('Первый элемент массива:', data[0]);
            console.log('Тип первого элемента:', typeof data[0]);
            console.log('Ключи первого элемента:', Object.keys(data[0]));
          }
        } else if (data && typeof data === 'object') {
          console.log('Это объект, ключи:', Object.keys(data));
          
          // Проверяем разные возможные форматы
          if (data.$values && Array.isArray(data.$values)) {
            console.log('Найдены $values');
            processedCategories = data.$values;
          } else if (data.data && Array.isArray(data.data)) {
            console.log('Найдены data');
            processedCategories = data.data;
          } else if (data.categories && Array.isArray(data.categories)) {
            console.log('Найдены categories');
            processedCategories = data.categories;
          } else if (data.items && Array.isArray(data.items)) {
            console.log('Найдены items');
            processedCategories = data.items;
          } else {
            // Если это одиночный объект, преобразуем в массив
            processedCategories = [data];
          }
        }
        
        console.log('Обработанные категории:', processedCategories);
        setCategories(processedCategories);
        
        if (processedCategories.length === 0) {
          setError('Нет доступных категорий');
        }
        
      } catch (err) {
        console.error('Ошибка при загрузке категорий:', err);
        setError(err instanceof Error ? err.message : 'Не удалось загрузить категории');
        setCategories([]);
      } finally {
        setLoading(false);
      }
    };

    fetchCategories();
  }, [API_BASE_URL, hideCategory]);

  // Обновление локального состояния при изменении пропсов
  useEffect(() => {
    setMinPrice(initialMinPrice);
    setMaxPrice(initialMaxPrice);
    setCategory(initialCategory);
  }, [initialMinPrice, initialMaxPrice, initialCategory]);

  const handleMinPriceChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const value = e.target.value;
    setMinPrice(value);
    onFiltersChange({ minPrice: value, maxPrice, category });
  };

  const handleMaxPriceChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const value = e.target.value;
    setMaxPrice(value);
    onFiltersChange({ minPrice, maxPrice: value, category });
  };

  const handleCategoryChange = (e: React.ChangeEvent<HTMLSelectElement>) => {
    const value = e.target.value;
    setCategory(value);
    onFiltersChange({ minPrice, maxPrice, category: value });
  };

  // Функция для безопасного получения названия категории
  const getCategoryName = (cat: any): string => {
    if (!cat) return 'Без названия';
    
    // Проверяем различные возможные названия полей
    if (typeof cat === 'string') return cat;
    if (cat.name) return cat.name;
    if (cat.categoryName) return cat.categoryName;
    if (cat.title) return cat.title;
    if (cat.category) return cat.category;
    if (cat.value) return cat.value;
    if (cat.label) return cat.label;
    
    // Если ничего не нашли, возвращаем строковое представление
    return String(cat);
  };

  // Функция для безопасного получения ID категории
  const getCategoryId = (cat: any): number | string => {
    if (!cat) return Math.random();
    
    if (typeof cat === 'object') {
      if (cat.id !== undefined) return cat.id;
      if (cat.categoryId !== undefined) return cat.categoryId;
      if (cat.value !== undefined) return cat.value;
      if (cat.key !== undefined) return cat.key;
    }
    
    // Если это примитив, используем его как ID
    return typeof cat === 'string' || typeof cat === 'number' ? cat : Math.random();
  };

  console.log('Рендер компонента, категории:', categories);

  return (
    <div className="filters-panel">
      <h3>Фильтры</h3>
      
      <div className="filter-section">
        <h4>Цена</h4>
        <div className="price-inputs">
          <div className="price-input">
            <label htmlFor="min-price">От:</label>
            <input
              id="min-price"
              type="number"
              min="0"
              step="1000.0"
              value={minPrice}
              onChange={handleMinPriceChange}
              placeholder="0"
            />
          </div>
          <div className="price-input">
            <label htmlFor="max-price">До:</label>
            <input
              id="max-price"
              type="number"
              min="0"
              step="1000.0"
              value={maxPrice}
              onChange={handleMaxPriceChange}
              placeholder="10000"
            />
          </div>
        </div>
      </div>

      {!hideCategory && (
        <div className="filter-section">
          <h4>Категория</h4>
          {loading ? (
            <div className="filter-loading">Загрузка категорий...</div>
          ) : error ? (
            <div className="filter-error">{error}</div>
          ) : categories.length === 0 ? (
            <div className="filter-error">Нет доступных категорий</div>
          ) : (
            <>
              <select 
                value={category} 
                onChange={handleCategoryChange}
                className="category-select"
              >
                <option value="">Все категории</option>
                {categories.map((cat, index) => {
                  const catId = getCategoryId(cat);
                  const catName = getCategoryName(cat);
                  console.log(`Категория ${index}:`, { cat, catId, catName });
                  
                  return (
                    <option key={catId} value={catName}>
                      {catName}
                    </option>
                  );
                })}
              </select>
            </>
          )}
        </div>
      )}
    </div>
  );
};

export default FiltersPanel;