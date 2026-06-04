/* Preview-only shim: maps "pinia" to the global IIFE build
   (window.Pinia) loaded via <script> in preview.html. */
const P = window.Pinia;
export const createPinia = P.createPinia;
export const setActivePinia = P.setActivePinia;
export const defineStore = P.defineStore;
export const storeToRefs = P.storeToRefs;
export const mapStores = P.mapStores;
export const mapState = P.mapState;
export const mapActions = P.mapActions;
export default P;
