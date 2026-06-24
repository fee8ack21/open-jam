/* ============================================================
   CatalogService DTO → 店面 Product 對應
   後端公開瀏覽端點回傳的欄位較精簡，rating / sales / 檔案清單等
   後端尚無資料，於此以預設值補齊，讓既有店面 UI 不需大改即可顯示。
   ============================================================ */
import type {
  CatalogSummaryDto,
  CatalogDto,
  CatalogCategoryDto,
} from '@/api/catalog-service';
import type { Product } from '@/data/products';
import type { Store } from '@/data/store';
import type { StoreDto } from '@/api/store-service';

/** 後端頂層分類 slug → 前端店面分類鍵。 */
const ROOT_SLUG_TO_KEY: Record<string, string> = {
  music: 'music',
  photography: 'photo',
  ebook: 'ebook',
};

/**
 * 由分類清單建立 categoryId → 前端分類鍵（music / photo / ebook）的解析器：
 * 沿 parentId 走到頂層分類，再以其 slug 對應前端鍵；無法對應時回 'music'。
 */
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

/** 店家顯示資訊（套到每張商品卡片的創作者欄位）。 */
export interface StoreInfo {
  creator: string;
  handle: string;
  avatar: string;
}

/** 由 coverHue 產生穩定的頭像底色（mock avatar 欄位是 CSS 顏色而非圖片）。 */
export function hueColor(hue: number): string {
  return `hsl(${hue} 70% 58%)`;
}

/** CatalogSummaryDto / CatalogDto → 店面 Product。 */
export function toProduct(
  dto: CatalogSummaryDto | CatalogDto,
  catKey: string,
  storeInfo: StoreInfo,
): Product {
  const detail = dto as CatalogDto;
  const description = detail.description ?? null;
  return {
    id: dto.id ?? '',
    cat: catKey,
    hue: dto.coverHue ?? 256,
    title: dto.name ?? '',
    creator: storeInfo.creator,
    handle: storeInfo.handle,
    avatar: storeInfo.avatar,
    price: dto.price ?? 0,
    rating: 0,
    ratingCount: 0,
    sales: 0,
    tags: detail.tags ?? [],
    blurb: dto.summary ?? '',
    desc: description ? description.split(/\n+/).map((s) => s.trim()).filter(Boolean) : [],
    files: [],
    totalSize: '—',
    formats: [],
    contents: [],
    previews: 0,
    image: dto.thumbnailUrl ?? undefined,
    versionId: detail.currentVersion?.id ?? undefined,
    currency: dto.currency ?? undefined,
  };
}

/** StoreDto → 店面 Store（mock 同型別）。 */
export function toStore(dto: StoreDto): Store {
  return {
    id: dto.id ?? '',
    storeName: dto.storeName ?? '',
    storeSlug: dto.storeSlug ?? '',
    description: dto.description ?? null,
    avatarUrl: dto.avatarUrl ?? null,
    bannerUrl: dto.bannerUrl ?? null,
    status: (dto.status as Store['status']) ?? 'Active',
    createdAt: dto.createdAt ?? new Date().toISOString(),
    updatedAt: dto.updatedAt ?? null,
  };
}
