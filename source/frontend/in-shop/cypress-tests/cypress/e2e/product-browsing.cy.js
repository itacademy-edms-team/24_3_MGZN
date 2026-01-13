describe('template spec', () => {
  it('Просмотр товаров и навигаци по сайту', function() {
  cy.visit('http://localhost:3000')
  cy.get('#root a[href="/category/%D0%A1%D0%BC%D0%B0%D1%80%D1%82%D1%84%D0%BE%D0%BD%D1%8B"] p').click();
  cy.get('#root a[href="/product/15"] div.product-info').click();
  cy.get('#root img[alt="IPhone 11"]').click();
  cy.get('#root a[href="/category/%D0%A1%D0%BC%D0%B0%D1%80%D1%82%D1%84%D0%BE%D0%BD%D1%8B"]').click();
  cy.get('#root img[alt="IPhone 12"]').click();
  cy.get('#root a[href="/catalog"]').click();
  cy.get('#root img[alt="Ноутбуки"]').click();
  cy.get('#root img[alt="MacBook Pro M4"]').click();
  cy.get('#root p.product-description').click();
  cy.get('#root p.product-description').click();
  cy.get('#root div.product-info').click();
  cy.get('#root div.product-info').click();
  cy.get('#root div.product-info').click();
  cy.get('#root li:nth-child(2) h4').click();
  cy.get('#root img[alt="MacBook Air M4"]').click();
  cy.get('#root a[href="/catalog"]').click();
  cy.get('#root img[alt="Смартфоны"]').click();
  cy.get('#root a[href="/product/15"] div.product-info').click();
  cy.get('#root ul.related-products-list li:nth-child(4)').click();
  cy.get('#root img[alt="IPhone 12"]').click();
  cy.get('#root a[href="/category/%D0%A1%D0%BC%D0%B0%D1%80%D1%82%D1%84%D0%BE%D0%BD%D1%8B"]').click();
  cy.get('#root a[href="/catalog"]').click();
  cy.get('#root img[alt="Ноутбуки"]').click();
  cy.get('#root img[alt="MacBook Pro M4"]').click();
  cy.get('#root a[href="/catalog"]').click();
  cy.get('#root a[href="/category/%D0%90%D1%83%D0%B4%D0%B8%D0%BE"]').click();
  cy.get('#root a[href="/product/8"] div.product-content').click();
  cy.get('#root img[alt="AirPods Pro"]').click();
  cy.get('#root a[href="/catalog"]').click();
  cy.get('#root img[alt="Планшеты"]').click();
  cy.get('#root img[alt="Ipad Mini 2023"]').click();
  cy.get('#root a.logo h1').click();
  cy.get('#root a[href="http://localhost:3000/category/Аксессуары"]').click();
  cy.get('#root a[href="/catalog"]').click();
  
});
});

