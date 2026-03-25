// src/hooks/useProductSearch.ts
import { useState, useCallback } from 'react';
import { ProductSearchResultDto, SearchRequestDto } from '../types/search';

// Вспомогательный тип для формата, который ожидает бэкенд (camelCase ключи)
interface BackendSearchRequest {
  q: string;
  limit: number;
  category: string | null;
  minPrice: number | null;
  maxPrice: number | null;
  inStock: boolean | null;
  sortBy: string;
  sortOrder: 'asc' | 'desc';
  specFilters: Record<string, any> | null;
}

// Нормализация specFilters: удаляем пустые/невалидные значения
const normalizeSpecFilters = (
  filters: Record<string, any> | null | undefined
): Record<string, any> | null => {
  if (!filters || typeof filters !== 'object') return null;
  
  const result: Record<string, any> = {};
  
  for (const [key, value] of Object.entries(filters)) {
    if (value == null) continue;
    
    // Обработка Number-диапазона { Min, Max }
    if (typeof value === 'object' && value !== null && !Array.isArray(value)) {
      const min = value.Min != null && value.Min !== '' 
        ? parseFloat(String(value.Min)) 
        : null;
      const max = value.Max != null && value.Max !== '' 
        ? parseFloat(String(value.Max)) 
        : null;
      
      // Пропускаем, если оба значения невалидны
      if (min === null && max === null) continue;
      
      result[key] = {
        ...(min !== null && { Min: min }),
        ...(max !== null && { Max: max }),
      };
    } 
    // Обработка Text-значения
    else if (typeof value === 'string') {
      const trimmed = value.trim();
      if (trimmed !== '') {
        result[key] = trimmed;
      }
    } 
    // Числовое значение (если вдруг пришло числом)
    else if (typeof value === 'number' && !isNaN(value)) {
      result[key] = value;
    }
    // Игнорируем остальные типы
  }
  
  return Object.keys(result).length > 0 ? result : null;
};

// Преобразование внутреннего DTO в формат бэкенда (camelCase ключи)
const toBackendRequest = (request: SearchRequestDto): BackendSearchRequest => ({
  q: request.query ?? '',  // ← Ключ "q", как в [JsonPropertyName("q")]
  limit: request.limit ?? 50,
  category: request.category ?? null,
  minPrice: request.minPrice != null 
    ? parseFloat(String(request.minPrice)) 
    : null,
  maxPrice: request.maxPrice != null 
    ? parseFloat(String(request.maxPrice)) 
    : null,
  inStock: typeof request.inStock === 'boolean' ? request.inStock : null,
  sortBy: request.sortBy ?? 'relevance',
  sortOrder: request.sortOrder === 'asc' ? 'asc' : 'desc',
  specFilters: normalizeSpecFilters(request.specFilters),
});

interface UseProductSearchReturn {
  results: ProductSearchResultDto[];
  loading: boolean;
  error: string | null;
  search: (request: SearchRequestDto) => Promise<void>;
  clear: () => void;
}

export const useProductSearch = (apiBaseUrl: string): UseProductSearchReturn => {
  const [results, setResults] = useState<ProductSearchResultDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const search = useCallback(async (request: SearchRequestDto) => {
    setLoading(true);
    setError(null);
    
    try {
      // Преобразуем запрос в формат, который ожидает бэкенд
      const backendRequest = toBackendRequest(request);
      
      // Лог для отладки (можно убрать в продакшене)
      console.log('📤 Search request to backend:', backendRequest);

      const response = await fetch(`${apiBaseUrl}/search/search`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(backendRequest), // ← Отправляем с camelCase ключами
      });

      if (!response.ok) {
        let errorMsg = `Ошибка сервера: ${response.status}`;
        
        try {
          // Пытаемся распарсить детали ошибки
          const errorData = await response.json();
          console.error('❌ Backend error response:', errorData);
          
          // Приоритет полей для сообщения об ошибке
          errorMsg = errorData?.detail 
            || errorData?.message 
            || errorData?.title 
            || errorData?.errors 
            || JSON.stringify(errorData)
            || errorMsg;
        } catch (parseError) {
          // Если тело не JSON, пробуем прочитать как текст
          try {
            const text = await response.text();
            if (text) {
              console.error('❌ Backend error text:', text);
              errorMsg = text;
            }
          } catch {}
        }
        
        throw new Error(errorMsg);
      }

      const data = await response.json();
      
      // Гарантируем, что результаты — это массив
      setResults(Array.isArray(data) ? data : []);
      
    } catch (e) {
      console.error('🔥 Search error:', e);
      setError(e instanceof Error ? e.message : 'Неизвестная ошибка при поиске');
      setResults([]);
    } finally {
      setLoading(false);
    }
  }, [apiBaseUrl]);

  const clear = useCallback(() => {
    setResults([]);
    setError(null);
    setLoading(false);
  }, []);

  return { results, loading, error, search, clear };
};