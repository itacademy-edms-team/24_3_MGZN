import React, { useState } from 'react';
import { getReviewAiSummary } from '../../api/reviews.ts';
import { ReviewSummary } from '../../types/reviewSummary';
import './AiSummaryBlock.css';

interface AiSummaryBlockProps {
  productId: number;
}

const AiSummaryBlock: React.FC<AiSummaryBlockProps> = ({ productId }) => {
  const [isOpen, setIsOpen] = useState(false);
  const [data, setData] = useState<ReviewSummary | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleToggle = async () => {
    if (isOpen) {
      setIsOpen(false);
      return;
    }

    setIsOpen(true);
    
    // Если данные уже есть, не грузим снова
    if (data) return;

    setIsLoading(true);
    setError(null);

    try {
      const summary = await getReviewAiSummary(productId);
      setData(summary);
    } catch (err: any) {
      console.error(err);
      if (err.response?.status === 503) {
        setError('АНАЛИЗ УЖЕ ГОТОВИТСЯ ДРУГИМ ПОЛЬЗОВАТЕЛЕМ. ПОПРОБУЙТЕ ЧЕРЕЗ НЕСКОЛЬКО СЕКУНД.');
      } else {
        setError('НЕ УДАЛОСЬ ЗАГРУЗИТЬ АНАЛИЗ. ПОПРОБУЙТЕ ПОЗЖЕ.');
      }
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="ai-summary-wrapper">
      {/* Кнопка вызова */}
      {!isOpen && (
        <button
          onClick={handleToggle}
          className="ai-summary-trigger-btn"
        >
          <span className="btn-icon">✨</span>
          AI-АНАЛИЗ ОТЗЫВОВ
        </button>
      )}

      {/* Раскрытый блок */}
      {isOpen && (
        <div className={`ai-summary-content ${isLoading ? 'rainbow-border' : ''}`}>
          
          {isLoading ? (
            <div className="ai-loading-state">
              <div className="loader-spinner"></div>
              <p>ИИ ИЗУЧАЕТ МНЕНИЯ ПОКУПАТЕЛЕЙ...</p>
            </div>
          ) : error ? (
            <div className="ai-error-state">
              {error}
            </div>
          ) : data ? (
            <div className="ai-data-container">
              
              {/* Заголовок */}
              <div className="ai-header">
                <h3>КРАТКИЙ ОБЗОР ОТ ИИ</h3>
                <span className={`ai-trend-badge trend-${data.ratingTrend.toLowerCase()}`}>
                  НАСТРОЕНИЕ: {data.ratingTrend === 'Positive' ? 'ПОЛОЖИТЕЛЬНОЕ' : 
                               data.ratingTrend === 'Negative' ? 'ОТРИЦАТЕЛЬНОЕ' : 'НЕЙТРАЛЬНОЕ'}
                </span>
              </div>

              <div className="ai-grid">
                {/* Плюсы */}
                {data.pros.length > 0 && (
                  <div className="ai-column ai-pros">
                    <h4>✅ ПЛЮСЫ</h4>
                    <ul>
                      {data.pros.map((pro, idx) => (
                        <li key={idx}>{pro}</li>
                      ))}
                    </ul>
                  </div>
                )}

                {/* Минусы */}
                {data.cons.length > 0 && (
                  <div className="ai-column ai-cons">
                    <h4>❌ МИНУСЫ</h4>
                    <ul>
                      {data.cons.map((con, idx) => (
                        <li key={idx}>{con}</li>
                      ))}
                    </ul>
                  </div>
                )}
              </div>

              {/* Итог */}
              <div className="ai-footer">
                <p>{data.summary}</p>
              </div>

            </div>
          ) : null}
        </div>
      )}
    </div>
  );
};

export default AiSummaryBlock;