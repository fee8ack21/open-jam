import { createRouter, createWebHistory, type RouteRecordRaw } from 'vue-router';
import DiscoverView from '@/views/DiscoverView.vue';
import LegalView from '@/views/LegalView.vue';
import AboutView from '@/views/AboutView.vue';
import FaqView from '@/views/FaqView.vue';

const routes: RouteRecordRaw[] = [
  { path: '/', name: 'home', component: DiscoverView },
  { path: '/discover', redirect: '/' }, // 舊市集網址導回首頁（外部書籤相容）
  { path: '/about', name: 'about', component: AboutView },
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
