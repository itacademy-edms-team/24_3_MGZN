// src/components/StarRating.tsx

import React from 'react';
import './StarRating.css';

interface StarRatingProps {
  rating: number;
  maxRating?: number;
  onSetRating?: (rating: number) => void;
  readOnly?: boolean;
  size?: 'small' | 'medium' | 'large';
}

const StarRating: React.FC<StarRatingProps> = ({
  rating,
  maxRating = 5,
  onSetRating,
  readOnly = false,
  size = 'medium',
}) => {
  const [hoverRating, setHoverRating] = React.useState<number>(0);
  const isInteractive = !!onSetRating && !readOnly;
  const displayRating = isInteractive ? hoverRating || rating : rating;

  return (
    <div
      className={`star-rating ${size}`}
      role="img"
      aria-label={`Рейтинг ${rating.toFixed(1)} из ${maxRating}`}
    >
      {Array.from({ length: maxRating }, (_, index) => {
        const starValue = index + 1;
        const fillRatio = Math.min(1, Math.max(0, displayRating - index));
        const fillPercent = Math.round(fillRatio * 100);

        if (isInteractive) {
          const isFilled = starValue <= displayRating;
          return (
            <span
              key={index}
              className={`star ${isFilled ? 'filled' : 'empty'}`}
              onClick={() => onSetRating?.(starValue)}
              onMouseEnter={() => setHoverRating(starValue)}
              onMouseLeave={() => setHoverRating(0)}
              style={{ cursor: 'pointer' }}
            >
              ★
            </span>
          );
        }

        return (
          <span key={index} className="star star--progress">
            <span className="star__base" aria-hidden>
              ★
            </span>
            <span
              className="star__fill"
              style={{ width: `${fillPercent}%` }}
              aria-hidden
            >
              ★
            </span>
          </span>
        );
      })}
    </div>
  );
};

export default StarRating;
