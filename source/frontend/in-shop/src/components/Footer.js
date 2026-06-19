import React from 'react';
import './Footer.css'; // Импортируем стили

const Footer = () => {
    return (
        <footer className="footer">
            <div className="footer-container">
                {/* Логотип или название */}
                <div className="logo">
                    <h1>.InShop</h1>
                </div>

                {/* Категории */}
                <div className="categories">
                    <ul>
                        <li><a href="/category/Аудио">Аудио</a></li>
                        <li><a href="/category/Смартфоны">Смартфоны</a></li>
                        <li><a href="/category/Аксессуары">Аксессуары</a></li>
                        <li><a href="/category/Планшеты">Планшеты</a></li>
                        <li><a href="/category/Ноутбуки">Ноутбуки</a></li>
                    </ul>
                </div>

                {/* Социальные сети */}
                <div className="social-links">
                    <a href="https://facebook.com" aria-label="Facebook" target="_blank" rel="noreferrer"><i className="fab fa-facebook"></i></a>
                    <a href="https://twitter.com" aria-label="Twitter" target="_blank" rel="noreferrer"><i className="fab fa-twitter"></i></a>
                    <a href="https://instagram.com" aria-label="Instagram" target="_blank" rel="noreferrer"><i className="fab fa-instagram"></i></a>
                    <a href="https://youtube.com" aria-label="YouTube" target="_blank" rel="noreferrer"><i className="fab fa-youtube"></i></a>
                </div>

                {/* Авторские права */}
                <p>&copy; 2025 InShop. Все права защищены.</p>
            </div>
        </footer>
    );
};

export default Footer;