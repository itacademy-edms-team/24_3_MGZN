describe('template spec', () => {
  it('passes', () => {
    cy.visit('http://localhost:3000/catalog')
    cy.get('#root img[alt="Смартфоны"]').click();
    cy.get('#root a[href="/product/15"] div.product-content').click();
    cy.get('#root button.add-to-cart-button').click();
    cy.get('#root img[alt="Корзина"]').click();
    cy.get('#root a.checkout-cart-button').click();
    cy.get('#root [name="customerFullName"]').click();
    cy.get('#root [name="customerFullName"]').type('898989');
    cy.get('#root [name="customerEmail"]').click();
    cy.get('#root [name="customerEmail"]').type('88838938');
    cy.get('#root [name="customerPhoneNumber"]').click();
    cy.get('#root [name="customerPhoneNumber"]').type('+7 (990) 976-64-67');
    cy.get('#root [name="payMethod"]').select('Онлайн');
    cy.get('#root [name="shipMethod"]').select('Служба доставки');
    cy.get('#root [name="shipAddress"]').click();
    cy.get('#root [name="shipAddress"]').type('fjfjjf');
    cy.get('#root [name="shipCompanyId"]').select('1');
    cy.get('#root button.submit-button').click();
    cy.get('#root form.checkout-form').click();
    cy.get('#root [name="customerEmail"]').clear();
    cy.get('#root [name="customerEmail"]').type('cybersj@vk.com');
    cy.get('#root button.submit-button').click();
    cy.get('#root form.checkout-form').click();
    cy.get('#root [name="customerFullName"]').clear();
    cy.get('#root [name="customerFullName"]').type('Иван');
    cy.get('#root button.submit-button').click();
    cy.get('#root [name="customerFullName"]').type(' Иванович');
    cy.get('#root button.submit-button').click();
  })
})