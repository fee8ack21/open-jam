/* ============================================================
   Vue Router — SPA route map
   /                       → 市場集 hub (MarketView)
   /shop                   → storefront list (inside ShopLayout)
   /shop/product/:id       → product detail
   /shop/checkout          → checkout
   ============================================================ */
import { createRouter, createWebHistory } from 'vue-router';

import MarketView from '@/views/MarketView.vue';
import ShopLayout from '@/layouts/ShopLayout.vue';
import ListView from '@/views/ListView.vue';
import DetailView from '@/views/DetailView.vue';
import CheckoutView from '@/views/CheckoutView.vue';

const routes = [
  { path: '/', name: 'market', component: MarketView },
  {
    path: '/shop',
    component: ShopLayout,
    children: [
      { path: '', name: 'shop-list', component: ListView },
      { path: 'product/:id', name: 'product', component: DetailView, props: true },
      { path: 'checkout', name: 'checkout', component: CheckoutView },
    ],
  },
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
