// src/components/FiltersPanel/FiltersPanel.tsx
import React, { useState, useEffect, useCallback, useMemo, useRef } from 'react';
import { SpecificationFilterDto, FiltersState } from '../../types/search.ts';
import { validateNumberRange } from '../../utils/filters.ts';
// 🔧 FIX: Импортируем новый хук для дебаунса примитивов
import { useDebouncedValue } from '../../hooks/useDebounce.ts';
import './FiltersPanel.css';

interface CategoryDto {
  categoryId: number;
  categoryName: string;
  imageURL?: string;
}

type SpecFilterValue = string | number | { Min?: number; Max?: number } | null;

// 🔧 FIX: Константа задержки для единообразия с основным поиском
const SPEC_FILTER_DEBOUNCE_DELAY = 400;

interface Props {
  filters: FiltersState;
  specFilters: Record<string, SpecFilterValue> | null;
  onBasicFilterChange: (changes: Partial<FiltersState>) => void;
  onSpecFilterChange: (specName: string, value: SpecFilterValue) => void;
  onClearSpecFilters: () => void;
  apiBaseUrl: string;
}

const FiltersPanel: React.FC<Props> = ({
  filters,
  specFilters,
  onBasicFilterChange,
  onSpecFilterChange,
  onClearSpecFilters,
  apiBaseUrl,
}) => {
  const [specErrors, setSpecErrors] = useState<Record<string, string>>({});
  const [categories, setCategories] = useState<CategoryDto[]>([]);
  const [availableSpecs, setAvailableSpecs] = useState<SpecificationFilterDto[]>([]);
  const [loadingSpecs, setLoadingSpecs] = useState(false);
  const prevCategoryRef = useRef<string | null>(null);
  const isUpdatingSpecsRef = useRef(false);

  // 🔧 FIX: Локальный стейт для мгновенного отображения ввода в числовых полях
  const [localNumberSpecs, setLocalNumberSpecs] = useState<Record<string, { Min?: string; Max?: string }>>({});

  // 🔧 FIX: Дебаунс-версия локального стейта — именно это значение отправляется в глобальный стейт
  const debouncedLocalNumberSpecs = useDebouncedValue(
    JSON.stringify(localNumberSpecs),
    SPEC_FILTER_DEBOUNCE_DELAY
  );

  // Загрузка категорий
  useEffect(() => {
    const fetchCategories = async () => {
      try {
        const res = await fetch(`${apiBaseUrl}/Category`);
        if (res.ok) {
          const data = await res.json();
          const normalized = Array.isArray(data)
            ? data.map((cat: any) => ({
                categoryId: cat?.categoryId ?? cat?.id ?? Math.random(),
                categoryName: cat?.categoryName ?? cat?.name ?? String(cat),
              }))
            : [];
          setCategories(normalized);
        }
      } catch (e) {
        console.error('Ошибка загрузки категорий:', e);
      }
    };
    fetchCategories();
  }, [apiBaseUrl]);

  // Загрузка спецификаций при смене категории
  useEffect(() => {
    const currentCategory = filters.category;
    const categoryChanged = prevCategoryRef.current !== currentCategory;
    prevCategoryRef.current = currentCategory;

    if (!currentCategory) {
      setAvailableSpecs([]);
      return;
    }

    const fetchSpecs = async () => {
      setLoadingSpecs(true);
      try {
        const res = await fetch(
          `${apiBaseUrl}/search/specifications/filters?categoryName=${encodeURIComponent(currentCategory)}`
        );
        if (res.ok) {
          const data = await res.json();
          const newAvailableSpecs = data.filters || [];
          setAvailableSpecs(newAvailableSpecs);
          
          if (categoryChanged && specFilters && Object.keys(specFilters).length > 0) {
            isUpdatingSpecsRef.current = true;
            
            const validFilters = Object.fromEntries(
              Object.entries(specFilters).filter(([key]) => 
                newAvailableSpecs.some((spec: any) => spec.name === key)
              )
            );
            
            if (Object.keys(validFilters).length > 0) {
              Object.entries(validFilters).forEach(([key, value]) => {
                onSpecFilterChange(key, value);
              });
            } else {
              onClearSpecFilters();
            }
            
            setTimeout(() => {
              isUpdatingSpecsRef.current = false;
            }, 0);
          }
        } else if (res.status === 404) {
          setAvailableSpecs([]);
          if (categoryChanged) {
            onClearSpecFilters();
          }
        }
      } catch (e) {
        console.error('Ошибка загрузки спецификаций:', e);
        setAvailableSpecs([]);
      } finally {
        setLoadingSpecs(false);
      }
    };
    
    fetchSpecs();
  }, [filters.category, apiBaseUrl, specFilters, onSpecFilterChange, onClearSpecFilters]);

  // 🔧 FIX: Эффект синхронизации дебаунс-значений с глобальным стейтом
  useEffect(() => {
    if (!debouncedLocalNumberSpecs) return;
    
    try {
      const parsed = JSON.parse(debouncedLocalNumberSpecs as string) as Record<string, { Min?: string; Max?: string }>;
      
      Object.entries(parsed).forEach(([specName, values]) => {
        const min = values.Min && values.Min !== '' ? parseFloat(values.Min) : undefined;
        const max = values.Max && values.Max !== '' ? parseFloat(values.Max) : undefined;
        
        // Если оба значения пустые — сбрасываем фильтр в null
        if (min === undefined && max === undefined) {
          onSpecFilterChange(specName, null);
        } else {
          onSpecFilterChange(specName, {
            ...(min !== undefined && { Min: min }),
            ...(max !== undefined && { Max: max }),
          });
        }
      });
    } catch (e) {
      console.error('Ошибка парсинга дебаунс-значений:', e);
    }
  }, [debouncedLocalNumberSpecs, onSpecFilterChange]);

  // 🔧 FIX: Синхронизация локального стейта при изменении specFilters извне (например, из URL)
  useEffect(() => {
    if (specFilters) {
      const synced: Record<string, { Min?: string; Max?: string }> = {};
      Object.entries(specFilters).forEach(([name, value]) => {
        if (typeof value === 'object' && value !== null && 'Min' in value && 'Max' in value) {
          synced[name] = {
            Min: value.Min?.toString() || '',
            Max: value.Max?.toString() || '',
          };
        }
      });
      setLocalNumberSpecs(synced);
    }
  }, [specFilters]);

  // Обработчики базовых фильтров
  const handlePriceChange = useCallback((field: 'minPrice' | 'maxPrice', value: string) => {
    const newMin = field === 'minPrice' ? value : filters.minPrice;
    const newMax = field === 'maxPrice' ? value : filters.maxPrice;
    
    const validation = validateNumberRange(
      newMin || null,
      newMax || null
    );
    
    if (!validation.valid) {
      setSpecErrors(prev => ({ ...prev, [field]: validation.error! }));
      return;
    }
    
    setSpecErrors(prev => {
      const { [field]: _, ...rest } = prev;
      return rest;
    });
    
    onBasicFilterChange({ [field]: value });
  }, [filters.minPrice, filters.maxPrice, onBasicFilterChange]);

  const handleCategoryChange = useCallback((value: string) => {
    setSpecErrors({});
    onBasicFilterChange({ category: value });
  }, [onBasicFilterChange]);

  const handleInStockChange = useCallback((checked: boolean) => {
    const value = checked ? true : null;
    onBasicFilterChange({ inStock: value });
  }, [onBasicFilterChange]);

  // 🔧 FIX: Обновлённый обработчик для числовых инпутов с локальным стейтом
  const handleNumberSpecChange = useCallback((specName: string, spec: SpecificationFilterDto, field: 'Min' | 'Max', rawValue: string) => {
    if (isUpdatingSpecsRef.current) return;
    
    // 1. Мгновенно обновляем локальный стейт для отзывчивости UI
    setLocalNumberSpecs(prev => ({
      ...prev,
      [specName]: {
        ...(prev[specName] || {}),
        [field]: rawValue,
      },
    }));
    
    // 2. Валидация диапазона (опционально, для мгновенной обратной связи)
    if (spec.dataType === 'Number') {
      const currentValues = localNumberSpecs[specName] || {
        Min: specFilters?.[specName]?.Min?.toString() || '',
        Max: specFilters?.[specName]?.Max?.toString() || '',
      };
      const testMin = field === 'Min' ? rawValue : currentValues.Min;
      const testMax = field === 'Max' ? rawValue : currentValues.Max;
      
      if (testMin && testMax && testMin !== '' && testMax !== '') {
        const minNum = parseFloat(testMin);
        const maxNum = parseFloat(testMax);
        if (!isNaN(minNum) && !isNaN(maxNum) && minNum > maxNum) {
          setSpecErrors(prev => ({ ...prev, [specName]: 'Мин. значение не может превышать макс.' }));
          return;
        }
      }
    }
    
    // 3. Очищаем ошибку при вводе
    setSpecErrors(prev => {
      const { [specName]: _, ...rest } = prev;
      return rest;
    });
  }, [localNumberSpecs, specFilters, isUpdatingSpecsRef]);

  // Обработчик для текстовых фильтров (без изменений, но оставим для полноты)
  const handleSpecChange = useCallback((specName: string, spec: SpecificationFilterDto, value: any) => {
    if (isUpdatingSpecsRef.current) return;
    
    if (spec.dataType === 'Number' && value?.Min != null && value?.Max != null) {
      const min = parseFloat(String(value.Min));
      const max = parseFloat(String(value.Max));
      if (!isNaN(min) && !isNaN(max) && min > max) {
        setSpecErrors(prev => ({ ...prev, [specName]: 'Мин. значение не может превышать макс.' }));
        return;
      }
    }
    
    setSpecErrors(prev => {
      const { [specName]: _, ...rest } = prev;
      return rest;
    });
    
    let finalValue: SpecFilterValue = value;
    if (spec.dataType === 'Number' && typeof value === 'object' && value !== null) {
      const min = value.Min != null && value.Min !== '' ? parseFloat(String(value.Min)) : undefined;
      const max = value.Max != null && value.Max !== '' ? parseFloat(String(value.Max)) : undefined;
      
      if (min === undefined && max === undefined) {
        finalValue = null;
      } else {
        finalValue = {
          ...(min !== undefined && { Min: min }),
          ...(max !== undefined && { Max: max }),
        };
      }
    }
    
    onSpecFilterChange(specName, finalValue);
  }, [onSpecFilterChange]);

  const hasActiveSpecFilters = useMemo(() => {
    if (!specFilters || Object.keys(specFilters).length === 0) return false;
    return Object.values(specFilters).some(v => {
      if (v == null || v === '') return false;
      if (typeof v === 'object' && v !== null) {
        return Object.values(v).some(x => x != null && x !== '');
      }
      return true;
    });
  }, [specFilters]);

  const handleClearSpecs = useCallback(() => {
    setSpecErrors({});
    setLocalNumberSpecs({}); // 🔧 FIX: Очищаем и локальный стейт
    onClearSpecFilters();
  }, [onClearSpecFilters]);

  const currentPriceValues = useMemo(() => ({
    minPrice: filters.minPrice ?? '',
    maxPrice: filters.maxPrice ?? '',
  }), [filters.minPrice, filters.maxPrice]);

  return (
    <aside className="filters-panel">
      <h3 className="filters-panel__title">Фильтры</h3>

      <div className="filters-panel__section">
        <h4 className="filters-panel__section-title">Цена, ₽</h4>
        <div className="filters-panel__price-inputs">
          <div className="filters-panel__price-input-wrapper">
            <label className="filters-panel__input-label">От</label>
            <input
              type="number"
              min="0"
              step="any"
              placeholder="0"
              value={currentPriceValues.minPrice}
              onChange={(e) => handlePriceChange('minPrice', e.target.value)}
              className={`filters-panel__input ${specErrors.minPrice ? 'filters-panel__input--error' : ''}`}
            />
          </div>
          <div className="filters-panel__price-input-wrapper">
            <label className="filters-panel__input-label">До</label>
            <input
              type="number"
              min="0"
              step="any"
              placeholder="100 000"
              value={currentPriceValues.maxPrice}
              onChange={(e) => handlePriceChange('maxPrice', e.target.value)}
              className={`filters-panel__input ${specErrors.maxPrice ? 'filters-panel__input--error' : ''}`}
            />
          </div>
        </div>
        {(specErrors.minPrice || specErrors.maxPrice) && (
          <span className="filters-panel__status filters-panel__status--error">
            {specErrors.minPrice || specErrors.maxPrice}
          </span>
        )}
      </div>

      <div className="filters-panel__section">
        <label className="filters-panel__checkbox-label">
          <input
            type="checkbox"
            className="filters-panel__checkbox"
            checked={filters.inStock === true}
            onChange={(e) => handleInStockChange(e.target.checked)}
          />
          <span className="filters-panel__checkbox-text">Только в наличии</span>
        </label>
      </div>

      <div className="filters-panel__section">
        <h4 className="filters-panel__section-title">Категория</h4>
        <select
          className="filters-panel__category-select"
          value={filters.category ?? ''}
          onChange={(e) => handleCategoryChange(e.target.value)}
        >
          <option value="">Все категории</option>
          {categories.map((cat) => (
            <option key={cat.categoryId} value={cat.categoryName}>
              {cat.categoryName}
            </option>
          ))}
        </select>
      </div>

      {filters.category && (
        <div className="filters-panel__section">
          <div className="filters-panel__specs-header">
            <h4 className="filters-panel__specs-title">Характеристики</h4>
            {hasActiveSpecFilters && (
              <button
                type="button"
                className="filters-panel__specs-clear-btn"
                onClick={handleClearSpecs}
              >
                Сбросить
              </button>
            )}
          </div>
          
          {loadingSpecs ? (
            <div className="filters-panel__status filters-panel__status--loading">
              Загрузка...
            </div>
          ) : availableSpecs.length === 0 ? (
            <p className="filters-panel__status filters-panel__status--info">
              Нет фильтров для этой категории
            </p>
          ) : (
            <div className="filters-panel__specs-list">
              {availableSpecs.map((spec) => (
                <div key={spec.specId} className="filters-panel__spec-item">
                  <label className="filters-panel__spec-label">
                    {spec.displayName}
                  </label>

                  {spec.dataType === 'Text' && (
                    <select
                      className="filters-panel__spec-select"
                      value={specFilters?.[spec.name] ?? ''}
                      onChange={(e) => {
                        const val = e.target.value;
                        onSpecFilterChange(spec.name, val === '' ? null : val);
                      }}
                    >
                      <option value="">Любое</option>
                      {spec.possibleValues?.map((val, idx) => (
                        <option key={`${spec.name}-${idx}`} value={val}>
                          {val}
                        </option>
                      ))}
                    </select>
                  )}

                  {spec.dataType === 'Number' && (
                    <div className="filters-panel__price-inputs">
                      <div className="filters-panel__price-input-wrapper">
                        <label className="filters-panel__input-label">От</label>
                        <input
                          type="number"
                          min="0"
                          step="any"
                          placeholder="0"
                          // 🔧 FIX: Читаем из локального стейта, иначе из глобального
                          value={localNumberSpecs[spec.name]?.Min ?? specFilters?.[spec.name]?.Min?.toString() ?? ''}
                          // 🔧 FIX: Используем новый обработчик с локальным стейтом
                          onChange={(e) => handleNumberSpecChange(spec.name, spec, 'Min', e.target.value)}
                          className={`filters-panel__input ${specErrors[spec.name] ? 'filters-panel__input--error' : ''}`}
                        />
                      </div>
                      <div className="filters-panel__price-input-wrapper">
                        <label className="filters-panel__input-label">До</label>
                        <input
                          type="number"
                          min="0"
                          step="any"
                          placeholder="100 000"
                          value={localNumberSpecs[spec.name]?.Max ?? specFilters?.[spec.name]?.Max?.toString() ?? ''}
                          onChange={(e) => handleNumberSpecChange(spec.name, spec, 'Max', e.target.value)}
                          className={`filters-panel__input ${specErrors[spec.name] ? 'filters-panel__input--error' : ''}`}
                        />
                      </div>
                    </div>
                  )}

                  {specErrors[spec.name] && (
                    <span className="filters-panel__status filters-panel__status--error">
                      {specErrors[spec.name]}
                    </span>
                  )}
                </div>
              ))}
            </div>
          )}
        </div>
      )}
    </aside>
  );
};

export default FiltersPanel;