import React, { useEffect, useRef, useState } from 'react';
import { getReviewAiSummary } from '../../api/reviews';
import { ReviewSummary } from '../../types/reviewSummary';
import './AiSummaryBlock.css';

interface AiSummaryBlockProps {
  productId: number;
}

type Phase = 'idle' | 'motion' | 'result' | 'error';

const MOTION_MS = 3000;

const MOTION_STATUSES = [
  'Собираем отзывы…',
  'Ищем повторяющиеся оценки…',
  'Складываем плюсы и минусы…',
  'Формируем краткий обзор…',
];

const AiSummaryBlock: React.FC<AiSummaryBlockProps> = ({ productId }) => {
  const [phase, setPhase] = useState<Phase>('idle');
  const [data, setData] = useState<ReviewSummary | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [statusIndex, setStatusIndex] = useState(0);
  const [progress, setProgress] = useState(0);
  const abortRef = useRef(false);

  useEffect(() => {
    abortRef.current = false;
    return () => {
      abortRef.current = true;
    };
  }, []);

  useEffect(() => {
    if (phase !== 'motion') return undefined;

    setStatusIndex(0);
    setProgress(0);

    const statusTimer = window.setInterval(() => {
      setStatusIndex((prev) => Math.min(prev + 1, MOTION_STATUSES.length - 1));
    }, MOTION_MS / MOTION_STATUSES.length);

    const started = performance.now();
    let rafId = 0;
    const tick = (now: number) => {
      const t = Math.min(1, (now - started) / MOTION_MS);
      setProgress(t * 100);
      if (t < 1) {
        rafId = requestAnimationFrame(tick);
      }
    };
    rafId = requestAnimationFrame(tick);

    return () => {
      window.clearInterval(statusTimer);
      cancelAnimationFrame(rafId);
    };
  }, [phase]);

  const handleClose = () => {
    setPhase('idle');
  };

  const handleOpen = async () => {
    if (phase === 'motion') return;

    if (phase === 'result' || phase === 'error') {
      handleClose();
      return;
    }

    setPhase('motion');
    setError(null);
    setProgress(0);
    setStatusIndex(0);

    const startedAt = Date.now();

    const waitRemaining = async () => {
      const elapsed = Date.now() - startedAt;
      const left = Math.max(0, MOTION_MS - elapsed);
      if (left > 0) {
        await new Promise((resolve) => window.setTimeout(resolve, left));
      }
    };

    try {
      let summary = data;
      if (!summary) {
        summary = await getReviewAiSummary(productId);
      }
      await waitRemaining();
      if (abortRef.current) return;
      setData(summary);
      setPhase('result');
    } catch (err: any) {
      await waitRemaining();
      if (abortRef.current) return;
      console.error(err);
      if (err.response?.status === 503) {
        setError('Анализ уже готовится. Попробуйте через несколько секунд.');
      } else {
        setError('Не удалось загрузить анализ. Попробуйте позже.');
      }
      setPhase('error');
    }
  };

  return (
    <div className="ai-summary-wrapper">
      {phase === 'idle' && (
        <button type="button" onClick={handleOpen} className="ai-summary-trigger-btn">
          <span className="btn-icon" aria-hidden="true">
            ✦
          </span>
          AI-анализ отзывов
        </button>
      )}

      {phase === 'motion' && (
        <div className="ai-motion" role="status" aria-live="polite">
          <div className="ai-motion__frame">
            <span className="ai-motion__corner ai-motion__corner--tl" />
            <span className="ai-motion__corner ai-motion__corner--tr" />
            <span className="ai-motion__corner ai-motion__corner--bl" />
            <span className="ai-motion__corner ai-motion__corner--br" />
            <div className="ai-motion__scan" />
            <div className="ai-motion__bars" aria-hidden="true">
              <span />
              <span />
              <span />
              <span />
              <span />
            </div>
            <p className="ai-motion__label">AI-анализ</p>
            <p className="ai-motion__status" key={statusIndex}>
              {MOTION_STATUSES[statusIndex]}
            </p>
            <div className="ai-motion__progress" aria-hidden="true">
              <div className="ai-motion__progress-fill" style={{ width: `${progress}%` }} />
            </div>
          </div>
        </div>
      )}

      {(phase === 'result' || phase === 'error') && (
        <div className="ai-summary-content ai-summary-content--enter">
          <button
            type="button"
            className="ai-summary-close"
            onClick={handleClose}
            aria-label="Скрыть анализ"
          >
            ×
          </button>

          {phase === 'error' ? (
            <div className="ai-error-state">{error}</div>
          ) : data ? (
            <div className="ai-data-container">
              <div className="ai-header">
                <h3>Краткий обзор от ИИ</h3>
                <span className={`ai-trend-badge trend-${data.ratingTrend.toLowerCase()}`}>
                  Настроение:{' '}
                  {data.ratingTrend === 'Positive'
                    ? 'положительное'
                    : data.ratingTrend === 'Negative'
                      ? 'отрицательное'
                      : 'нейтральное'}
                </span>
              </div>

              <div className="ai-grid">
                {data.pros.length > 0 && (
                  <div className="ai-column ai-pros">
                    <h4>Плюсы</h4>
                    <ul>
                      {data.pros.map((pro, idx) => (
                        <li key={idx}>{pro}</li>
                      ))}
                    </ul>
                  </div>
                )}

                {data.cons.length > 0 && (
                  <div className="ai-column ai-cons">
                    <h4>Минусы</h4>
                    <ul>
                      {data.cons.map((con, idx) => (
                        <li key={idx}>{con}</li>
                      ))}
                    </ul>
                  </div>
                )}
              </div>

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
