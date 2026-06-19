import { defineStore } from 'pinia';
import { catalogApi, storeApi } from '@/api';
import { categoryKeyResolver, toProduct } from '@/data/mapCatalog';
import { PRODUCTS, type Product } from '@/data/products';
import type { CatalogCategoryDto } from '@/api/catalog-service';
import type { StoreDto } from '@/api/store-service';

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
    // 市集商品（由 CatalogService 載入；後端尚未可用時退回示範資料）
    products: [] as Product[],
    categories: [] as CatalogCategoryDto[],
    loading: false,
    loaded: false,
  }),
  actions: {
    setFont(f: Font): void {
      this.font = f;
      localStorage.setItem('openjam.shop.font', JSON.stringify(f));
    },

    /** 載入全站已上架商品；每筆補上所屬商店資訊（店名 / slug / 頭像）。 */
    async loadCatalog(): Promise<void> {
      if (this.loaded || this.loading) return;
      this.loading = true;
      try {
        const [catRes, listRes] = await Promise.all([
          catalogApi.catalogCategories.list(),
          catalogApi.catalogs.list({ Offset: 0, Limit: 100 }),
        ]);
        this.categories = catRes.data ?? [];
        const catKeyOf = categoryKeyResolver(this.categories);
        const items = listRes.data.items ?? [];

        // 依 storeId 去重後逐一補商店資訊（公開端點）
        const storeIds = [...new Set(items.map((p) => p.storeId).filter((v): v is string => !!v))];
        const stores = await Promise.all(
          storeIds.map((id) =>
            storeApi.stores
              .get(id)
              .then((r) => r.data)
              .catch(() => undefined),
          ),
        );
        const storeMap = new Map<string, StoreDto>();
        stores.forEach((s) => { if (s?.id) storeMap.set(s.id, s); });

        this.products = items.map((p) =>
          toProduct(p, catKeyOf(p.categoryId), p.storeId ? storeMap.get(p.storeId) : undefined),
        );
        this.loaded = true;
      } catch {
        // 後端尚未可用：退回示範資料，市集不致空白
        if (!this.products.length) this.products = PRODUCTS.slice();
      } finally {
        this.loading = false;
      }
    },
  },
});
