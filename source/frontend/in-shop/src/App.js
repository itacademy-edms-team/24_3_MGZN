import React, { useState, useEffect } from 'react';
import './App.css'; // Импортируем CSS для стилей

function App() {
    // Состояние для хранения категорий
    const [categories, setCategories] = useState([]);
    const [loading, setLoading] = useState(true); // Состояние загрузки
    const [error, setError] = useState(null); // Состояние ошибки

    // Функция для загрузки категорий из API
    const fetchCategories = async () => {
        try {
            // Замените URL на адрес вашего API
            const response = await fetch('https://localhost:7275/api/Category');

            // Проверяем, успешен ли ответ сервера
            if (!response.ok) {
                throw new Error(`Ошибка ${response.status}: ${response.statusText}`);
            }

            // Парсим данные из JSON
            const data = await response.json();

            // Сохраняем данные в состояние
            setCategories(data);
        } catch (err) {
            // Обрабатываем ошибку и сохраняем сообщение об ошибке
            setError(err.message || 'Неизвестная ошибка');
        } finally {
            // Устанавливаем флаг загрузки в false независимо от результата
            setLoading(false);
        }
    };

    // Загружаем категории при монтировании компонента
    useEffect(() => {
        fetchCategories();
    }, []); // Пустой массив зависимостей гарантирует выполнение только один раз

    return (
        <div className="App">
            {/* Шапка страницы */}
            <header>
                <h1>InShop</h1>
                <img src="logo.png" alt="Логотип магазина" />
            </header>

            {/* Основное содержимое */}
            <main>
                <h2>Категории товаров</h2>

                {/* Индикатор загрузки */}
                {loading && <p>Загрузка...</p>}

                {/* Сообщение об ошибке */}
                {error && <p style={{ color: 'red' }}>Ошибка: {error}</p>}

                {/* Список категорий */}
                {!loading && !error && categories.length > 0 && (
                    <ul className="categories">
                        {categories.map(category => (
                            <li key={category.categoryId} className="category-title">
                                {category.categoryName}
                            </li>
                        ))}
                    </ul>
                )}

                {/* Если категорий нет */}
                {!loading && !error && categories.length === 0 && (
                    <p>Нет доступных категорий.</p>
                )}
            </main>

            {/* Футер */}
            <footer>
                <p>&copy; 2025 InShop. Все права защищены.</p>
                <ul>
                    <li><a href="#">О нас</a></li>
                    <li><a href="#">Контакты</a></li>
                    <li><a href="#">Политика конфиденциальности</a></li>
                </ul>
            </footer>
        </div>
    );
}

export default App;