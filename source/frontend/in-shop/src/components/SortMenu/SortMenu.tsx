// src/components/SortMenu/SortMenu.tsx
import React, { useState, useEffect, useRef } from 'react';
import './SortMenu.css';

export type SortOption = 'name-asc' | 'name-desc' | 'price-asc' | 'price-desc';

interface SortMenuProps {
  currentSortOption: SortOption;
  onSortOptionChange: (newSortOption: SortOption) => void;
  className?: string; // ✅ Для гибкого позиционирования
}

const SORT_OPTIONS: Array<{ value: SortOption; label: string }> = [
  { value: 'name-asc', label: 'Название товара ↓' },
  { value: 'name-desc', label: 'Название товара ↑' },
  { value: 'price-asc', label: 'Цена ↑' },
  { value: 'price-desc', label: 'Цена ↓' },
];

const SortMenu: React.FC<SortMenuProps> = ({
  currentSortOption,
  onSortOptionChange,
  className = '',
}) => {
  const [isMenuOpen, setIsMenuOpen] = useState(false);
  const menuRef = useRef<HTMLDivElement>(null);

  // ✅ Закрытие меню при клике вне
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (menuRef.current && !menuRef.current.contains(event.target as Node)) {
        setIsMenuOpen(false);
      }
    };

    if (isMenuOpen) {
      document.addEventListener('mousedown', handleClickOutside);
    }
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, [isMenuOpen]);

  // ✅ Закрытие по Escape
  useEffect(() => {
    const handleEscape = (event: KeyboardEvent) => {
      if (event.key === 'Escape') setIsMenuOpen(false);
    };
    if (isMenuOpen) {
      document.addEventListener('keydown', handleEscape);
    }
    return () => document.removeEventListener('keydown', handleEscape);
  }, [isMenuOpen]);

  const toggleMenu = () => setIsMenuOpen(!isMenuOpen);

  const handleOptionChange = (value: SortOption) => {
    onSortOptionChange(value);
    setIsMenuOpen(false); // ✅ Закрываем после выбора
  };

  const currentLabel = SORT_OPTIONS.find(opt => opt.value === currentSortOption)?.label || 'Сортировка';

  return (
    <div className={`sort-menu ${className}`} ref={menuRef}>
      <button
        type="button"
        className="sort-menu__trigger"
        onClick={toggleMenu}
        aria-haspopup="listbox"
        aria-expanded={isMenuOpen}
        aria-label="Выберите сортировку"
      >
        <span className="sort-menu__trigger-text">{currentLabel}</span>
        <span className={`sort-menu__arrow ${isMenuOpen ? 'sort-menu__arrow--up' : 'sort-menu__arrow--down'}`}></span>
      </button>

      {isMenuOpen && (
        <div className="sort-menu__dropdown" role="listbox">
          {SORT_OPTIONS.map((option) => (
            <label
              key={option.value}
              className={`sort-menu__option ${currentSortOption === option.value ? 'sort-menu__option--active' : ''}`}
              role="option"
              aria-selected={currentSortOption === option.value}
            >
              <input
                type="radio"
                name="sort"
                value={option.value}
                checked={currentSortOption === option.value}
                onChange={() => handleOptionChange(option.value)}
                className="sort-menu__radio"
              />
              <span className="sort-menu__label">{option.label}</span>
            </label>
          ))}
        </div>
      )}
    </div>
  );
};

export default SortMenu;