// src/components/ReviewForm.tsx

import React, { useState, useEffect } from 'react';
import { CreateReviewDto, UpdateReviewDto, Review } from '../types/review';
import StarRating from '../StarRating/StarRating.tsx';
import './ReviewForm.css';

interface ReviewFormProps {
  initialReview?: Review | null; // Если передан, значит режим редактирования
  onSubmit: (data: CreateReviewDto | UpdateReviewDto) => Promise<void>;
  onCancel: () => void;
  isLoading: boolean;
}

const ReviewForm: React.FC<ReviewFormProps> = ({ initialReview, onSubmit, onCancel, isLoading }) => {
  const [rating, setRating] = useState<number>(initialReview?.rating || 0);
  const [comment, setComment] = useState<string>(initialReview?.comment || '');
  const [error, setError] = useState<string>('');

  useEffect(() => {
    if (initialReview) {
      setRating(initialReview.rating);
      setComment(initialReview.comment);
    } else {
      setRating(0);
      setComment('');
    }
    setError('');
  }, [initialReview]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');

    if (rating === 0) {
      setError('Пожалуйста, поставьте оценку.');
      return;
    }
    if (comment.trim().length < 10) {
      setError('Комментарий должен содержать минимум 10 символов.');
      return;
    }

    const data = initialReview 
      ? { rating, comment } as UpdateReviewDto
      : { rating, comment } as CreateReviewDto;

    try {
      await onSubmit(data);
    } catch (err: any) {
      setError(err.response?.data?.message || 'Произошла ошибка при отправке отзыва.');
    }
  };

  return (
    <form onSubmit={handleSubmit} className="review-form">
      <h3>{initialReview ? 'Редактировать отзыв' : 'Написать отзыв'}</h3>
      
      <div className="form-group">
        <label>Ваша оценка:</label>
        <StarRating 
          rating={rating} 
          onSetRating={setRating} 
          size="large" 
        />
      </div>

      <div className="form-group">
        <label htmlFor="comment">Комментарий:</label>
        <textarea
          id="comment"
          value={comment}
          onChange={(e) => setComment(e.target.value)}
          rows={5}
          placeholder="Расскажите о вашем опыте использования товара..."
          maxLength={10000}
        />
        <small>{comment.length} / 10000</small>
      </div>

      {error && <div className="form-error">{error}</div>}

      <div className="form-actions">
        <button type="button" onClick={onCancel} disabled={isLoading}>Отмена</button>
        <button type="submit" disabled={isLoading}>
          {isLoading ? 'Отправка...' : (initialReview ? 'Сохранить' : 'Отправить')}
        </button>
      </div>
    </form>
  );
};

export default ReviewForm;