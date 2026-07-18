import { useLayoutEffect, useRef, useState } from 'react';
import { flushSync } from 'react-dom';

const MORPH_DISTANCE = 140;
const ENTER_DURATION_MS = 520;
const EASE = (t) => t * t * (3 - 2 * t);

const clamp = (value, min, max) => Math.min(max, Math.max(min, value));
const lerp = (from, to, t) => from + (to - from) * t;

function readLogoMetrics(el) {
  if (!el) return null;
  const rect = el.getBoundingClientRect();
  if (rect.width < 2 || rect.height < 2) return null;
  const styles = window.getComputedStyle(el);
  return {
    left: rect.left,
    top: rect.top,
    fontSize: parseFloat(styles.fontSize),
    letterSpacing: styles.letterSpacing,
  };
}

function buildMorphStyle(from, to, progress) {
  const left = lerp(from.left, to.left, progress);
  const top = lerp(from.top, to.top, progress);
  const fontSize = lerp(from.fontSize, to.fontSize, progress);

  return {
    position: 'fixed',
    left: `${left}px`,
    top: `${top}px`,
    fontSize: `${fontSize}px`,
    fontWeight: 800,
    letterSpacing: progress > 0.5 ? to.letterSpacing : from.letterSpacing,
    lineHeight: 0.95,
    margin: 0,
    color: '#fff',
    zIndex: 1001,
    pointerEvents: 'none',
    whiteSpace: 'nowrap',
    textDecoration: 'none',
    transformOrigin: 'left top',
    willChange: 'left, top, font-size',
  };
}

/**
 * На главной: логотип летит из hero в слот шапки по скроллу.
 * При переходе на главную — reverse header → hero.
 * Header-лого не скрываем opacity — слой morph только перекрывает/уходит.
 */
