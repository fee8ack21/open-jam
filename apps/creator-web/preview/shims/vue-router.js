/* Preview-only shim: maps "vue-router" to the global build
   (window.VueRouter) loaded via <script> in preview.html. */
const R = window.VueRouter;
export const createRouter = R.createRouter;
export const createWebHistory = R.createWebHistory;
export const createWebHashHistory = R.createWebHashHistory;
export const createMemoryHistory = R.createMemoryHistory;
export const useRouter = R.useRouter;
export const useRoute = R.useRoute;
export const RouterView = R.RouterView;
export const RouterLink = R.RouterLink;
export default R;
