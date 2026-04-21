// src/components/ReviewItem.tsx

import React, { useState } from 'react';
import { Review, VoteDto } from '../../types/review.ts';
import { voteReview } from '../../api/reviews.ts';
import StarRating from '../StarRating/StarRating.tsx';
import './ReviewItem.css';

interface ReviewItemProps {
  review: Review;
  onVoteSuccess: () => void;
  onEdit?: (review: Review) => void;
  onDelete?: (reviewId: number) => void;
}

const ReviewItem: React.FC<ReviewItemProps> = ({ 
  review, 
  onVoteSuccess, 
  onEdit, 
  onDelete
}) => {
  const [isVoting, setIsVoting] = useState(false);

  const handleVote = async (voteType: number) => {
    if (isVoting) return;
    setIsVoting(true);
    try {
      await voteReview(review.reviewId, { voteType });
      onVoteSuccess();
    } catch (error) {
      console.error('Ошибка голосования:', error);
    } finally {
      setIsVoting(false);
    }
  };

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString('ru-RU', {
      year: 'numeric',
      month: 'long',
      day: 'numeric'
    });
  };

  return (
    <div className="review-item">
      <div className="review-header">
        <div className="review-author">
          <span className="author-name">Пользователь #{review.sessionId}</span>

           {review.isVerifiedPurchase ? (
            <span className="verified-badge">✓ Проверенный покупатель</span>
          ) : (
            <span className="unverified-badge">✗ Не покупал товар</span>
          )}
        </div>
        
        <span className="review-date">{formatDate(review.createdAt)}</span>
      </div>

      <div className="review-content">
        <StarRating rating={review.rating} readOnly size="small" />
        <p className="review-text">{review.comment}</p>
      </div>

      <div className="review-footer">
        <div className="vote-section">
          <span className="vote-score">Полезно: {review.voteScore}</span>
          <button 
            onClick={() => handleVote(1)} 
            disabled={isVoting || review.userVote === 1}
            className={`vote-btn up ${review.userVote === 1 ? 'active' : ''}`}
          >
            👍
          </button>
          <button 
            onClick={() => handleVote(-1)} 
            disabled={isVoting || review.userVote === -1}
            className={`vote-btn down ${review.userVote === -1 ? 'active' : ''}`}
          >
            👎
          </button>
        </div>

        {/* Показываем кнопки только если бэкенд сказал, что это наш отзыв */}
        {review.isOwner && (
          <div className="review-actions">
            {onEdit && (
              <button className="action-btn edit" onClick={() => onEdit(review)}>
                Редактировать
              </button>
            )}
            {onDelete && (
              <button className="action-btn delete" onClick={() => onDelete(review.reviewId)}>
                Удалить
              </button>
            )}
          </div>
        )}
      </div>
    </div>
  );
};

export default ReviewItem;