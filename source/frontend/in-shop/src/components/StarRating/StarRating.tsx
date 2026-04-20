// src/components/StarRating.tsx

import React from 'react';
import './StarRating.css';

interface StarRatingProps {
  rating: number; // Текущий рейтинг (0-5)
  maxRating?: number;
  onSetRating?: (rating: number) => void; // Если есть, значит режим ввода
  readOnly?: boolean;
  size?: 'small' | 'medium' | 'large';
}

const StarRating: React.FC<StarRatingProps> = ({ 
  rating, 
  maxRating = 5, 
  onSetRating, 
  readOnly = false,
  size = 'medium'
}) => {
  const [hoverRating, setHoverRating] = React.useState<number>(0);

  const isInteractive = !!onSetRating && !readOnly;

  return (
    <div className={`star-rating ${size}`}>
      {[...Array(maxRating)].map((_, index) => {
        const starValue = index + 1;
        const isFilled = isInteractive 
          ? starValue <= (hoverRating || rating) 
          : starValue <= rating;

        return (
          <span
            key={index}
            className={`star ${isFilled ? 'filled' : 'empty'}`}
            onClick={() => isInteractive && onSetRating?.(starValue)}
            onMouseEnter={() => isInteractive && setHoverRating(starValue)}
            onMouseLeave={() => isInteractive && setHoverRating(0)}
            style={{ cursor: isInteractive ? 'pointer' : 'default' }}
          >
            ★
          </span>
        );
      })}
    </div>
  );
};

export default StarRating;