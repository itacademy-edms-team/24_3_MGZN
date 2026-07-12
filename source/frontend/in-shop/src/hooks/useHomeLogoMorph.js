import { useEffect, useRef, useState } from 'react';

const MORPH_DISTANCE = 140;
const EASE = (t) => t * t * (3 - 2 * t); // smoothstep

const clamp = (value, min, max) => Math.min(max, Math.max(min, value));

const lerp = (from, to, t) => from + (to - from) * t;

/**
 * На главной: логотип летит из hero в слот шапки по скроллу.
 * Возвращает inline-стиль для fixed-элемента.
 */
export function useHomeLogoMorph(enabled) {
  const [style, setStyle] = useState(null);
  const startRef = useRef(null);
  const rafRef = useRef(0);

  useEffect(() => {
    if (!enabled) {
      startRef.current = null;
      setStyle(null);
      document.body.classList.remove('home-logo-morph-active', 'home-logo-morph-settled');
      return undefined;
    }

    const reducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    if (reducedMotion) {
      setStyle(null);
      document.body.classList.remove('home-logo-morph-active', 'home-logo-morph-settled');
      return undefined;
    }

    const captureStart = () => {
      const source = document.getElementById('catalog-hero-brand');
      if (!source) return false;
      const rect = source.getBoundingClientRect();
      if (rect.width < 2 || rect.height < 2) return false;
      const styles = window.getComputedStyle(source);
      startRef.current = {
        docLeft: rect.left + window.scrollX,
        docTop: rect.top + window.scrollY,
        fontSize: parseFloat(styles.fontSize),
        letterSpacing: styles.letterSpacing,
      };
      return true;
    };

    const update = () => {
      const target = document.getElementById('header-logo');
      if (!target) return;

      if (!startRef.current && !captureStart()) {
        document.body.classList.remove('home-logo-morph-active', 'home-logo-morph-settled');
        setStyle(null);
        return;
      }

      document.body.classList.add('home-logo-morph-active');

      const progress = EASE(clamp(window.scrollY / MORPH_DISTANCE, 0, 1));
      const isSettled = progress >= 0.999;
      document.body.classList.toggle('home-logo-morph-settled', isSettled);

      if (isSettled) {
        setStyle(null);
        return;
      }

      const start = startRef.current;
      const end = target.getBoundingClientRect();
      const endStyles = window.getComputedStyle(target);

      const left = lerp(start.docLeft - window.scrollX, end.left, progress);
      const top = lerp(start.docTop - window.scrollY, end.top, progress);
      const fontSize = lerp(start.fontSize, parseFloat(endStyles.fontSize), progress);

      setStyle({
        position: 'fixed',
        left: `${left}px`,
        top: `${top}px`,
        fontSize: `${fontSize}px`,
        fontWeight: 800,
        letterSpacing: progress > 0.5 ? endStyles.letterSpacing : start.letterSpacing,
        lineHeight: 0.95,
        margin: 0,
        color: '#fff',
        zIndex: 1001,
        pointerEvents: progress > 0.85 ? 'auto' : 'none',
        whiteSpace: 'nowrap',
        textDecoration: 'none',
        transformOrigin: 'left top',
        willChange: 'left, top, font-size',
      });
    };

    const onScrollOrResize = () => {
      if (window.scrollY < 8) {
        startRef.current = null;
        captureStart();
      }
      cancelAnimationFrame(rafRef.current);
      rafRef.current = requestAnimationFrame(update);
    };

    const retryId = window.setInterval(() => {
      if (captureStart()) {
        window.clearInterval(retryId);
        update();
      }
    }, 50);

    window.addEventListener('scroll', onScrollOrResize, { passive: true });
    window.addEventListener('resize', onScrollOrResize);
    update();

    return () => {
      window.clearInterval(retryId);
      cancelAnimationFrame(rafRef.current);
      window.removeEventListener('scroll', onScrollOrResize);
      window.removeEventListener('resize', onScrollOrResize);
      document.body.classList.remove('home-logo-morph-active', 'home-logo-morph-settled');
    };
  }, [enabled]);

  return { style };
}
