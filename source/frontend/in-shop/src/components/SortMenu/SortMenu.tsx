// src/components/SortMenu/SortMenu.tsx
import React, { useState } from 'react';
import './SortMenu.css'; // Импортируем стили

interface SortMenuProps {
  currentSortOption: string; // Значение текущей сортировки (например, 'name-asc'), передаётся из родителя
  onSortOptionChange: (newSortOption: string) => void; // Функция для обновления сортировки в родителе, передаётся из родителя
}

const SortMenu: React.FC<SortMenuProps> = ({ currentSortOption, onSortOptionChange }) => {
  const [isMenuOpen, setIsMenuOpen] = useState(false); // Состояние открытия/закрытия меню всё ещё нужно внутри компонента

  const toggleMenu = () => {
    setIsMenuOpen(!isMenuOpen);
  };

  const handleOptionChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const selectedValue = e.target.value;
    // Вызываем функцию из пропсов, передавая выбранное значение
    onSortOptionChange(selectedValue);
    // Опционально: закрыть меню после выбора
    // setIsMenuOpen(false);
  };

  // Функция для получения отображаемого текста для опции
  const getDisplayText = (value: string): string => {
    switch (value) {
      case 'name-asc':
        return 'Название товара ↓';
      case 'name-desc':
        return 'Название товара ↑';
      case 'price-asc':
        return 'Цена ↑';
      case 'price-desc':
        return 'Цена ↓';
      default:
        return 'Сортировка';
    }
  };

  return (
    <div className="div__sort-menu">
      <div className={`sort-menu ${isMenuOpen ? 'open' : ''}`}>
        <div
          className="sort-menu-header"
          onClick={toggleMenu}
        >
          {/* Отображаем текст, соответствующий текущей опции */}
          {getDisplayText(currentSortOption)}
          <span className={`sort-arrow ${isMenuOpen ? 'up' : 'down'}`}></span>
        </div>
        <div className="sort-menu-dropdown">
          <label className="sort-option">
            <input
              type="radio"
              name="sort"
              value="name-asc"
              checked={currentSortOption === 'name-asc'} // checked теперь зависит от пропса
              onChange={handleOptionChange} // handleChange теперь использует пропс
            />
            <span className="sort-label">Название товара ↓</span>
          </label>
          <label className="sort-option">
            <input
              type="radio"
              name="sort"
              value="name-desc"
              checked={currentSortOption === 'name-desc'} // checked теперь зависит от пропса
              onChange={handleOptionChange} // handleChange теперь использует пропс
            />
            <span className="sort-label">Название товара ↑</span>
          </label>
          <label className="sort-option">
            <input
              type="radio"
              name="sort"
              value="price-asc"
              checked={currentSortOption === 'price-asc'} // checked теперь зависит от пропса
              onChange={handleOptionChange} // handleChange теперь использует пропс
            />
            <span className="sort-label">Цена ↑</span>
          </label>
          <label className="sort-option">
            <input
              type="radio"
              name="sort"
              value="price-desc"
              checked={currentSortOption === 'price-desc'} // checked теперь зависит от пропса
              onChange={handleOptionChange} // handleChange теперь использует пропс
            />
            <span className="sort-label">Цена ↓</span>
          </label>
        </div>
      </div>
    </div>
  );
};

export default SortMenu;