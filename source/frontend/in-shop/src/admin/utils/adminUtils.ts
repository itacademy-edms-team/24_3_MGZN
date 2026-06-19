import { resolveAssetUrl } from '../../config/api.js';

/** Заказ в терминальном статусе — смена статуса запрещена в UI и на бэкенде. */
export const isTerminalOrderStatus = (status: string): boolean =>
  status === 'Cancelled' || status === 'Delivered';

/** Самовывоз — транспортная компания не используется (как в CheckoutForm). */
export const isPickupShipMethod = (shipMethod?: string | null): boolean =>
  shipMethod?.trim().toLowerCase() === 'самовывоз';

/** Абсолютный URL для изображения товара (относительный путь из API). */
export const resolveProductImageUrl = (imageUrl?: string | null): string | null => {
  if (!imageUrl?.trim()) return null;
  return resolveAssetUrl(imageUrl);
};
