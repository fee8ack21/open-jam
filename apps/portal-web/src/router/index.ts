import { createRouter, createWebHistory, type RouteRecordRaw } from 'vue-router';
import DiscoverView from '@/views/DiscoverView.vue';
import LegalView from '@/views/LegalView.vue';
import LandingView from '@/views/LandingView.vue';
import FaqView from '@/views/FaqView.vue';

const routes: RouteRecordRaw[] = [
  { path: '/', name: 'landing', component: LandingView },
  { path: '/discover', name: 'discover', component: DiscoverView },
  { path: '/faq', name: 'faq', component: FaqView },
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
