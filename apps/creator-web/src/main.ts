/* ============================================================
   Open Jam — application entry
   ============================================================ */
import { createApp } from 'vue';
import { createPinia } from 'pinia';
import naive from 'naive-ui';
import App from './App.vue';
import router from './router';

// When you migrate to a full Vite build you can import the stylesheet
// here instead of <link>-ing it from index.html:
//   import './assets/styles/base.css';

const app = createApp(App);
app.use(createPinia());
app.use(router);
app.use(naive);
app.mount('#app');
