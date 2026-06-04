/* ============================================================
   Open Jam — router (vue-router 4)

   NOTE on history mode:
   We use createWebHashHistory() so the app runs from a plain
   static file (the in-tool preview, file:// or any host without
   SPA rewrite rules). When you move this into a real server with
   history fallback, switch to createWebHistory(import.meta.env.BASE_URL).
   ============================================================ */
import { createRouter, createWebHashHistory } from 'vue-router';
import ListView from '../views/ListView.js';
import DetailView from '../views/DetailView.js';
import CheckoutView from '../views/CheckoutView.js';

export const routes = [
  { path: '/', name: 'list', component: ListView },
  { path: '/product/:id', name: 'product', component: DetailView },
  { path: '/checkout', name: 'checkout', component: CheckoutView },
  { path: '/:pathMatch(.*)*', redirect: { name: 'list' } },
];

const router = createRouter({
  history: createWebHashHistory(),
  routes,
  scrollBehavior() {
    return { top: 0 };
  },
});

export default router;
