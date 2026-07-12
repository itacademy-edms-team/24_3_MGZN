import React from 'react';
import SearchResultsPage from './SearchResultPage/SearchResultsPage';
import './SearchPage.css';

const SearchPage: React.FC = () => (
  <div className="search-page">
    <SearchResultsPage />
  </div>
);

export default SearchPage;
