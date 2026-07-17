/* ============================================================
   CatalogService DTO → 店面 Product 對應
   檔案清單 / 總大小 / 格式由完整 CatalogDto 的 currentVersion.assets
   推導；列表摘要（CatalogSummaryDto）無版本資料，於詳情載入時補齊。
   ============================================================ */
import {
  CatalogAssetType,
  type CatalogSummaryDto,
  type CatalogDto,
  type CatalogCategoryDto,
} from '@/api/catalog-service';
import type { PreviewMediaItem, Product, ProductContent, ProductFile } from '@/data/products';
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

/** 前端分類鍵（music / photo / ebook）→ 後端頂層分類 ID，供列表查詢的 CategoryId 篩選；無法對應時回 undefined。 */
export function rootCategoryIdOf(categories: CatalogCategoryDto[], key: string): string | undefined {
  const slug = Object.entries(ROOT_SLUG_TO_KEY).find(([, k]) => k === key)?.[0];
  if (!slug) return undefined;
  return categories.find((c) => !c.parentId && c.slug === slug)?.id ?? undefined;
}

/** 店家顯示資訊（套到每張商品卡片的創作者欄位）。 */
export interface StoreInfo {
  creator: string;
  handle: string;
  avatar: string;
  /** 商店頭像公開 URL；null 表示未設定，卡片退回底色縮寫。 */
  avatarUrl: string | null;
}

/** 由 coverHue 產生穩定的頭像底色（mock avatar 欄位是 CSS 顏色而非圖片）。 */
export function hueColor(hue: number): string {
  return `hsl(${hue} 70% 58%)`;
}

/** 由 YouTube 網址取出影片 ID（後端已正規化為 watch 網址，其餘形式僅防禦性支援）。 */
function youtubeVideoId(url: string): string | null {
  const match =
    /[?&]v=([A-Za-z0-9_-]{11})/.exec(url) ??
    /youtu\.be\/([A-Za-z0-9_-]{11})/.exec(url) ??
    /\/(?:shorts|embed|live)\/([A-Za-z0-9_-]{11})/.exec(url);
  return match ? match[1] : null;
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

/** 展示型資產 → 圖庫預覽媒體項目（不支援的資產型別回 null）。 */
function toPreviewMediaItem(asset: NonNullable<CatalogDto['assets']>[number]): PreviewMediaItem | null {
  if (asset.type === CatalogAssetType.Screenshot && asset.url)
    return { kind: 'image', url: asset.url };
  if (asset.type === CatalogAssetType.PreviewVideo && asset.url)
    return { kind: 'video', url: asset.url };
  if (asset.type === CatalogAssetType.ExternalVideo) {
    const url = asset.externalUrl ?? asset.url;
    const id = url ? youtubeVideoId(url) : null;
    if (url && id) return { kind: 'youtube', url, youtubeId: id };
  }
  return null;
}

/** CatalogSummaryDto / CatalogDto → 店面 Product。 */
export function toProduct(
  dto: CatalogSummaryDto | CatalogDto,
  catKey: string,
  storeInfo: StoreInfo,
): Product {
  const detail = dto as CatalogDto;
  const description = detail.description ?? null;

  // 版本可下載檔（僅完整 CatalogDto 帶有 currentVersion；列表摘要為空清單）
  const versionAssets = (detail.currentVersion?.assets ?? [])
    .slice()
    .sort((a, b) => (a.sortOrder ?? 0) - (b.sortOrder ?? 0));
  const contents: ProductContent[] = versionAssets.map((a) => ({
    name: a.fileName ?? '',
    type: fileTypeOf(a.fileName ?? ''),
    size: formatBytes(a.fileSize ?? 0) || '—',
  }));
  const typeCounts = new Map<string, number>();
  for (const c of contents) typeCounts.set(c.type, (typeCounts.get(c.type) ?? 0) + 1);
  const files: ProductFile[] = [...typeCounts].map(([type, count]) => ({ type, count }));
  const totalBytes = versionAssets.reduce((s, a) => s + (a.fileSize ?? 0), 0);

  return {
    id: dto.id ?? '',
    cat: catKey,
    hue: dto.coverHue ?? 256,
    title: dto.name ?? '',
    creator: storeInfo.creator,
    handle: storeInfo.handle,
    avatar: storeInfo.avatar,
    avatarUrl: storeInfo.avatarUrl,
    price: dto.price ?? 0,
    rating: dto.ratingAverage ?? 0,
    ratingCount: dto.ratingCount ?? 0,
    sales: dto.salesCount ?? 0,
    views: dto.viewCount ?? 0,
    tags: dto.tags ?? [],
    blurb: dto.summary ?? '',
    desc: description ? description.split(/\n+/).map((s) => s.trim()).filter(Boolean) : [],
    files,
    totalSize: formatBytes(totalBytes) || '—',
    formats: [...typeCounts.keys()],
    contents,
    previews: 0,
    image: dto.thumbnailUrl ?? undefined,
    // 預覽媒體（Screenshot / PreviewVideo / ExternalVideo 資產，依 sortOrder 混排）
    // 僅完整 CatalogDto（詳情頁）帶有；列表摘要為空陣列
    previewMedia: (detail.assets ?? [])
      .slice()
      .sort((a, b) => (a.sortOrder ?? 0) - (b.sortOrder ?? 0))
      .map(toPreviewMediaItem)
      .filter((m): m is PreviewMediaItem => m !== null),
    featured: dto.isStoreFeatured ?? false,
    featuredOrder: dto.storeFeaturedSortOrder ?? 0,
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
