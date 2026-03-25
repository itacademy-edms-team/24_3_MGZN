// src/components/FiltersPanel/SpecFilterItem.tsx
import React from 'react';
import { SpecificationFilterDto } from '../../types/search';

interface Props {
  spec: SpecificationFilterDto;
  value: any;
  onChange: (value: any) => void;
  error?: string;
}

const SpecFilterItem: React.FC<Props> = ({ spec, value, onChange, error }) => {
  if (spec.dataType === 'Text') {
    return (
      <div className="spec-filter-item">
        <label htmlFor={`spec-${spec.name}`}>{spec.displayName}</label>
        <select
          id={`spec-${spec.name}`}
          value={value ?? ''}
          onChange={(e) => onChange(e.target.value || null)}
          className="spec-value-select"
        >
          <option value="">Любое</option>
          {spec.possibleValues?.map((val, idx) => (
            // ✅ Ключ должен быть стабильным: используем значение + индекс
            <option key={`${spec.name}-${idx}`} value={val}>
              {val}
            </option>
          ))}
        </select>
        {error && <span className="filter-error">{error}</span>}
      </div>
    );
  }

  if (spec.dataType === 'Number') {
    const min = value?.Min ?? '';
    const max = value?.Max ?? '';
    
    return (
      <div className="spec-filter-item">
        <label>{spec.displayName}</label>
        <div className="number-filter-inputs">
          <input
            type="number"
            min="0"
            step="any"
            placeholder="От"
            value={min}
            onChange={(e) => {
              const val = e.target.value;
              onChange({ 
                ...value, 
                Min: val === '' ? null : val 
              });
            }}
            className="number-filter-input"
          />
          <span>—</span>
          <input
            type="number"
            min="0"
            step="any"
            placeholder="До"
            value={max}
            onChange={(e) => {
              const val = e.target.value;
              onChange({ 
                ...value, 
                Max: val === '' ? null : val 
              });
            }}
            className="number-filter-input"
          />
        </div>
        {error && <span className="filter-error">{error}</span>}
      </div>
    );
  }

  return null;
};

export default SpecFilterItem;