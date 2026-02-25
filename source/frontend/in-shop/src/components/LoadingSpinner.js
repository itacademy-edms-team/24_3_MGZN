import React from 'react';
import './LoadingSpinner.css';

const LoadingSpinner = ({ message = "Загрузка..." }) => {
  return (
      <div className="modern-spinner-container">
        <div className="spinner-wrapper">
          <div className="spinner-glow"></div>
          <div className="spinner-ring">
            <div className="ring-spin"></div>
          </div>
        </div>
        <p className="spinner-message">{message}</p>
      </div>
    );
};

export default LoadingSpinner;