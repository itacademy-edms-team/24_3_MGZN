describe('Каталог и карточка товара', () => {
  it('открывает каталог и страницу товара', () => {
    cy.visit('/');
    cy.contains('.category-card', /смартфоны/i).click();
    cy.url().should('include', '/category/');

    cy.get('.product-card').first().click();
    cy.url().should('include', '/product/');
    cy.get('.product-price').should('be.visible');
    cy.get('[data-testid="add-to-cart-button"]').should('exist');
  });
});
