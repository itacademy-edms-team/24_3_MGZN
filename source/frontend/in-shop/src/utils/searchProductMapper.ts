import { ProductSearchResultDto } from '../types/search';

/** Поля карточки товара для ProductCard */
export interface ProductCardModel {
  productId: number;
  productName: string;
  productPrice: number;
  imageUrl?: string;
  productStockQuantity?: number;
  productAvailability?: boolean;
  averageRating?: number;
  reviewsCount?: number;
}

const readField = <T,>(source: Record<string, unknown>, ...keys: string[]): T | undefined => {
  for (const key of keys) {
    const value = source[key];
    if (value !== undefined && value !== null) {
      return value as T;
    }
  }
  return undefined;
};

/** Нормализация DTO поиска (camelCase / PascalCase) */
export const normalizeSearchProduct = (raw: unknown): ProductSearchResultDto | null => {
  if (!raw || typeof raw !== 'object') return null;

  const item = raw as Record<string, unknown>;
  const id = Number(readField<number>(item, 'id', 'Id', 'productId', 'ProductId'));
  if (!Number.isFinite(id) || id <= 0) return null;

  return {
    id,
    name: String(readField<string>(item, 'name', 'Name', 'productName', 'ProductName') ?? ''),
    price: Number(readField<number>(item, 'price', 'Price', 'productPrice', 'ProductPrice') ?? 0),
    category: String(readField<string>(item, 'category', 'Category', 'productCategoryName', 'ProductCategoryName') ?? ''),
    description: String(readField<string>(item, 'description', 'Description') ?? ''),
    stockQuantity: Number(readField<number>(item, 'stockQuantity', 'StockQuantity', 'productStockQuantity', 'ProductStockQuantity') ?? 0),
    isAvailable: Boolean(readField<boolean>(item, 'isAvailable', 'IsAvailable', 'productAvailability', 'ProductAvailability') ?? true),
    imageUrl: String(readField<string>(item, 'imageUrl', 'ImageUrl', 'imageURL', 'ImageURL') ?? ''),
    averageRating: Number(readField<number>(item, 'averageRating', 'AverageRating') ?? 0),
    reviewsCount: Number(readField<number>(item, 'reviewsCount', 'ReviewsCount') ?? 0),
  };
};

export const normalizeSearchProductList = (items: unknown): ProductSearchResultDto[] => {
  if (!Array.isArray(items)) return [];
  return items.map(normalizeSearchProduct).filter((item): item is ProductSearchResultDto => item !== null);
};

export const toProductCardModel = (product: ProductSearchResultDto): ProductCardModel => ({
  productId: product.id,
  productName: product.name || 'Товар',
  productPrice: product.price,
  imageUrl: product.imageUrl,
  productStockQuantity: product.stockQuantity,
  productAvailability: product.isAvailable,
  averageRating: product.averageRating ?? 0,
  reviewsCount: product.reviewsCount ?? 0,
});

export const toProductCardModels = (products: ProductSearchResultDto[]): ProductCardModel[] =>
  products.map(toProductCardModel);
