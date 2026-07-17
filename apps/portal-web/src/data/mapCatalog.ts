/* ============================================================
   CatalogService DTO → 市集 Product 對應
   市集列出全站已上架商品；每筆商品的「創作者」資訊（店名 / slug /
   頭像）由 StoreService 依 storeId 補上。檔案清單 / 總大小 / 格式由
   完整 CatalogDto 的 currentVersion.assets 推導；列表摘要
   （CatalogSummaryDto）無版本資料，於 QuickView 開啟時載入詳情補齊。
   ============================================================ */
import type {
  CatalogDto,
  CatalogSummaryDto,
  CatalogCategoryDto,
} from '@/api/catalog-service';
import type { StoreDto } from '@/api/store-service';
import type { ContentItem, FileEntry, Product } from '@/data/products';
import i18n from '@/i18n';

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

/** 前端分類鍵（music / photo / ebook）→ 後端頂層分類 ID，供列表查詢的 CategoryId 篩選；無法對應時回 undefined。 */
export function rootCategoryIdOf(categories: CatalogCategoryDto[], key: string): string | undefined {
  const slug = Object.entries(ROOT_SLUG_TO_KEY).find(([, k]) => k === key)?.[0];
  if (!slug) return undefined;
  return categories.find((c) => !c.parentId && c.slug === slug)?.id ?? undefined;
}

/** 由 coverHue 產生穩定的頭像底色（mock avatar 欄位是 CSS 顏色而非圖片）。 */
export function hueColor(hue: number): string {
  return `hsl(${hue} 70% 58%)`;
}

/** 檔案大小人類可讀格式（未知大小回空字串）。 */
export function formatBytes(bytes: number): string {
  if (!bytes || bytes <= 0) return '';
  const units = ['B', 'KB', 'MB', 'GB'];
  let n = bytes;
  let i = 0;
  while (n >= 1024 && i < units.length - 1) { n /= 1024; i++; }
  return `${n >= 10 || i === 0 ? Math.round(n) : n.toFixed(1)} ${units[i]}`;
}

/** 檔名 → 顯示用格式代碼（副檔名大寫，最多 4 字）。 */
function fileTypeOf(name: string): string {
  return (name.split('.').pop() || 'FILE').toUpperCase().slice(0, 4);
}

/** QuickView 詳情補齊欄位（完整 CatalogDto 才有版本檔案與描述）。 */
export interface ProductDetailMeta {
  desc: string[];
  files: FileEntry[];
  totalSize: string;
  formats: string[];
  contents: ContentItem[];
}

/** 完整 CatalogDto → 檔案 meta（清單 / 數量 / 格式 / 總大小）與描述段落。 */
export function toDetailMeta(dto: CatalogDto): ProductDetailMeta {
  const versionAssets = (dto.currentVersion?.assets ?? [])
    .slice()
    .sort((a, b) => (a.sortOrder ?? 0) - (b.sortOrder ?? 0));
  const contents: ContentItem[] = versionAssets.map((a) => ({
    name: a.fileName ?? '',
    type: fileTypeOf(a.fileName ?? ''),
    size: formatBytes(a.fileSize ?? 0) || '—',
  }));
  const typeCounts = new Map<string, number>();
  for (const c of contents) typeCounts.set(c.type, (typeCounts.get(c.type) ?? 0) + 1);
  const totalBytes = versionAssets.reduce((s, a) => s + (a.fileSize ?? 0), 0);
  const description = dto.description ?? '';
  return {
    desc: description ? description.split(/\n+/).map((s) => s.trim()).filter(Boolean) : [],
    files: [...typeCounts].map(([type, count]) => ({ type, count })),
    totalSize: formatBytes(totalBytes) || '—',
    formats: [...typeCounts.keys()],
    contents,
  };
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
    creator: store?.storeName ?? i18n.global.t('creatorFallback'),
    handle: store?.storeSlug ? '@' + store.storeSlug : '',
    avatar: hueColor(dto.coverHue ?? 256),
    avatarUrl: store?.avatarUrl ?? null,
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
