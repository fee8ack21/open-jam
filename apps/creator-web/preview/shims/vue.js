/* Preview-only shim: maps the bare specifier "vue" to the global
   UMD build (window.Vue) loaded via <script> in preview.html.
   Under a real Vite build this file is NOT used — "vue" resolves
   from node_modules instead. */
const V = window.Vue;
export const createApp = V.createApp;
export const createSSRApp = V.createSSRApp;
export const defineComponent = V.defineComponent;
export const h = V.h;
export const ref = V.ref;
export const shallowRef = V.shallowRef;
export const reactive = V.reactive;
export const computed = V.computed;
export const watch = V.watch;
export const watchEffect = V.watchEffect;
export const onMounted = V.onMounted;
export const onUnmounted = V.onUnmounted;
export const onBeforeMount = V.onBeforeMount;
export const onBeforeUnmount = V.onBeforeUnmount;
export const nextTick = V.nextTick;
export const provide = V.provide;
export const inject = V.inject;
export const toRefs = V.toRefs;
export const toRef = V.toRef;
export const markRaw = V.markRaw;
export const defineAsyncComponent = V.defineAsyncComponent;
export default V;
