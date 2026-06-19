describe('Корзина', () => {
  beforeEach(() => {
    cy.visit('/catalog');
  });

  it('добавляет товар в корзину и очищает её', () => {
    cy.get('a[href*="/product/"]').first().click();
    cy.get('[data-testid="add-to-cart-button"]').should('be.visible').click();

    cy.get('img[alt="Корзина"]').click();
    cy.get('.cart-modal').should('be.visible');
    cy.get('.cart-item-card').should('have.length.at.least', 1);

    cy.get('[data-testid="clear-cart-button"]').click();
    cy.get('.empty-cart-message').should('contain', 'пуста');
  });
});
