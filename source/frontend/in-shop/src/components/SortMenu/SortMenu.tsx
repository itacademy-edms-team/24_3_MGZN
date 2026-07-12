// src/components/SortMenu/SortMenu.tsx
import React, { useState, useEffect, useLayoutEffect, useRef, useCallback } from 'react';
import { createPortal } from 'react-dom';
import { useMatchMedia } from '../../hooks/useMatchMedia';
import './SortMenu.css';

export type SortOption = 'relevance' | 'name-asc' | 'name-desc' | 'price-asc' | 'price-desc';

interface SortMenuProps {
  currentSortOption: SortOption;
  onSortOptionChange: (newSortOption: SortOption) => void;
  className?: string;
}

const SORT_OPTIONS: Array<{ value: SortOption; label: string }> = [
  { value: 'relevance', label: 'По релевантности' },
  { value: 'name-asc', label: 'Название товара ↓' },
  { value: 'name-desc', label: 'Название товара ↑' },
  { value: 'price-asc', label: 'Цена ↑' },
  { value: 'price-desc', label: 'Цена ↓' },
];

const MOBILE_EDGE_FALLBACK = 16;

const readMobileEdge = (): number => {
  if (typeof window === 'undefined') return MOBILE_EDGE_FALLBACK;
  const value = getComputedStyle(document.documentElement)
    .getPropertyValue('--space-edge')
    .trim();
  const parsed = parseFloat(value);
  return Number.isFinite(parsed) ? parsed : MOBILE_EDGE_FALLBACK;
};

const SortMenu: React.FC<SortMenuProps> = ({
  currentSortOption,
  onSortOptionChange,
  className = '',
}) => {
  const [isMenuOpen, setIsMenuOpen] = useState(false);
  const [dropdownStyle, setDropdownStyle] = useState<React.CSSProperties>({});
  const menuRef = useRef<HTMLDivElement>(null);
  const dropdownRef = useRef<HTMLDivElement>(null);
  const triggerRef = useRef<HTMLButtonElement>(null);
  const isMobileLayout = useMatchMedia('(max-width: 768px)');

  const updateDropdownPosition = useCallback(() => {
    if (!isMobileLayout || !triggerRef.current) return;

    const rect = triggerRef.current.getBoundingClientRect();
    const edge = readMobileEdge();
    const horizontalInset = edge;
    const width = Math.max(0, window.innerWidth - horizontalInset * 2);
    const top = rect.bottom + 4;
    const maxHeight = Math.max(120, window.innerHeight - top - edge);

    setDropdownStyle({
      position: 'fixed',
      top: `${top}px`,
      left: `${horizontalInset}px`,
      width: `${width}px`,
      maxHeight: `${maxHeight}px`,
    });
  }, [isMobileLayout]);

  useLayoutEffect(() => {
    if (!isMenuOpen || !isMobileLayout) return undefined;

    updateDropdownPosition();

    window.addEventListener('resize', updateDropdownPosition);
    window.addEventListener('scroll', updateDropdownPosition, true);

    return () => {
      window.removeEventListener('resize', updateDropdownPosition);
      window.removeEventListener('scroll', updateDropdownPosition, true);
    };
  }, [isMenuOpen, isMobileLayout, updateDropdownPosition]);

  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      const target = event.target as Node;
      if (menuRef.current?.contains(target) || dropdownRef.current?.contains(target)) {
        return;
      }
      setIsMenuOpen(false);
    };

    if (isMenuOpen) {
      document.addEventListener('mousedown', handleClickOutside);
    }
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, [isMenuOpen]);

  useEffect(() => {
    const handleEscape = (event: KeyboardEvent) => {
      if (event.key === 'Escape') setIsMenuOpen(false);
    };
    if (isMenuOpen) {
      document.addEventListener('keydown', handleEscape);
    }
    return () => document.removeEventListener('keydown', handleEscape);
  }, [isMenuOpen]);

  useEffect(() => {
    if (!isMobileLayout) {
      setIsMenuOpen(false);
    }
  }, [isMobileLayout]);

  const toggleMenu = () => setIsMenuOpen((open) => !open);

  const handleOptionChange = (value: SortOption) => {
    onSortOptionChange(value);
    setIsMenuOpen(false);
  };

  const currentLabel =
    SORT_OPTIONS.find((opt) => opt.value === currentSortOption)?.label || 'Сортировка';

  const dropdownClassName = [
    'sort-menu__dropdown',
    isMobileLayout ? 'sort-menu__dropdown--portal' : '',
  ]
    .filter(Boolean)
    .join(' ');

  const dropdown = isMenuOpen ? (
    <div
      ref={dropdownRef}
      className={dropdownClassName}
      style={isMobileLayout ? dropdownStyle : undefined}
      role="listbox"
    >
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
  ) : null;

  return (
    <div className={`sort-menu ${className}`} ref={menuRef}>
      <button
        ref={triggerRef}
        type="button"
        className="sort-menu__trigger"
        onClick={toggleMenu}
        aria-haspopup="listbox"
        aria-expanded={isMenuOpen}
        aria-label="Выберите сортировку"
      >
        <span className="sort-menu__trigger-text">{currentLabel}</span>
        <span
          className={`sort-menu__arrow ${isMenuOpen ? 'sort-menu__arrow--up' : 'sort-menu__arrow--down'}`}
        />
      </button>

      {isMobileLayout && dropdown
        ? createPortal(dropdown, document.body)
        : !isMobileLayout && dropdown}
    </div>
  );
};

export default SortMenu;
