const { createProxyMiddleware } = require('http-proxy-middleware');

/**
 * Dev-прокси: фронт ходит на same-origin `/api`, без cross-origin и без
 * самоподписанного HTTPS-сертификата API (иначе axios получает Network Error,
 * хотя бэкенд сессию уже создал).
 */
const API_TARGET = process.env.INSHOP_API_PROXY_TARGET || 'http://localhost:5269';

module.exports = function setupProxy(app) {
  const proxy = createProxyMiddleware({
    target: API_TARGET,
    changeOrigin: true,
    secure: false,
  });

  app.use('/api', proxy);
  app.use('/uploads', proxy);
  app.use('/images', proxy);
};
