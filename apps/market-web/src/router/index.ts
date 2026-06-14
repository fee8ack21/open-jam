import { createRouter, createWebHistory, type RouteRecordRaw } from 'vue-router';
import MarketView from '@/views/MarketView.vue';
import LegalView from '@/views/LegalView.vue';
import AboutView from '@/views/AboutView.vue';

const routes: RouteRecordRaw[] = [
  { path: '/', name: 'market', component: MarketView },
  { path: '/about', name: 'about', component: AboutView },
  { path: '/terms', name: 'terms', component: LegalView, props: { doc: 'terms' } },
  { path: '/privacy', name: 'privacy', component: LegalView, props: { doc: 'privacy' } },
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
