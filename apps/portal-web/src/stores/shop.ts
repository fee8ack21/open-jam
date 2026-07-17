import { defineStore } from 'pinia';
import { catalogApi, storeApi } from '@/api';
import { categoryKeyResolver, toDetailMeta, toProduct } from '@/data/mapCatalog';
import type { ProductDetailMeta } from '@/data/mapCatalog';
import type { Product } from '@/data/products';
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
    // 市集商品（由 CatalogService 載入）
    products: [] as Product[],
    categories: [] as CatalogCategoryDto[],
    // 熱門標籤（跑馬燈用）：後端依已上架商品引用數計算，點擊必有搜尋結果
    popularTags: [] as string[],
    loading: false,
    loaded: false,
    // 已載入的商品詳情 meta（檔案清單 / 格式 / 總大小）快取；同商品在
    // 精選區（products）與 browse 格（queryCatalog 回傳）是不同物件，
    // 快取 meta 才能就地補齊任一份，不重複請求
    detailCache: new Map<string, ProductDetailMeta>(),
    // 商店公開資訊快取（queryCatalog 補商品所屬店家資訊時重用，避免重複請求）
    storeCache: new Map<string, StoreDto>(),
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
        const [catRes, listRes, tagRes] = await Promise.all([
          catalogApi.catalogCategories.list(),
          catalogApi.catalogs.list({ Offset: 0, Limit: 100 }),
          // 熱門標籤失敗不應阻擋市集載入（跑馬燈自有 fallback）
          catalogApi.catalogTags.popular({ Limit: 14 }).catch(() => null),
        ]);
        this.categories = catRes.data ?? [];
        this.popularTags = (tagRes?.data.items ?? [])
          .map((t) => t.name)
          .filter((n): n is string => !!n);
        const catKeyOf = categoryKeyResolver(this.categories);
        const items = listRes.data.items ?? [];

        // 依 storeId 去重後逐一補商店資訊（公開端點），存入快取供列表查詢重用
        await this.resolveStores(items.map((p) => p.storeId));

        this.products = items.map((p) =>
          toProduct(p, catKeyOf(p.categoryId), p.storeId ? this.storeCache.get(p.storeId) : undefined),
        );
        this.loaded = true;
      } catch {
        // 後端暫時不可用：保留空市集（各版位已有空狀態），loaded 未設定可於下次進頁重試
      } finally {
        this.loading = false;
      }
    },

    /** 補齊尚未快取的商店公開資訊（去重後並行查詢，單店失敗不影響其他）。 */
    async resolveStores(storeIds: (string | null | undefined)[]): Promise<void> {
      const missing = [...new Set(storeIds.filter((v): v is string => !!v && !this.storeCache.has(v)))];
      const stores = await Promise.all(
        missing.map((id) =>
          storeApi.stores
            .get(id)
            .then((r) => r.data)
            .catch(() => undefined),
        ),
      );
      stores.forEach((s) => { if (s?.id) this.storeCache.set(s.id, s); });
    },

    /**
     * 依查詢條件向 CatalogService 撈商品列表（browse 格的篩選 / 排序 / 分頁皆走 API，非 in-memory）。
     * 回傳本頁商品與符合條件總數；商品所屬店家資訊由快取補齊。
     */
    async queryCatalog(
      query: Parameters<typeof catalogApi.catalogs.list>[0],
    ): Promise<{ items: Product[]; total: number }> {
      // 分類清單通常已由 loadCatalog 載入；防禦性補載以確保分類鍵解析
      if (!this.categories.length) {
        try { this.categories = (await catalogApi.catalogCategories.list()).data ?? []; } catch { /* 保留空清單 */ }
      }
      const res = await catalogApi.catalogs.list(query);
      const items = res.data.items ?? [];
      await this.resolveStores(items.map((p) => p.storeId));
      const catKeyOf = categoryKeyResolver(this.categories);
      return {
        items: items.map((p) =>
          toProduct(p, catKeyOf(p.categoryId), p.storeId ? this.storeCache.get(p.storeId) : undefined),
        ),
        total: res.data.totalCount ?? items.length,
      };
    },

    /**
     * 載入商品完整資訊，就地補齊 QuickView 檔案 meta（清單 / 格式 / 總大小）與描述。
     * 補在「呼叫端實際顯示的那份 Product」上——browse 格物件不在 products 內，
     * 補錯份會讓 dialog 永遠停在佔位符。
     */
    async loadProductDetail(product: Product): Promise<void> {
      if (!product.id) return;
      const cached = this.detailCache.get(product.id);
      if (cached) {
        Object.assign(product, cached);
        return;
      }
      try {
        const { data } = await catalogApi.catalogs.get(product.id);
        const meta = toDetailMeta(data);
        this.detailCache.set(product.id, meta);
        Object.assign(product, meta);
        // 市集列表（精選 / 熱門區）中同商品的另一份也一併補齊
        const listed = this.products.find((p) => p.id === product.id);
        if (listed && listed !== product) Object.assign(listed, meta);
      } catch {
        // 詳情暫時取不到：保留列表資料（meta 顯示佔位），下次開啟重試
      }
    },
  },
});
