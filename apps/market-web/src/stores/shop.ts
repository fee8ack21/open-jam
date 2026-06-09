import { defineStore } from 'pinia';

type Font = 'sora' | 'grotesk';

const load = (k: string, fb: Font): Font => {
  try {
    const v = localStorage.getItem('openjam.shop.' + k);
    return v ? (JSON.parse(v) as Font) : fb;
  } catch {
    return fb;
  }
};

export const useShopStore = defineStore('shop', {
  state: () => ({
    font: load('font', 'sora'),
  }),
  actions: {
    setFont(f: Font): void {
      this.font = f;
      localStorage.setItem('openjam.shop.font', JSON.stringify(f));
    },
  },
});
