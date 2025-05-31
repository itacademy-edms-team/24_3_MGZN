import React from 'react';
import { Link } from 'react-router-dom';
import './Breadcrumb.css';

const Breadcrumb = ({ currentPage, categoryName }) => {
    console.log('Breadcrumb props:', { currentPage, categoryName }); // Логируем props

    return (
        <nav className="breadcrumb">
            <Link to="/catalog">Каталог товаров</Link>
            {categoryName && ( // Проверяем наличие categoryName
                <>
                    {' / '}
                    <Link to={`/category/${encodeURIComponent(categoryName)}`}>
                        {decodeURIComponent(categoryName)}
                    </Link>
                </>
            )}
            {currentPage && (
                <>
                    {' / '}
                    <span>{currentPage}</span>
                </>
            )}
        </nav>
    );
};

export default Breadcrumb;