export function useHomeLogoMorph(enabled) {
  const [style, setStyle] = useState(null);
  const startDocRef = useRef(null);
  const rafRef = useRef(0);
  const enterRafRef = useRef(0);
  const prevEnabledRef = useRef(enabled);
  const enteringRef = useRef(false);

  useLayoutEffect(() => {
    const wasEnabled = prevEnabledRef.current;
    prevEnabledRef.current = enabled;

    const clearBody = () => {
      document.body.classList.remove(
        'home-logo-morph-active',
        'home-logo-morph-settled',
        'home-logo-morph-entering'
      );
    };

    if (!enabled) {
      cancelAnimationFrame(rafRef.current);
      cancelAnimationFrame(enterRafRef.current);
      enteringRef.current = false;
      startDocRef.current = null;
      flushSync(() => setStyle(null));
      clearBody();
      return undefined;
    }

    const reducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    if (reducedMotion) {
      flushSync(() => setStyle(null));
      clearBody();
      return undefined;
    }

    const captureHeroStartDoc = () => {
      const source = document.getElementById('catalog-hero-brand');
      if (!source) return false;
      const rect = source.getBoundingClientRect();
      if (rect.width < 2 || rect.height < 2) return false;
      const styles = window.getComputedStyle(source);
      startDocRef.current = {
        docLeft: rect.left + window.scrollX,
        docTop: rect.top + window.scrollY,
        fontSize: parseFloat(styles.fontSize),
        letterSpacing: styles.letterSpacing,
      };
      return true;
    };

    const updateScrollMorph = () => {
      if (enteringRef.current) return;

      const target = document.getElementById('header-logo');
      if (!target) return;

      if (!startDocRef.current && !captureHeroStartDoc()) {
        document.body.classList.remove('home-logo-morph-active', 'home-logo-morph-settled');
        flushSync(() => setStyle(null));
        return;
      }

      document.body.classList.add('home-logo-morph-active');
      document.body.classList.remove('home-logo-morph-entering');

      const progress = EASE(clamp(window.scrollY / MORPH_DISTANCE, 0, 1));
      const isSettled = progress >= 0.999;
      document.body.classList.toggle('home-logo-morph-settled', isSettled);

      if (isSettled) {
        flushSync(() => setStyle(null));
        return;
      }

      const start = startDocRef.current;
      const end = target.getBoundingClientRect();
      const endStyles = window.getComputedStyle(target);

      const from = {
        left: start.docLeft - window.scrollX,
        top: start.docTop - window.scrollY,
        fontSize: start.fontSize,
        letterSpacing: start.letterSpacing,
      };
      const to = {
        left: end.left,
        top: end.top,
        fontSize: parseFloat(endStyles.fontSize),
        letterSpacing: endStyles.letterSpacing,
      };

      setStyle(buildMorphStyle(from, to, progress));
    };

    const runEnterMorph = () => {
      enteringRef.current = true;
      document.body.classList.add('home-logo-morph-active', 'home-logo-morph-entering');
      document.body.classList.remove('home-logo-morph-settled');
      window.scrollTo(0, 0);

      const from = readLogoMetrics(document.getElementById('header-logo'));
      if (!from) {
        enteringRef.current = false;
        document.body.classList.remove('home-logo-morph-entering');
        updateScrollMorph();
        return;
      }

      // Синхронно до paint: morph + скрытие слота лого в одном кадре.
      flushSync(() => {
        setStyle(buildMorphStyle(from, from, 0));
      });

      let fromFrozen = from;
      let fromLocked = false;
      let startedAt = 0;

      const tick = (now) => {
        const to = readLogoMetrics(document.getElementById('catalog-hero-brand'));
        if (!to) {
          enterRafRef.current = requestAnimationFrame(tick);
          return;
        }

        if (!fromLocked) {
          const latestHeader = readLogoMetrics(document.getElementById('header-logo'));
          if (latestHeader) fromFrozen = latestHeader;
          fromLocked = true;
          startedAt = now;
          setStyle(buildMorphStyle(fromFrozen, fromFrozen, 0));
          enterRafRef.current = requestAnimationFrame(tick);
          return;
        }

        const t = EASE(clamp((now - startedAt) / ENTER_DURATION_MS, 0, 1));
        setStyle(buildMorphStyle(fromFrozen, to, t));

        if (t < 1) {
          enterRafRef.current = requestAnimationFrame(tick);
          return;
        }

        enteringRef.current = false;
        document.body.classList.remove('home-logo-morph-entering');
        captureHeroStartDoc();
        updateScrollMorph();
      };

      enterRafRef.current = requestAnimationFrame(tick);
    };

    const onScrollOrResize = () => {
      if (enteringRef.current) return;
      if (window.scrollY < 8) {
        startDocRef.current = null;
        captureHeroStartDoc();
      }
      cancelAnimationFrame(rafRef.current);
      rafRef.current = requestAnimationFrame(updateScrollMorph);
    };

    const enteringHome = enabled && !wasEnabled;

    if (enteringHome) {
      runEnterMorph();
    } else {
      const retryId = window.setInterval(() => {
        if (captureHeroStartDoc()) {
          window.clearInterval(retryId);
          updateScrollMorph();
        }
      }, 50);

      window.addEventListener('scroll', onScrollOrResize, { passive: true });
      window.addEventListener('resize', onScrollOrResize);
      updateScrollMorph();

      return () => {
        window.clearInterval(retryId);
        cancelAnimationFrame(rafRef.current);
        cancelAnimationFrame(enterRafRef.current);
        window.removeEventListener('scroll', onScrollOrResize);
        window.removeEventListener('resize', onScrollOrResize);
        enteringRef.current = false;
        clearBody();
      };
    }

    window.addEventListener('scroll', onScrollOrResize, { passive: true });
    window.addEventListener('resize', onScrollOrResize);

    return () => {
      cancelAnimationFrame(rafRef.current);
      cancelAnimationFrame(enterRafRef.current);
      window.removeEventListener('scroll', onScrollOrResize);
      window.removeEventListener('resize', onScrollOrResize);
      enteringRef.current = false;
      clearBody();
    };
  }, [enabled]);

  return { style };
}
