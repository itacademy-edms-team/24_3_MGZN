document.addEventListener('DOMContentLoaded', () => {
    // URL API для получения категорий
    const categoriesUrl = 'https://localhost:7275/api/Category';

    // Контейнер для категорий
    const categoriesList = document.getElementById('categories-list');

    //Функция для загрузки категорий
    async function loadCategories() {
        try {
            // GET-запрос к API
            const response = await fetch(categoriestURL);

            //Проверка на успешность запроса
            if (!response.ok) {
                throw new Error('Ошибка при загрузке категорий');
            }
             
            //Получаем данные из ответа
            const categories = await response.json();

            // Очищаем контейнер перед добавлением новых элементов
            categoriesList.innerHTML = '';

            // Добавляем каждую категорию в список
            categories.forEach(category => {
                const categoryItem = document.createElement('li');

                //Создаем заголовок категории
                const categoryTitle = document.createElement('span');
                categoryTitle.textContent = category.name;
                categoryTitle.className = 'category-title';

                //Добавляем элементы в DOM
                categoryItem.appendChild(categoryTitle);
                categoriesList.appendChild(categoryItem);
            });
        } catch (error) {
            console.error(error.message);
        }
    }
    loadCategories();
});
console.log("Скрипт подключен!");