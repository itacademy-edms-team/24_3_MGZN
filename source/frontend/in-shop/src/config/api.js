export const API_BASE_URL = process.env.REACT_APP_API_BASE_URL || '/api';

export const API_ORIGIN = API_BASE_URL.endsWith('/api')
  ? API_BASE_URL.slice(0, -4) || ''
  : API_BASE_URL.replace(/\/$/, '');

export const resolveApiUrl = (path) => {
  const normalizedPath = path.startsWith('/') ? path : `/${path}`;
  return `${API_BASE_URL}${normalizedPath}`;
};

export const resolveAssetUrl = (path) => {
  if (!path) return null;
  if (path.startsWith('http://') || path.startsWith('https://')) return path;
  const normalizedPath = path.startsWith('/') ? path : `/${path}`;
  return `${API_ORIGIN}${normalizedPath}`;
};

export const PRODUCT_PLACEHOLDER_URL = resolveAssetUrl('/images/placeholder.svg');
