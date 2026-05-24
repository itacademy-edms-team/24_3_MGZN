/** Заказ в терминальном статусе — смена статуса запрещена в UI и на бэкенде. */
export const isTerminalOrderStatus = (status: string): boolean =>
  status === 'Cancelled' || status === 'Delivered';

/** Абсолютный URL для изображения товара (относительный путь из API). */
export const resolveProductImageUrl = (imageUrl?: string | null): string | null => {
  if (!imageUrl?.trim()) return null;
  if (imageUrl.startsWith('http://') || imageUrl.startsWith('https://')) return imageUrl;
  return `https://localhost:7275${imageUrl.startsWith('/') ? '' : '/'}${imageUrl}`;
};
