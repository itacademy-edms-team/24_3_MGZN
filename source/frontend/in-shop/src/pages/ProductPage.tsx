import React, { useEffect, useState, useContext } from 'react';
import { useParams } from 'react-router-dom';
import axios from 'axios';
import Breadcrumb from '../components/Breadcrumb.js';
import { CartContext } from '../components/CartContext.js';
import './ProductPage.css';
import ProductCard from '../components/ProductCard.jsx';
import LoadingSpinner from '../components/LoadingSpinner.tsx';
import StarRating from '../components/StarRating/StarRating.tsx'; 
import ReviewList from '../components/ReviewList/ReviewList.tsx';
import ReviewForm from '../components/ReviewForm/ReviewForm.tsx';
import Modal from '../components/Modal.tsx';
import { createReview, updateReview, deleteReview } from '../api/reviews.ts';
import { Review, CreateReviewDto, UpdateReviewDto } from '../types/review.ts';

interface ProductSpecificationDto {
    specId: number;
    name: string;
    displayName: string;
    dataType: string;
    textValue: string | null;
    numberValue: number | null;
}

interface ProductDto {
    productId: number;
    productName: string;
    productDescription: string;
    productPrice: number;
    productAvailability: boolean;
    productCategoryId: number;
    productStockQuantity: number;
    imageUrl: string;
    productCategoryName: string;
    averageRating?: number;
    reviewsCount?: number;
}

