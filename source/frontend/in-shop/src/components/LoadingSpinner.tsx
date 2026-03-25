// src/components/LoadingSpinner/LoadingSpinner.tsx
import React from 'react';
import './LoadingSpinner.css';

interface LoadingSpinnerProps {
  message?: string;
}

const LoadingSpinner: React.FC<LoadingSpinnerProps> = ({ message = "Загрузка..." }) => {
  return (
    <div className="square-path-spinner-container"> {/* Сохраняем имя класса */}
      <div className="card-stack-container"> {/* Новый контейнер для стэка */}
        {/* 4 карточки стэка */}
        <div className="card-stack-item"></div> {/* Карточка 1 (верхняя) */}
        <div className="card-stack-item"></div> {/* Карточка 2 */}
        <div className="card-stack-item"></div> {/* Карточка 3 */}
        <div className="card-stack-item"></div> {/* Карточка 4 (нижняя) */}
      </div>
      <p className="spinner-message">{message}</p> {/* Сообщение без изменений */}
    </div>
  );
};

export default LoadingSpinner;