import { isAllowedPaymentRedirectUrl } from './paymentRedirect';

describe('paymentRedirect', () => {
  it('allows configured payment provider HTTPS URLs', () => {
    expect(isAllowedPaymentRedirectUrl('https://yookassa.ru/payments/123')).toBe(true);
    expect(isAllowedPaymentRedirectUrl('https://checkout.yoomoney.ru/payments/123')).toBe(true);
  });

  it('rejects unknown, malformed, and non-HTTPS redirect URLs', () => {
    expect(isAllowedPaymentRedirectUrl('https://evil.example/pay')).toBe(false);
    expect(isAllowedPaymentRedirectUrl('http://yookassa.ru/payments/123')).toBe(false);
    expect(isAllowedPaymentRedirectUrl('not-a-url')).toBe(false);
  });
});
