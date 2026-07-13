import { createRouter, createWebHistory, type RouteRecordRaw } from 'vue-router';
import DiscoverView from '@/views/DiscoverView.vue';
import LegalView from '@/views/LegalView.vue';
import AboutView from '@/views/AboutView.vue';
import FaqView from '@/views/FaqView.vue';
import { hasCookie, setCookie } from '@/utils/cookies';

// 首次造訪標記：無此 cookie 者第一次進首頁時導向品牌 landing 頁
const LANDING_SEEN_COOKIE = 'oj_landing_seen';
const LANDING_SEEN_DAYS = 365;

const routes: RouteRecordRaw[] = [
  { path: '/', name: 'home', component: DiscoverView },
  { path: '/discover', redirect: '/' }, // 舊市集網址導回首頁（外部書籤相容）
  { path: '/landing', name: 'landing', component: () => import('@/views/LandingView.vue') }, // 品牌敘事捲動頁
  { path: '/about', name: 'about', component: AboutView },
  { path: '/faq', name: 'faq', component: FaqView },
  { path: '/terms', name: 'terms', component: LegalView, props: { doc: 'terms' } },
  { path: '/privacy', name: 'privacy', component: LegalView, props: { doc: 'privacy' } },
  { path: '/:pathMatch(.*)*', name: 'not-found', component: () => import('@/views/NotFoundView.vue') },
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

router.beforeEach((to, from) => {
  // 只攔截「開站後的第一個導航」（from 為初始空路由）直接進首頁的情境；
  // 站內點回首頁不再導向。導向時即寫入 cookie，之後造訪直接進市集。
  if (to.name === 'home' && !from.matched.length && !hasCookie(LANDING_SEEN_COOKIE)) {
    setCookie(LANDING_SEEN_COOKIE, '1', LANDING_SEEN_DAYS);
    return { name: 'landing', replace: true };
  }
  // 直接造訪 landing 頁也視為已看過
  if (to.name === 'landing' && !hasCookie(LANDING_SEEN_COOKIE)) {
    setCookie(LANDING_SEEN_COOKIE, '1', LANDING_SEEN_DAYS);
  }
});

export default router;
