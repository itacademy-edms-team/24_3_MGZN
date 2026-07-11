const CHECKOUT_DRAFT_KEY = 'checkoutDraft';
const COMPLETED_ORDER_KEY = 'completedOrder';
const CHECKOUT_STORAGE_TTL_MS = 60 * 60 * 1000;

interface StoredValue<T> {
  expiresAt: number;
  value: T;
}

const readStoredValue = <T>(key: string): T | null => {
  try {
    const raw = sessionStorage.getItem(key);
    if (!raw) return null;

    const parsed = JSON.parse(raw) as StoredValue<T>;
    if (!parsed || typeof parsed.expiresAt !== 'number' || parsed.expiresAt < Date.now()) {
      sessionStorage.removeItem(key);
      return null;
    }

    return parsed.value ?? null;
  } catch {
    sessionStorage.removeItem(key);
    return null;
  }
};

const writeStoredValue = <T>(key: string, value: T): void => {
  const payload: StoredValue<T> = {
    expiresAt: Date.now() + CHECKOUT_STORAGE_TTL_MS,
    value,
  };
  sessionStorage.setItem(key, JSON.stringify(payload));
};

export const saveCheckoutDraft = <T>(draft: T): void => {
  writeStoredValue(CHECKOUT_DRAFT_KEY, draft);
};

export const readCheckoutDraft = <T>(): T | null => readStoredValue<T>(CHECKOUT_DRAFT_KEY);

export const saveCompletedOrder = <T>(order: T): void => {
  writeStoredValue(COMPLETED_ORDER_KEY, order);
};

export const readCompletedOrder = <T>(): T | null => readStoredValue<T>(COMPLETED_ORDER_KEY);

export const clearCheckoutStorage = (): void => {
  sessionStorage.removeItem(CHECKOUT_DRAFT_KEY);
  sessionStorage.removeItem(COMPLETED_ORDER_KEY);
  localStorage.removeItem('orderData');
  localStorage.removeItem('completedOrderId');
};
