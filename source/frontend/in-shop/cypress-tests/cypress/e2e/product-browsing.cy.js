describe('Каталог и карточка товара', () => {
  it('открывает каталог и страницу товара', () => {
    cy.visit('/');
    cy.get('a[href="/catalog"]').click();
    cy.url().should('include', '/catalog');

    cy.get('a[href*="/product/"]').first().click();
    cy.get('.product-price').should('be.visible');
    cy.get('[data-testid="add-to-cart-button"]').should('exist');
  });
});
