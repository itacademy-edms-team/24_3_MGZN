// src/types/review.ts

export interface Review {
  reviewId: number;
  productId: number;
  sessionId: number;
  rating: number; // 1-5
  comment: string;
  createdAt: string; // ISO date string
  updatedAt?: string | null;
  voteScore: number; // Сумма голосов (Up - Down)
  userVote?: number | null; // 1, -1 или null (голос текущего пользователя)
  isVerifiedPurchase: boolean;
  isOwner: boolean;
}

export interface ReviewsResponse {
  reviews: Review[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface CreateReviewDto {
  rating: number;
  comment: string;
}

export interface UpdateReviewDto {
  rating: number;
  comment: string;
}

export interface VoteDto {
  voteType: number; // 1 или -1
}