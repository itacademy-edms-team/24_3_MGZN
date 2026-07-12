import React, { useEffect, useState } from 'react';
import './ScrollToTopButton.css';

const SHOW_AFTER = 280;

const ScrollToTopButton: React.FC = () => {
  const [visible, setVisible] = useState(false);

  useEffect(() => {
    const onScroll = () => {
      setVisible(window.scrollY > SHOW_AFTER);
    };

    onScroll();
    window.addEventListener('scroll', onScroll, { passive: true });
    return () => window.removeEventListener('scroll', onScroll);
  }, []);

  const scrollToTop = () => {
    const reduced = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    window.scrollTo({ top: 0, behavior: reduced ? 'auto' : 'smooth' });
  };

  return (
    <button
      type="button"
      className={`scroll-to-top${visible ? ' scroll-to-top--visible' : ''}`}
      onClick={scrollToTop}
      aria-label="Наверх"
      title="Наверх"
      tabIndex={visible ? 0 : -1}
    >
      <svg
        className="scroll-to-top__icon"
        viewBox="0 0 24 24"
        aria-hidden="true"
        focusable="false"
      >
        <path
          d="M12 5l-7 7h4.5v7h5v-7H19l-7-7z"
          fill="currentColor"
        />
      </svg>
    </button>
  );
};

export default ScrollToTopButton;
