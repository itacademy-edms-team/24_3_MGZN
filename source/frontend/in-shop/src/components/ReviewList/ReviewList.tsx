// src/components/ReviewList.tsx

import React, { useState, useEffect } from 'react';
import { Review, ReviewsResponse } from '../../types/review.ts';
import { getProductReviews } from '../../api/reviews.ts';
import ReviewItem from '../ReviewItem/ReviewItem.tsx';
import LoadingSpinner from '../LoadingSpinner.tsx';
import './ReviewList.css';

interface ReviewListProps {
  productId: number;
  onRefreshTrigger: number;
  onEdit: (review: Review) => void;
  onDelete?: (reviewId: number) => void;
  onTotalCountChange?: (count: number) => void;
}

const ReviewList: React.FC<ReviewListProps> = ({ 
  productId, 
  onRefreshTrigger, 
  onEdit, 
  onDelete,
  onTotalCountChange
}) => {
  const [reviews, setReviews] = useState<Review[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);
  const [loading, setLoading] = useState(false);
  const [hasMore, setHasMore] = useState(true);

  const pageSize = 5;

  const loadReviews = async (pageNum: number, append: boolean = false) => {
    if (loading) return;
    setLoading(true);
    try {
      const data: ReviewsResponse = await getProductReviews(productId, pageNum, pageSize);
      
      if (append) {
        setReviews(prev => [...prev, ...data.reviews]);
      } else {
        setReviews(data.reviews);
      }
      
      setTotalCount(data.totalCount);
      onTotalCountChange?.(data.totalCount);
      setHasMore(data.reviews.length > 0 && (pageNum * pageSize) < data.totalCount);
    } catch (error) {
      console.error('Ошибка загрузки отзывов:', error);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    setPage(1);
    loadReviews(1, false);
  }, [productId, onRefreshTrigger]);

  useEffect(() => {
    if (page > 1) {
      loadReviews(page, true);
    }
  }, [page]);

  const handleLoadMore = () => {
    setPage(prev => prev + 1);
  };

  const handleVoteSuccess = async () => {
    if (loading) return;
    setLoading(true);
    try {
      const loadedPageCount = Math.max(page, 1);
      const loadedItemsCount = loadedPageCount * pageSize;
      const data: ReviewsResponse = await getProductReviews(productId, 1, loadedItemsCount);

      setReviews(data.reviews);
      setTotalCount(data.totalCount);
      onTotalCountChange?.(data.totalCount);
      setHasMore(data.reviews.length > 0 && data.reviews.length < data.totalCount);
    } catch (error) {
      console.error('Ошибка обновления отзывов после голосования:', error);
    } finally {
      setLoading(false);
    }
  };

  const handleLocalDelete = async (reviewId: number) => {
    if (onDelete) {
      await onDelete(reviewId);
    }
  };

  if (loading && reviews.length === 0) {
    return <LoadingSpinner message="Загрузка отзывов..." />;
  }

  return (
    <div className="review-list-container">
      <h3>Отзывы ({totalCount})</h3>
      
      {reviews.length === 0 ? (
        <p className="no-reviews">Пока нет отзывов. Будьте первым!</p>
      ) : (
        <>
          <div className="reviews-wrapper">
            {reviews.map(review => (
              <ReviewItem 
                key={review.reviewId} 
                review={review} 
                onVoteSuccess={handleVoteSuccess}
                onEdit={onEdit}
                onDelete={handleLocalDelete}
                // currentSessionId больше не передается, проверка прав на бэкенде
              />
            ))}
          </div>
          
          {hasMore && (
            <div className="load-more-container">
              <button onClick={handleLoadMore} disabled={loading}>
                {loading ? 'Загрузка...' : 'Показать еще'}
              </button>
            </div>
          )}
        </>
      )}
    </div>
  );
};

export default ReviewList;