// src/types/search.ts

export interface ProductSearchResultDto {
  id: number;
  name: string;
  price: number;
  category: string;
  description: string;
  stockQuantity: number;
  isAvailable: boolean;
  imageUrl: string;
}

export interface CategoryDto {
  categoryId: number;
  categoryName: string;
  imageURL?: string;
}

export interface FiltersState {
  query: string;
  minPrice: string;
  maxPrice: string;
  category: string;
  inStock: boolean | null;
}

export interface SearchResponseDto {
  results: ProductSearchResultDto[];
  recommended: ProductSearchResultDto[];
}

export interface SearchRequestDto {
  query: string;
  limit: number; // Сколько товаров подгрузить за раз (например, 12)
  offset: number; // Смещение (сколько уже есть)
  category: string | null;
  minPrice: number | null;
  maxPrice: number | null;
  inStock: boolean | null;
  specFilters: Record<string, any> | null;
  sortBy: string;
  sortOrder: 'asc' | 'desc';
}

export interface SpecificationFilterDto {
  specId: number;
  name: string;
  displayName: string;
  dataType: 'Text' | 'Number';
  possibleValues?: string[];
}