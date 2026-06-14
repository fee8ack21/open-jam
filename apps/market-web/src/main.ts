/* ============================================================
   main.ts — app bootstrap
   Vue 3 · Pinia · Vue Router · Naive UI · global icon set
   ============================================================ */
import { createApp } from 'vue';
import { createPinia } from 'pinia';
import naive from 'naive-ui';

import App from './App.vue';
import router from './router';

import './assets/styles/base.css';
import './assets/styles/market.css';

import JIcon from './components/JIcon.vue';
import Stars from './components/Stars.vue';
import ProductThumb from './components/ProductThumb.vue';
import MktCard from './components/MktCard.vue';

const app = createApp(App);

app.use(createPinia());
app.use(router);
app.use(naive);

app.component('JIcon', JIcon);
app.component('Stars', Stars);
app.component('ProductThumb', ProductThumb);
app.component('MktCard', MktCard);

app.mount('#app');
