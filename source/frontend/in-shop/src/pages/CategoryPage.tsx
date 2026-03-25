// src/pages/CategoryPage/CategoryPage.tsx
import React from 'react';
import { useParams } from 'react-router-dom';
import Breadcrumb from '../components/Breadcrumb.js';
import SearchResultsPage from './SearchResultPage/SearchResultsPage.tsx';
import './CategoryPage.css';

const CategoryPage: React.FC = () => {
  const { categoryName } = useParams<{ categoryName: string }>();
  
  if (!categoryName) {
    return (
      <div className="category-page">
        <div className="error-message">Категория не найдена</div>
      </div>
    );
  }
  
  const decodedCategoryName = decodeURIComponent(categoryName);
  
  return (
    <div className="category-page">
      <Breadcrumb categoryName={decodedCategoryName} />
      
      <SearchResultsPage
        forcedCategory={decodedCategoryName}
        hideSearchQuery={true}
        pageTitleOverride={`Категория: ${decodedCategoryName}`}
      />
    </div>
  );
};

export default CategoryPage;