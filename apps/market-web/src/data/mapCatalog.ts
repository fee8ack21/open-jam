/* ============================================================
   CatalogService DTO → 市集 Product 對應
   市集列出全站已上架商品；每筆商品的「創作者」資訊（店名 / slug /
   頭像）由 StoreService 依 storeId 補上。rating / sales / 檔案清單等
   後端尚無資料，以預設值補齊讓既有市集 UI 不需大改即可顯示。
   ============================================================ */
import type {
  CatalogSummaryDto,
  CatalogCategoryDto,
} from '@/api/catalog-service';
import type { StoreDto } from '@/api/store-service';
import type { Product } from '@/data/products';

const ROOT_SLUG_TO_KEY: Record<string, string> = {
  music: 'music',
  photography: 'photo',
  ebook: 'ebook',
};

/** categoryId → 前端分類鍵（沿 parentId 走到頂層分類後對應）。 */
export function categoryKeyResolver(categories: CatalogCategoryDto[]) {
  const byId = new Map(categories.filter((c) => c.id).map((c) => [c.id as string, c]));
  return (categoryId?: string | null): string => {
    let cur = categoryId ? byId.get(categoryId) : undefined;
    const seen = new Set<string>();
    while (cur?.parentId && cur.id && !seen.has(cur.id)) {
      seen.add(cur.id);
      cur = byId.get(cur.parentId);
    }
    return (cur?.slug && ROOT_SLUG_TO_KEY[cur.slug]) || 'music';
  };
}

/** 由 coverHue 產生穩定的頭像底色（mock avatar 欄位是 CSS 顏色而非圖片）。 */
export function hueColor(hue: number): string {
  return `hsl(${hue} 70% 58%)`;
}

/** CatalogSummaryDto + 其所屬商店 → 市集 Product。 */
export function toProduct(
  dto: CatalogSummaryDto,
  catKey: string,
  store: StoreDto | undefined,
): Product {
  return {
    id: dto.id ?? '',
    storeSlug: store?.storeSlug ?? '',
    cat: catKey,
    hue: dto.coverHue ?? 256,
    title: dto.name ?? '',
    creator: store?.storeName ?? '創作者',
    handle: store?.storeSlug ? '@' + store.storeSlug : '',
    avatar: hueColor(dto.coverHue ?? 256),
    price: dto.price ?? 0,
    rating: dto.ratingAverage ?? 0,
    ratingCount: dto.ratingCount ?? 0,
    sales: dto.salesCount ?? 0,
    tags: dto.tags ?? [],
    blurb: dto.summary ?? '',
    desc: [],
    files: [],
    totalSize: '—',
    formats: [],
    contents: [],
    previews: 0,
    image: dto.thumbnailUrl ?? undefined,
    featured: dto.isFeatured ?? false,
  };
}
