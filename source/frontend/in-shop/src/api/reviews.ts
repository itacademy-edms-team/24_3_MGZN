// src/api/reviews.ts

import apiClient from './client.ts';
import { 
  ReviewsResponse, 
  CreateReviewDto, 
  UpdateReviewDto, 
  VoteDto 
} from '../types/review';

const REVIEWS_BASE_URL = '/Products'; // Так как эндпоинты привязаны к ProductsController

// Получить отзывы товара
export const getProductReviews = async (
  productId: number, 
  page: number = 1, 
  pageSize: number = 10
): Promise<ReviewsResponse> => {
  const response = await apiClient.get<ReviewsResponse>(`${REVIEWS_BASE_URL}/${productId}/reviews`, {
    params: { page, pageSize }
  });
  return response.data;
};

// Создать отзыв
export const createReview = async (
  productId: number, 
  dto: CreateReviewDto
): Promise<any> => {
  const response = await apiClient.post(`${REVIEWS_BASE_URL}/${productId}/reviews`, dto);
  return response.data;
};

// Обновить отзыв
export const updateReview = async (
  reviewId: number, 
  dto: UpdateReviewDto
): Promise<any> => {
  const response = await apiClient.put(`${REVIEWS_BASE_URL}/reviews/${reviewId}`, dto);
  return response.data;
};

// Удалить отзыв
export const deleteReview = async (reviewId: number): Promise<void> => {
  await apiClient.delete(`${REVIEWS_BASE_URL}/reviews/${reviewId}`);
};

// Проголосовать за отзыв
export const voteReview = async (
  reviewId: number, 
  dto: VoteDto
): Promise<void> => {
  await apiClient.post(`${REVIEWS_BASE_URL}/reviews/${reviewId}/vote`, dto);
};