import React from 'react';
import './LoadingSpinner.css';

const LoadingSpinner = ({ message = "Загрузка..." }) => {
  return (
    <div className="loading-container">
      <div className="loading-spinner"></div>
      <div className="loading-text">{message}</div>
    </div>
  );
};

export default LoadingSpinner;