const ProductPage = () => {
    const { productId } = useParams();
    
    const [product, setProduct] = useState<ProductDto | null>(null); 
    const [relatedProducts, setRelatedProducts] = useState<any[]>([]);
    const [specifications, setSpecifications] = useState<ProductSpecificationDto[]>([]);
    const [loading, setLoading] = useState(true);
    const [specsLoading, setSpecsLoading] = useState(false);

    const [isModalOpen, setIsModalOpen] = useState(false);
    const [editingReview, setEditingReview] = useState<Review | null>(null);
    const [refreshReviewsTrigger, setRefreshReviewsTrigger] = useState(0);
    const [isSubmitting, setIsSubmitting] = useState(false);

    const { addToCart } = useContext(CartContext);

    useEffect(() => {
        if (!productId) return;
        setLoading(true);
        setSpecsLoading(true);

        const fetchProductData = async () => {
            try {
                const productRes = await axios.get(`https://localhost:7275/api/Products/${productId}`);
                setProduct(productRes.data);
                const specsRes = await axios.get(`https://localhost:7275/api/Products/${productId}/specifications`);
                setSpecifications(specsRes.data || []);
            } catch (error) {
                console.error('Ошибка загрузки данных:', error);
            } finally {
                setSpecsLoading(false);
            }
        };
        fetchProductData();
    }, [productId]); 

    useEffect(() => {
        if (!product?.productCategoryName || !productId) return;
        const fetchRelatedProducts = async () => {
            try {
                const response = await axios.get(
                    `https://localhost:7275/api/Products/products-by-category?categoryName=${encodeURIComponent(product.productCategoryName)}`
                );
                const relatedProductsData = response.data.filter(
                    (p: any) => p.productId !== parseInt(productId)
                );
                setRelatedProducts(relatedProductsData);
            } catch (error) {
                console.error('Ошибка загрузки связанных товаров:', error);
            } finally {
                setLoading(false); 
            }
        };
        fetchRelatedProducts();
    }, [product, productId]); 

    const handleDeleteReview = async (reviewId: number) => {
        if (!window.confirm('Вы уверены, что хотите удалить этот отзыв?')) return;
        
        try {
            await deleteReview(reviewId);
            setRefreshReviewsTrigger(prev => prev + 1);
            
            if (productId) {
                const productRes = await axios.get(`https://localhost:7275/api/Products/${productId}`);
                setProduct(productRes.data);
            }
        } catch (error: any) {
            console.error('Ошибка удаления:', error);
            // Если бэкенд вернул 403 или 404 (нет прав), сообщаем пользователю
            if (error.response?.status === 403 || error.response?.status === 404) {
                alert('У вас нет прав на удаление этого отзыва.');
            } else {
                alert('Не удалось удалить отзыв');
            }
        }
    };

    const openCreateModal = () => {
        setEditingReview(null);
        setIsModalOpen(true);
    };

    const closeModal = () => {
        setIsModalOpen(false);
        setEditingReview(null);
    };

    const handleReviewSubmit = async (data: CreateReviewDto | UpdateReviewDto) => {
        setIsSubmitting(true);
        try {
            if (editingReview) {
                await updateReview(editingReview.reviewId, data as UpdateReviewDto);
            } else {
                await createReview(Number(productId), data as CreateReviewDto);
            }
            closeModal();
            setRefreshReviewsTrigger(prev => prev + 1);
            
            if (productId) {
                 const productRes = await axios.get(`https://localhost:7275/api/Products/${productId}`);
                 setProduct(productRes.data);
            }
        } catch (error) {
            console.error('Ошибка при отправке отзыва:', error);
            throw error;
        } finally {
            setIsSubmitting(false);
        }
    };

    if (loading) {
        return <LoadingSpinner message="Загрузка товаров..." />;
    }

    if (!product) {
        return <p>Товар не найден.</p>;
    }

    return (
        <div>
            <Breadcrumb
                currentPage={product.productName}
                categoryName={product.productCategoryName}
            />
            <div className="product-page">
                <div className="product-details">
                    <img 
                        className="product-page__img"
                        src={`https://localhost:7275${product.imageUrl}`}
                        alt={product.productName}
                        onError={(e) => {
                            e.target.src = 'https://localhost:7275/images/placeholder.svg';
                        }}
                        loading="lazy"
                    />

                    <div className="product-info">
                        <h2>{product.productName}</h2>

                        <div className="product-rating-summary">
                            <StarRating 
                                rating={product.averageRating || 0} 
                                readOnly 
                                size="medium" 
                            />
                            <span className="average-rating-value">
                                {product.averageRating ? product.averageRating.toFixed(1) : '—'}
                            </span>
                            <span className="reviews-count">
                                ({product.reviewsCount || 0} отзывов)
                            </span>
                        </div>

                        {product.productStockQuantity > 0 ? (
                            <p>На складе: {product.productStockQuantity} шт.</p>
                        ) : (
                            <p className="out-of-stock">Нет в наличии</p>
                        )}

                        <p className="product-price">Цена: {product.productPrice} ₽</p>
                        <p className="product-description">{product.productDescription}</p>

                        {product.productStockQuantity > 0 ? (
                            <button
                                className="add-to-cart-button"
                                onClick={() => addToCart(product)}
                            >
                                Добавить в корзину
                            </button>
                        ) : null}
                        
                        <button className="write-review-button" onClick={openCreateModal}>
                            Написать отзыв
                        </button>
                    </div>
                </div>

                {specifications.length > 0 && (
                    <div className="product-specifications-section">
                        <h3 className="specs-title">Характеристики</h3>
                        {specsLoading ? (
                            <div className="specs-loading"><LoadingSpinner message="Загрузка характеристик..." /></div>
                        ) : (
                            <div className="specs-grid">
                                {specifications.map((spec) => {
                                    const value = spec.textValue ?? spec.numberValue;
                                    const displayValue = typeof value === 'number' 
                                        ? (Number.isInteger(value) ? value : parseFloat(value.toFixed(2))) 
                                        : value;

                                    return (
                                        <div key={spec.specId} className="spec-item">
                                            <span className="spec-name">{spec.displayName}:</span>
                                            <span className="spec-value">{displayValue}</span>
                                        </div>
                                    );
                                })}
                            </div>
                        )}
                    </div>
                )}

                <div className="product-reviews-section">
                    <ReviewList 
                        productId={Number(productId)} 
                        onRefreshTrigger={refreshReviewsTrigger} 
                        onEdit={(review) => {
                            setEditingReview(review);
                            setIsModalOpen(true);
                        }}
                        onDelete={handleDeleteReview}
                    />
                </div>

                <div className="related-products">
                    <h3>Товары категории {product.productCategoryName}</h3>
                    <ul className="products-list">
                        {relatedProducts.map((relatedProduct) => (
                            <li key={relatedProduct.productId} className="product-card-wrapper">
                                <ProductCard product={relatedProduct} />
                            </li>
                        ))}
                    </ul>
                </div>
            </div>

            <Modal isOpen={isModalOpen} onClose={closeModal} title={editingReview ? "Редактировать отзыв" : "Написать отзыв"}>
                <ReviewForm 
                    initialReview={editingReview}
                    onSubmit={handleReviewSubmit}
                    onCancel={closeModal}
                    isLoading={isSubmitting}
                />
            </Modal>
        </div>
    );
};

export default ProductPage;