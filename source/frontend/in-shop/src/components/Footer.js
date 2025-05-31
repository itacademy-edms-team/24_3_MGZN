import React from 'react';
import './Footer.css'; // Импортируем стили

const Footer = () => {
    return (
        <footer className="footer">
            <p>&copy; 2025 InShop. Все права защищены.</p>
            <ul>
                <li>
                    <a href="#">О нас</a>
                </li>
                <li>
                    <a href="#">Контакты</a>
                </li>
                <li>
                    <a href="#">Политика конфиденциальности</a>
                </li>
            </ul>
        </footer>
    );
};

export default Footer;