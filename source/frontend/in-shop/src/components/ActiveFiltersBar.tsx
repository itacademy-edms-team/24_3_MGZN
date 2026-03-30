// src/components/ActiveFiltersBar.tsx
import React from 'react';
import { FiltersState } from '../types/search';

type SpecFilterValue = string | number | { Min?: number; Max?: number } | null;

interface Props {
  filters: FiltersState;
  specFilters: Record<string, SpecFilterValue> | null;
  // 🔧 FIX: Маппинг internal name → human-readable displayName
  specDisplayNames?: Record<string, string>;
  onRemoveBasic: (key: keyof FiltersState) => void;
  onRemoveSpec: (specName: string) => void;
  onClearAll: () => void;
}

const ActiveFiltersBar: React.FC<Props> = ({ 
  filters, 
  specFilters, 
  specDisplayNames = {}, // 🔧 FIX: Default empty object
  onRemoveBasic, 
  onRemoveSpec, 
  onClearAll 
}) => {
  // Форматирование значения числового фильтра для отображения
  const formatSpecValue = (value: SpecFilterValue): string => {
    if (value == null || value === '') return '';
    if (typeof value === 'object') {
      const parts: string[] = [];
      if (value.Min != null) parts.push(`от ${value.Min}`);
      if (value.Max != null) parts.push(`до ${value.Max}`);
      return parts.join(' ');
    }
    return String(value);
  };

  const chips: Array<{ label: string; onRemove: () => void; key: string }> = [];

  // Базовые фильтры
  if (filters.query) chips.push({ 
    label: `Поиск: "${filters.query}"`, 
    onRemove: () => onRemoveBasic('query'),
    key: 'query'
  });
  if (filters.minPrice) chips.push({ 
    label: `От ${filters.minPrice} ₽`, 
    onRemove: () => onRemoveBasic('minPrice'),
    key: 'minPrice'
  });
  if (filters.maxPrice) chips.push({ 
    label: `До ${filters.maxPrice} ₽`, 
    onRemove: () => onRemoveBasic('maxPrice'),
    key: 'maxPrice'
  });
  if (filters.category) chips.push({ 
    label: `Категория: ${filters.category}`, 
    onRemove: () => onRemoveBasic('category'),
    key: 'category'
  });
  if (filters.inStock !== null) chips.push({ 
    label: 'В наличии', 
    onRemove: () => onRemoveBasic('inStock'),
    key: 'inStock'
  });
  
  // 🔧 FIX: Фильтры по характеристикам с использованием displayName
  if (specFilters) {
    Object.entries(specFilters).forEach(([specName, specValue]) => {
      // 🔧 FIX: Берём displayName из мапы, или падаем на specName
      const displayName = specDisplayNames[specName] || specName;
      const formattedValue = formatSpecValue(specValue);
      
      chips.push({ 
        label: `${displayName}: ${formattedValue}`, 
        onRemove: () => onRemoveSpec(specName),
        key: `spec-${specName}` // 🔧 FIX: Уникальный ключ для списка
      });
    });
  }

  if (chips.length === 0) return null;

  return (
    <div className="active-filters-bar">
      <div className="chips-container">
        {chips.map((chip) => (
          <span key={chip.key} className="filter-chip">
            {chip.label}
            <button 
              type="button" 
              onClick={chip.onRemove} 
              className="chip-remove" 
              aria-label={`Убрать фильтр "${chip.label}"`}
            >
              ×
            </button>
          </span>
        ))}
      </div>
      <button type="button" onClick={onClearAll} className="clear-all-btn">
        Сбросить все
      </button>
    </div>
  );
};

export default ActiveFiltersBar;