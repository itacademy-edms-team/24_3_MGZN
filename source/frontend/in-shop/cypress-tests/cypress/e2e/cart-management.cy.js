describe('Корзина', () => {
  beforeEach(() => {
    cy.visit('/catalog');
  });

  it('добавляет товар в корзину и очищает её', () => {
    cy.contains('.category-card', /смартфоны/i).click();
    cy.get('.product-card').first().click();
    cy.get('[data-testid="add-to-cart-button"]').should('be.visible').click();

    cy.get('button[aria-label="Открыть корзину"]').click();
    cy.get('.cart-modal').should('be.visible');
    cy.get('.cart-item-card').should('have.length.at.least', 1);

    cy.get('[data-testid="clear-cart-button"]').click();
    cy.get('.empty-cart-message').should('contain', 'пуста');
  });
});
