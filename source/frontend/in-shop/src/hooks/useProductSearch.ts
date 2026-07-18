// src/hooks/useProductSearch.ts
import { useState, useCallback, useRef } from 'react';
import axios from 'axios';
import { apiClient } from '../api/client';
import { ProductSearchResultDto, SearchRequestDto } from '../types/search';
import { normalizeSearchProductList } from '../utils/searchProductMapper';

const isDevelopment = process.env.NODE_ENV === 'development';

const isRequestCanceled = (error: unknown): boolean => {
  if (axios.isCancel(error)) return true;
  if (!error || typeof error !== 'object') return false;
  const err = error as { name?: string; code?: string };
  return err.name === 'AbortError' || err.name === 'CanceledError' || err.code === 'ERR_CANCELED';
};

interface BackendSearchRequest {
  q: string;
  limit: number;
  offset: number;
  category: string | null;
  minPrice: number | null;
  maxPrice: number | null;
  inStock: boolean | null;
  sortBy: string;
  sortOrder: 'asc' | 'desc';
  specFilters: Record<string, any> | null;
}

const normalizeSpecFilters = (filters: Record<string, any> | null | undefined): Record<string, any> | null => {
  if (!filters || typeof filters !== 'object') return null;
  const result: Record<string, any> = {};
  for (const [key, value] of Object.entries(filters)) {
    if (value == null) continue;
    if (typeof value === 'object' && value !== null && !Array.isArray(value)) {
      const min = value.Min != null && value.Min !== '' ? parseFloat(String(value.Min)) : null;
      const max = value.Max != null && value.Max !== '' ? parseFloat(String(value.Max)) : null;
      if (min === null && max === null) continue;
      result[key] = { ...(min !== null && { Min: min }), ...(max !== null && { Max: max }) };
    } else if (typeof value === 'string') {
      const trimmed = value.trim();
      if (trimmed !== '') result[key] = trimmed;
    } else if (typeof value === 'number' && !isNaN(value)) {
      result[key] = value;
    }
  }
  return Object.keys(result).length > 0 ? result : null;
};

const toBackendRequest = (request: SearchRequestDto): BackendSearchRequest => ({
  q: request.query ?? '',
  limit: request.limit ?? 12,
  offset: request.offset ?? 0,
  category: request.category ?? null,
  minPrice: request.minPrice != null ? parseFloat(String(request.minPrice)) : null,
  maxPrice: request.maxPrice != null ? parseFloat(String(request.maxPrice)) : null,
  inStock: typeof request.inStock === 'boolean' ? request.inStock : null,
  sortBy: request.sortBy ?? 'relevance',
  sortOrder: request.sortOrder === 'asc' ? 'asc' : 'desc',
  specFilters: normalizeSpecFilters(request.specFilters),
});

interface UseProductSearchReturn {
  results: ProductSearchResultDto[];
  recommended: ProductSearchResultDto[];
  loading: boolean;
  error: string | null;
  hasMore: boolean;
  search: (request: SearchRequestDto, append?: boolean) => Promise<void>;
  loadMore: (request: SearchRequestDto) => Promise<void>;
  clear: () => void;
}

export const useProductSearch = (): UseProductSearchReturn => {
  const [results, setResults] = useState<ProductSearchResultDto[]>([]);
  const [recommended, setRecommended] = useState<ProductSearchResultDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [hasMore, setHasMore] = useState(true);
  
  const abortControllerRef = useRef<AbortController | null>(null);
  const requestIdRef = useRef<number>(0);

  const search = useCallback(async (request: SearchRequestDto, append: boolean = false) => {
    if (abortControllerRef.current) {
      abortControllerRef.current.abort();
    }
    
    const currentRequestId = ++requestIdRef.current;
    const abortController = new AbortController();
    abortControllerRef.current = abortController;
    
    setLoading(true);
    setError(null);
    
    try {
      const backendRequest = toBackendRequest(request);
      if (isDevelopment) {
        console.log('Search request:', backendRequest, append ? '(APPEND)' : '(REPLACE)');
      }

      const response = await apiClient.post('/search/search', backendRequest, {
        signal: abortController.signal,
      });

      if (currentRequestId !== requestIdRef.current) {
        if (isDevelopment) {
          console.log('Request cancelled, ignoring response');
        }
        return;
      }

      const data = response.data;
      const newResults = normalizeSearchProductList(
        Array.isArray(data.results)
          ? data.results
          : Array.isArray(data.Results)
            ? data.Results
            : []
      );
      const newRecommended = normalizeSearchProductList(
        Array.isArray(data.recommended)
          ? data.recommended
          : Array.isArray(data.Recommended)
            ? data.Recommended
            : []
      );

      if (!append) {
        setResults(newResults);
        setRecommended(newRecommended);
      } else {
        setResults(prev => [...prev, ...newResults]);
      }

      setHasMore(newResults.length >= (request.limit || 12));

    } catch (e) {
      if (isRequestCanceled(e)) {
        if (isDevelopment) {
          console.log('Request aborted');
        }
        return;
      }
      
      console.error('🔥 Search error:', e);
      setError(e instanceof Error ? e.message : 'Ошибка');
      if (!append) {
        setResults([]);
        setRecommended([]);
      }
    } finally {
      if (currentRequestId === requestIdRef.current) {
        setLoading(false);
      }
    }
  }, []);

  const loadMore = useCallback(async (request: SearchRequestDto) => {
    // Не абортим текущий REPLACE-поиск: loadMore — отдельный запрос.
    // Но если уже идёт другой loadMore/search — отменяем предыдущий.
    if (abortControllerRef.current) {
      abortControllerRef.current.abort();
    }

    const currentRequestId = ++requestIdRef.current;
    const abortController = new AbortController();
    abortControllerRef.current = abortController;
    
    setLoading(true);
    setError(null);
    
    try {
      const backendRequest = toBackendRequest(request);
      if (isDevelopment) {
        console.log('LoadMore request:', backendRequest);
      }

      const response = await apiClient.post('/search/search', backendRequest, {
        signal: abortController.signal,
      });

      if (currentRequestId !== requestIdRef.current) {
        if (isDevelopment) {
          console.log('LoadMore request cancelled');
        }
        return;
      }

      const data = response.data;
      const newResults = normalizeSearchProductList(
        Array.isArray(data.results)
          ? data.results
          : Array.isArray(data.Results)
            ? data.Results
            : []
      );

      setResults(prev => [...prev, ...newResults]);
      setHasMore(newResults.length >= (request.limit || 12));

    } catch (e) {
      if (isRequestCanceled(e)) {
        if (isDevelopment) {
          console.log('LoadMore request aborted');
        }
        return;
      }
      
      console.error('🔥 LoadMore error:', e);
      setError(e instanceof Error ? e.message : 'Ошибка');
    } finally {
      if (currentRequestId === requestIdRef.current) {
        setLoading(false);
      }
    }
  }, []);

  const clear = useCallback(() => {
    if (abortControllerRef.current) {
      abortControllerRef.current.abort();
    }
    setResults([]);
    setRecommended([]);
    setError(null);
    setLoading(false);
    setHasMore(true);
  }, []);

  return { results, recommended, loading, error, hasMore, search, loadMore, clear };
};