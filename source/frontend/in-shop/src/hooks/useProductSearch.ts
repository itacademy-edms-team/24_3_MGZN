// src/hooks/useProductSearch.ts
import { useState, useCallback, useRef } from 'react';
import { ProductSearchResultDto, SearchRequestDto, SearchResponseDto } from '../types/search';

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

export const useProductSearch = (apiBaseUrl: string): UseProductSearchReturn => {
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
      console.log('📤 Search request:', backendRequest, append ? '(APPEND)' : '(REPLACE)');

      const response = await fetch(`${apiBaseUrl}/search/search`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(backendRequest),
        signal: abortController.signal,
      });

      if (currentRequestId !== requestIdRef.current) {
        console.log('🔄 Request cancelled, ignoring response');
        return;
      }

      if (!response.ok) throw new Error(`Ошибка сервера: ${response.status}`);

      const data: SearchResponseDto = await response.json();
      const newResults = Array.isArray(data.results) ? data.results : [];
      const newRecommended = Array.isArray(data.recommended) ? data.recommended : [];

      if (!append) {
        setResults(newResults);
        setRecommended(newRecommended);
      } else {
        setResults(prev => [...prev, ...newResults]);
      }

      setHasMore(newResults.length >= (request.limit || 12));

    } catch (e) {
      if (e instanceof Error && e.name === 'AbortError') {
        console.log('Request aborted');
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
  }, [apiBaseUrl]);

  const loadMore = useCallback(async (request: SearchRequestDto) => {
    const currentRequestId = ++requestIdRef.current;
    const abortController = new AbortController();
    abortControllerRef.current = abortController;
    
    setLoading(true);
    setError(null);
    
    try {
      const backendRequest = toBackendRequest(request);
      console.log('📤 LoadMore request:', backendRequest);

      const response = await fetch(`${apiBaseUrl}/search/search`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(backendRequest),
        signal: abortController.signal,
      });

      if (currentRequestId !== requestIdRef.current) {
        console.log('🔄 LoadMore request cancelled');
        return;
      }

      if (!response.ok) throw new Error(`Ошибка сервера: ${response.status}`);

      const data: SearchResponseDto = await response.json();
      const newResults = Array.isArray(data.results) ? data.results : [];

      setResults(prev => [...prev, ...newResults]);
      setHasMore(newResults.length >= (request.limit || 12));

    } catch (e) {
      if (e instanceof Error && e.name === 'AbortError') {
        console.log('LoadMore request aborted');
        return;
      }
      
      console.error('🔥 LoadMore error:', e);
      setError(e instanceof Error ? e.message : 'Ошибка');
    } finally {
      if (currentRequestId === requestIdRef.current) {
        setLoading(false);
      }
    }
  }, [apiBaseUrl]);

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