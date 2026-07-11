/// <reference types="react-scripts" />

declare module '*.css';
declare module 'swiper/css';
declare module 'swiper/css/pagination';

declare const process: {
  env: {
    NODE_ENV: 'development' | 'production' | 'test';
    REACT_APP_API_BASE_URL?: string;
    REACT_APP_ALLOWED_PAYMENT_REDIRECT_HOSTS?: string;
  };
};

declare namespace NodeJS {
  type Timeout = ReturnType<typeof setTimeout>;
}

declare const describe: (name: string, fn: () => void) => void;
declare const beforeEach: (fn: () => void) => void;
declare const it: (name: string, fn: () => void | Promise<void>) => void;
declare const expect: <T = unknown>(actual: T) => {
  toBe(expected: unknown): void;
  toEqual(expected: unknown): void;
  toBeNull(): void;
};
