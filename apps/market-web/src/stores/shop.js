import { defineStore } from 'pinia';

const load = (k, fb) => {
  try {
    const v = localStorage.getItem('openjam.shop.' + k);
    return v ? JSON.parse(v) : fb;
  } catch {
    return fb;
  }
};

export const useShopStore = defineStore('shop', {
  state: () => ({
    font: load('font', 'sora'), // 'sora' (Bricolage) | 'grotesk' (Unbounded)
  }),
  actions: {
    setFont(f) {
      this.font = f;
      localStorage.setItem('openjam.shop.font', JSON.stringify(f));
    },
  },
});
