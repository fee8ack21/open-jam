/* ============================================================
   Open Jam — router (vue-router 4)

   NOTE on history mode:
   We use createWebHashHistory() so the app runs from a plain
   static file (the in-tool preview, file:// or any host without
   SPA rewrite rules). When you move this into a real server with
   history fallback, switch to createWebHistory(import.meta.env.BASE_URL).
   ============================================================ */
import { createRouter, createWebHistory, type RouteRecordRaw } from 'vue-router';
import ListView from '../views/ListView.vue';
import DetailView from '../views/DetailView.vue';
import CheckoutView from '../views/CheckoutView.vue';
import NotFoundView from '../views/NotFoundView.vue';

export const routes: RouteRecordRaw[] = [
  { path: '/', name: 'list', component: ListView },
  { path: '/product/:id', name: 'product', component: DetailView },
  { path: '/checkout', name: 'checkout', component: CheckoutView },
  { path: '/:pathMatch(.*)*', name: 'not-found', component: NotFoundView },
];

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes,
  scrollBehavior() {
    return { top: 0 };
  },
});

export default router;
