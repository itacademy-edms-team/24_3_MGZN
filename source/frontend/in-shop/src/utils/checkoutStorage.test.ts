import {
  clearCheckoutStorage,
  readCheckoutDraft,
  readCompletedOrder,
  saveCheckoutDraft,
  saveCompletedOrder,
} from './checkoutStorage';

describe('checkoutStorage', () => {
  beforeEach(() => {
    sessionStorage.clear();
    localStorage.clear();
  });

  it('stores checkout data in sessionStorage and clears legacy localStorage keys', () => {
    saveCheckoutDraft({ customerEmail: 'buyer@example.com' });
    saveCompletedOrder({ orderId: 42 });
    localStorage.setItem('orderData', '{"legacy":true}');
    localStorage.setItem('completedOrderId', '42');

    expect(readCheckoutDraft()).toEqual({ customerEmail: 'buyer@example.com' });
    expect(readCompletedOrder()).toEqual({ orderId: 42 });

    clearCheckoutStorage();

    expect(readCheckoutDraft()).toBeNull();
    expect(readCompletedOrder()).toBeNull();
    expect(localStorage.getItem('orderData')).toBeNull();
    expect(localStorage.getItem('completedOrderId')).toBeNull();
  });

  it('drops corrupted storage values instead of throwing', () => {
    sessionStorage.setItem('checkoutDraft', '{broken');

    expect(readCheckoutDraft()).toBeNull();
    expect(sessionStorage.getItem('checkoutDraft')).toBeNull();
  });
});
