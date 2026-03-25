// src/utils/filters.ts
import { FiltersState } from '../types/search';

export const parseFiltersFromUrl = (searchParams: URLSearchParams): Partial<FiltersState> => {
  return {
    query: searchParams.get('q') || '',
    minPrice: searchParams.get('minPrice') || '',
    maxPrice: searchParams.get('maxPrice') || '',
    category: searchParams.get('category') || '',
    inStock: searchParams.get('inStock') === 'true' ? true : null,
  };
};

export const buildUrlFromFilters = (
  filters: FiltersState,
  sort: { option: string; order: 'asc' | 'desc' }
): string => {
  const params = new URLSearchParams();
  
  if (filters.query) {
    params.set('q', filters.query);
  }
  
  if (filters.minPrice) params.set('minPrice', filters.minPrice);
  if (filters.maxPrice) params.set('maxPrice', filters.maxPrice);
  if (filters.category) params.set('category', filters.category);
  if (filters.inStock) params.set('inStock', 'true');
  if (sort.option !== 'relevance') params.set('sort', sort.option);
  if (sort.order !== 'desc') params.set('order', sort.order);
  
  return params.toString() ? `?${params.toString()}` : '';
};

export const validateNumberRange = (min: string | null, max: string | null): { valid: boolean; error?: string } => {
  if (!min && !max) return { valid: true };
  
  const minNum = min ? parseFloat(min) : null;
  const maxNum = max ? parseFloat(max) : null;
  
  if (minNum !== null && isNaN(minNum)) return { valid: false, error: 'Мин. цена — некорректное число' };
  if (maxNum !== null && isNaN(maxNum)) return { valid: false, error: 'Макс. цена — некорректное число' };
  if (minNum !== null && maxNum !== null && minNum > maxNum) {
    return { valid: false, error: 'Мин. значение не может превышать макс.' };
  }
  
  return { valid: true };
};