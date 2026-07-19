/* ============================================================
   main.ts — app bootstrap
   Vue 3 · Pinia · Vue Router · Naive UI · global icon set
   ============================================================ */
import { createApp } from 'vue';
import { createPinia } from 'pinia';
import naive from 'naive-ui';

import App from './App.vue';
import router from './router';
import i18n from './i18n';
import { dismissSplash } from './utils/splash';

import './assets/styles/base.css';
import './assets/styles/market.css';

import AppIcon from './components/app-icon';
import Stars from './components/Stars.vue';
import ProductThumb from './components/ProductThumb.vue';
import ProductCard from './components/ProductCard.vue';

const app = createApp(App);

app.use(createPinia());
app.use(router);
app.use(naive);
app.use(i18n);

app.component('app-icon', AppIcon);
app.component('stars', Stars);
app.component('product-thumb', ProductThumb);
app.component('product-card', ProductCard);

app.mount('#app');

// 首個路由解析完成（含路由懶載 chunk）再淡出 index.html 的全螢幕 splash；
// 6 秒硬上限保底，路由或字型卡住也不擋死頁面
router.isReady().then(() => dismissSplash());
window.setTimeout(() => dismissSplash({ immediate: true }), 6000);
