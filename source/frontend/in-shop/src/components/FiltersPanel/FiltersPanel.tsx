// src/components/FiltersPanel/FiltersPanel.tsx
import React, { useState, useEffect, useCallback, useMemo, useRef, memo } from 'react';
import { SpecificationFilterDto, FiltersState } from '../../types/search.ts';
import { validateNumberRange } from '../../utils/filters.ts';
import './FiltersPanel.css';

interface CategoryDto {
  categoryId: number;
  categoryName: string;
  imageURL?: string;
}

type SpecFilterValue = string | number | { Min?: number; Max?: number } | null;

const SPEC_FILTER_DEBOUNCE_DELAY = 400;

interface Props {
  filters: FiltersState;
  specFilters: Record<string, SpecFilterValue> | null;
  onBasicFilterChange: (changes: Partial<FiltersState>) => void;
  onSpecFilterChange: (specName: string, value: SpecFilterValue) => void;
  onClearSpecFilters: () => void;
  apiBaseUrl: string;
  isCategoryForced?: boolean
}

// ===== Вспомогательный компонент: кастомный дропдаун (стиль SortMenu) =====
interface DropdownOption {
  value: string;
  label: string;
}

interface CustomDropdownProps {
  options: DropdownOption[];
  value: string;
  onChange: (value: string) => void;
  placeholder?: string;
  className?: string;
  dropdownClassName?: string;
}

