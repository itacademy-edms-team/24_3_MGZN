export interface ReviewSummary {
  pros: string[];
  cons: string[];
  summary: string;
  ratingTrend: 'Positive' | 'Neutral' | 'Negative';
}