describe('template spec', () => {
  it('passes', () => {
    cy.visit('http://localhost:3000/catalog')
    cy.get('#root a[href="/category/%D0%9F%D0%BB%D0%B0%D0%BD%D1%88%D0%B5%D1%82%D1%8B"]').click();
    cy.get('#root img[alt="Ipad Mini 2023"]').click();
    cy.get('#root button.add-to-cart-button').click();
    cy.get('#root main').click();
    cy.get('#root a[href="/catalog"]').click();
    cy.get('#root img[alt="Ноутбуки"]').click();
    cy.get('#root a[href="/product/21"] div.product-content').click();
    cy.get('#root button.add-to-cart-button').click();
    cy.get('#root a[href="/catalog"]').click();
    cy.get('#root a[href="http://localhost:3000/category/Аудио"]').click();
    cy.get('#root a[href="/product/8"] div.product-info').click();
    cy.get('#root button.add-to-cart-button').click();
    cy.get('#root img[alt="Корзина"]').click();
    cy.get('#root div:nth-child(3) div.quantity-controls button:nth-child(3)').click();
    cy.get('#root div:nth-child(3) div.quantity-controls button:nth-child(3)').click();
    cy.get('#root div:nth-child(3) div.quantity-controls button:nth-child(1)').click();
    cy.get('#root div:nth-child(3) button.remove-btn').click();
    cy.get('#root button.clear-cart-button').click();
    cy.get('#root div.cart-modal-overlay').click();
  })
})