const CustomDropdown: React.FC<CustomDropdownProps> = memo(({
  options,
  value,
  onChange,
  placeholder = 'Выберите...',
  className = '',
  dropdownClassName = '',
}) => {
  const [isOpen, setIsOpen] = useState(false);
  const dropdownRef = useRef<HTMLDivElement>(null);
  const triggerRef = useRef<HTMLButtonElement>(null);

  const selectedLabel = useMemo(() => {
    const found = options.find(opt => opt.value === value);
    return found ? found.label : placeholder;
  }, [options, value, placeholder]);

  // Закрытие при клике вне
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (dropdownRef.current && !dropdownRef.current.contains(event.target as Node)) {
        setIsOpen(false);
      }
    };
    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  // Управление с клавиатуры
  const handleTriggerKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter' || e.key === ' ') {
      e.preventDefault();
      setIsOpen(prev => !prev);
    } else if (e.key === 'Escape') {
      setIsOpen(false);
      triggerRef.current?.focus();
    } else if (e.key === 'ArrowDown' && isOpen) {
      e.preventDefault();
      const currentIndex = options.findIndex(opt => opt.value === value);
      const nextIndex = (currentIndex + 1) % options.length;
      onChange(options[nextIndex].value);
    } else if (e.key === 'ArrowUp' && isOpen) {
      e.preventDefault();
      const currentIndex = options.findIndex(opt => opt.value === value);
      const prevIndex = currentIndex <= 0 ? options.length - 1 : currentIndex - 1;
      onChange(options[prevIndex].value);
    }
  };

  return (
    <div className={`filters-panel__dropdown-wrapper ${className}`} ref={dropdownRef}>
      {/* Триггер — стиль как в SortMenu */}
      <button
        ref={triggerRef}
        type="button"
        className={`filters-panel__category-select ${isOpen ? 'filters-panel__category-select--open' : ''}`}
        onClick={() => setIsOpen(prev => !prev)}
        onKeyDown={handleTriggerKeyDown}
        aria-haspopup="listbox"
        aria-expanded={isOpen}
      >
        <span className="sort-menu__trigger-text">{selectedLabel}</span>
        <span className={`sort-menu__arrow ${isOpen ? 'sort-menu__arrow--up' : 'sort-menu__arrow--down'}`} />
      </button>

      {/* Выпадающее меню — стиль как в SortMenu (без чекбоксов) */}
      {isOpen && (
        <ul 
          className={`filters-panel__dropdown ${dropdownClassName}`}
          role="listbox"
        >
          {options.map((option) => (
            <li
              key={option.value}
              className={`filters-panel__dropdown-option ${option.value === value ? 'filters-panel__dropdown-option--active' : ''}`}
              role="option"
              aria-selected={option.value === value}
              onClick={() => {
                onChange(option.value);
                setIsOpen(false);
                triggerRef.current?.focus();
              }}
              onMouseDown={(e) => e.preventDefault()}
            >
              {/* Только текст опции — без чекбоксов/радиокнопок */}
              <span className="filters-panel__dropdown-label">{option.label}</span>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
});

CustomDropdown.displayName = 'CustomDropdown';

// ===== Основной компонент FiltersPanel =====
const FiltersPanel = memo<Props>(({
  filters,
  specFilters,
  onBasicFilterChange,
  onSpecFilterChange,
  onClearSpecFilters,
  apiBaseUrl,
  isCategoryForced = false,
}) => {
  const [specErrors, setSpecErrors] = useState<Record<string, string>>({});
  const [categories, setCategories] = useState<CategoryDto[]>([]);
  const [availableSpecs, setAvailableSpecs] = useState<SpecificationFilterDto[]>([]);
  const [loadingSpecs, setLoadingSpecs] = useState(false);
  const prevCategoryRef = useRef<string | null>(null);
  const isUpdatingSpecsRef = useRef(false);

  const [localNumberSpecs, setLocalNumberSpecs] = useState<Record<string, { Min?: string; Max?: string }>>({});

  const localNumberSpecsRef = useRef(localNumberSpecs);
  useEffect(() => {
    localNumberSpecsRef.current = localNumberSpecs;
  }, [localNumberSpecs]);

  const debounceTimerRef = useRef<NodeJS.Timeout | null>(null);

  useEffect(() => {
    if (debounceTimerRef.current !== null) {
      return;
    }
    
    if (specFilters) {
      const synced: Record<string, { Min?: string; Max?: string }> = {};
      Object.entries(specFilters).forEach(([name, value]) => {
        if (typeof value === 'object' && value !== null && ('Min' in value || 'Max' in value)) {
          const specInfo = availableSpecs.find(s => s.name === name);
          if (specInfo && specInfo.dataType === 'Number') {
            synced[name] = {
              Min: (value as any).Min?.toString() ?? '',
              Max: (value as any).Max?.toString() ?? '',
            };
          }
        }
      });
      setLocalNumberSpecs(synced);
    }
  }, [specFilters, availableSpecs]);

  // --- Загрузка категорий ---
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

  // --- Загрузка спецификаций ---
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

            setLocalNumberSpecs({});

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
  }, [filters.category, apiBaseUrl, onSpecFilterChange, onClearSpecFilters]);

  // --- Обработчик числовых спецификаций ---
  const handleNumberSpecChange = useCallback((specName: string, field: 'Min' | 'Max', rawValue: string) => {
    if (isUpdatingSpecsRef.current) return;

    setLocalNumberSpecs(prev => ({
      ...prev,
      [specName]: {
        ...(prev[specName] || {}),
        [field]: rawValue,
      },
    }));

    if (debounceTimerRef.current) {
      clearTimeout(debounceTimerRef.current);
    }

    debounceTimerRef.current = setTimeout(() => {
      const currentValues = localNumberSpecsRef.current[specName] || {};
      const minStr = currentValues.Min?.trim();
      const maxStr = currentValues.Max?.trim();

      const min = minStr && minStr !== '' ? parseFloat(minStr) : undefined;
      const max = maxStr && maxStr !== '' ? parseFloat(maxStr) : undefined;

      if (min !== undefined && max !== undefined && min > max) {
        setSpecErrors(prev => ({ ...prev, [specName]: 'Мин. значение не может превышать макс.' }));
        return;
      }

      setSpecErrors(prev => {
        const { [specName]: _, ...rest } = prev;
        return rest;
      });

      if (min === undefined && max === undefined) {
        onSpecFilterChange(specName, null);
      } else {
        const newValue: { Min?: number; Max?: number } = {};
        if (min !== undefined) newValue.Min = min;
        if (max !== undefined) newValue.Max = max;
        onSpecFilterChange(specName, newValue);
      }
    }, SPEC_FILTER_DEBOUNCE_DELAY);
  }, [onSpecFilterChange]);

  useEffect(() => {
    return () => {
      if (debounceTimerRef.current) {
        clearTimeout(debounceTimerRef.current);
      }
    };
  }, []);

  // ✅ FIX: Обработчик цены — валидация не блокирует ввод
  const handlePriceChange = useCallback((field: 'minPrice' | 'maxPrice', value: string) => {
    const newMin = field === 'minPrice' ? value : filters.minPrice;
    const newMax = field === 'maxPrice' ? value : filters.maxPrice;

    const shouldValidate = 
      (newMin !== undefined && newMin !== null && newMin.trim() !== '') &&
      (newMax !== undefined && newMax !== null && newMax.trim() !== '');

    if (shouldValidate) {
      const validation = validateNumberRange(
        newMin || null,
        newMax || null
      );

      if (!validation.valid) {
        setSpecErrors(prev => ({ ...prev, [field]: validation.error! }));
      } else {
        setSpecErrors(prev => {
          const { [field]: _, ...rest } = prev;
          return rest;
        });
      }
    } else {
      setSpecErrors(prev => {
        const { [field]: _, ...rest } = prev;
        return rest;
      });
    }

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

  const handleClearSpecs = useCallback(() => {
    setSpecErrors({});
    setLocalNumberSpecs({});
    if (debounceTimerRef.current) {
      clearTimeout(debounceTimerRef.current);
    }
    onClearSpecFilters();
  }, [onClearSpecFilters]);

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

  const currentPriceValues = useMemo(() => ({
    minPrice: filters.minPrice ?? '',
    maxPrice: filters.maxPrice ?? '',
  }), [filters.minPrice, filters.maxPrice]);

  // --- Опции для дропдауна категории ---
  const categoryOptions = useMemo(() => {
    const opts: DropdownOption[] = [{ value: '', label: 'Все категории' }];
    categories.forEach(cat => {
      opts.push({ value: cat.categoryName, label: cat.categoryName });
    });
    return opts;
  }, [categories]);

  // --- Рендер спецификаций ---
  const renderedSpecs = useMemo(() => {
    return availableSpecs.map((spec) => (
      <div key={spec.specId} className="filters-panel__spec-item">
        <label className="filters-panel__spec-label">
          {spec.displayName}
        </label>

        {spec.dataType === 'Text' && (
          <CustomDropdown
            options={[
              { value: '', label: 'Любое' },
              ...(spec.possibleValues?.map((val) => ({ value: val, label: val })) || []),
            ]}
            value={specFilters?.[spec.name]?.toString() ?? ''}
            onChange={(val) => onSpecFilterChange(spec.name, val === '' ? null : val)}
            placeholder="Любое"
            className="filters-panel__spec-select-wrapper"
            dropdownClassName="filters-panel__spec-dropdown"
          />
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
                value={localNumberSpecs[spec.name]?.Min ?? ''}
                onChange={(e) => handleNumberSpecChange(spec.name, 'Min', e.target.value)}
                className={`filters-panel__input ${!!specErrors[spec.name] ? 'filters-panel__input--error' : ''}`}
              />
            </div>
            <div className="filters-panel__price-input-wrapper">
              <label className="filters-panel__input-label">До</label>
              <input
                type="number"
                min="0"
                step="any"
                placeholder="100 000"
                value={localNumberSpecs[spec.name]?.Max ?? ''}
                onChange={(e) => handleNumberSpecChange(spec.name, 'Max', e.target.value)}
                className={`filters-panel__input ${!!specErrors[spec.name] ? 'filters-panel__input--error' : ''}`}
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
    ));
  }, [availableSpecs, specFilters, localNumberSpecs, specErrors, handleNumberSpecChange, onSpecFilterChange]);

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
        <h4 className="filters-panel__section-title">Категория</h4>
        
        {isCategoryForced ? (
          // 🔒 Режим "только просмотр": категория заблокирована
          <div className="filters-panel__category-locked">
            <span className="filters-panel__category-name">
              {filters.category || 'Все категории'}
            </span>
            <span className="filters-panel__lock-icon" title="Категория зафиксирована при переходе из каталога">
              🔒
            </span>
          </div>
        ) : (
          // ✏️ Режим поиска: категория редактируется
          <CustomDropdown
            options={categoryOptions}
            value={filters.category ?? ''}
            onChange={handleCategoryChange}
            placeholder="Все категории"
            className="filters-panel__category-select-wrapper"
            dropdownClassName="filters-panel__category-dropdown"
          />
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
              {renderedSpecs}
            </div>
          )}
        </div>
      )}
    </aside>
  );
});

FiltersPanel.displayName = 'FiltersPanel';

export default FiltersPanel;