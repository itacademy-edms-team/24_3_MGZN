const DEFAULT_ALLOWED_PAYMENT_HOSTS = [
  'yookassa.ru',
  'yoomoney.ru',
];

const getAllowedPaymentHosts = (): string[] => {
  const configuredHosts = process.env.REACT_APP_ALLOWED_PAYMENT_REDIRECT_HOSTS;
  if (!configuredHosts) return DEFAULT_ALLOWED_PAYMENT_HOSTS;

  return configuredHosts
    .split(',')
    .map((host) => host.trim().toLowerCase())
    .filter(Boolean);
};

export const isAllowedPaymentRedirectUrl = (redirectUrl: string): boolean => {
  try {
    const parsedUrl = new URL(redirectUrl);
    const hostname = parsedUrl.hostname.toLowerCase();

    if (process.env.NODE_ENV === 'development' && ['localhost', '127.0.0.1'].includes(hostname)) {
      return true;
    }

    if (parsedUrl.protocol !== 'https:') {
      return false;
    }

    return getAllowedPaymentHosts().some(
      (allowedHost) => hostname === allowedHost || hostname.endsWith(`.${allowedHost}`)
    );
  } catch {
    return false;
  }
};

export const redirectToPaymentProvider = (redirectUrl: string): void => {
  if (!isAllowedPaymentRedirectUrl(redirectUrl)) {
    throw new Error('Сервер вернул недопустимую ссылку на оплату.');
  }

  window.location.assign(redirectUrl);
};
