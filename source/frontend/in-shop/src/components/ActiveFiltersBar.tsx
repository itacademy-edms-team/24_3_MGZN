import React from 'react';
import { FiltersState } from '../types/search';

interface Props {
  filters: FiltersState;
  specFilters: Record<string, any> | null;
  onRemoveBasic: (key: keyof FiltersState) => void;
  onRemoveSpec: (specName: string) => void;
  onClearAll: () => void;
}

const ActiveFiltersBar: React.FC<Props> = ({ 
  filters, specFilters, onRemoveBasic, onRemoveSpec, onClearAll 
}) => {
  const chips: Array<{ label: string; onRemove: () => void }> = [];

  if (filters.category) chips.push({ label: `Категория: ${filters.category}`, onRemove: () => onRemoveBasic('category') });
  if (filters.minPrice) chips.push({ label: `От ${filters.minPrice} ₽`, onRemove: () => onRemoveBasic('minPrice') });
  if (filters.maxPrice) chips.push({ label: `До ${filters.maxPrice} ₽`, onRemove: () => onRemoveBasic('maxPrice') });
  if (filters.inStock) chips.push({ label: 'В наличии', onRemove: () => onRemoveBasic('inStock') });
  
  if (specFilters) {
    Object.entries(specFilters).forEach(([name, value]) => {
      let label = `${name}: `;
      if (typeof value === 'object' && value !== null) {
        if (value.Min != null && value.Max != null) label += `${value.Min}–${value.Max}`;
        else if (value.Min != null) label += `от ${value.Min}`;
        else if (value.Max != null) label += `до ${value.Max}`;
      } else {
        label += String(value);
      }
      chips.push({ label, onRemove: () => onRemoveSpec(name) });
    });
  }

  if (chips.length === 0) return null;

  return (
    <div className="active-filters-bar">
      <div className="chips-container">
        {chips.map((chip, idx) => (
          <span key={idx} className="filter-chip">
            {chip.label}
            <button type="button" onClick={chip.onRemove} className="chip-remove" aria-label="Убрать фильтр">×</button>
          </span>
        ))}
      </div>
      <button type="button" onClick={onClearAll} className="clear-all-btn">Сбросить все</button>
    </div>
  );
};

export default ActiveFiltersBar;