import { createRouter, createWebHistory } from 'vue-router';
import MarketView from '@/views/MarketView.vue';

const routes = [
  { path: '/', name: 'market', component: MarketView },
  { path: '/:pathMatch(.*)*', redirect: '/' },
];

export const router = createRouter({
  history: createWebHistory(),
  routes,
  scrollBehavior(to, from, savedPosition) {
    if (savedPosition) return savedPosition;
    if (to.hash) return { el: to.hash };
    return { top: 0 };
  },
});

export default router;
