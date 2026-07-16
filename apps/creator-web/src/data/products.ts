/* ============================================================
   Open Jam — catalogue 型別與分類 / 標籤定義 (ESM module)
   Each product carries a `hue` used to tint its striped
   placeholder thumbnail.若帶 `image` 則改用實際商品縮圖。
   Categories: music · photo · ebook
   商品資料一律由 CatalogService API 取得（見 data/mapCatalog.ts）。
   ============================================================ */

/** 商品分類。 */
export interface Category {
  id: string;
  glyph: string;
}

/** 商品內含檔案數量摘要。 */
export interface ProductFile {
  type: string;
  count: number;
}

/** 商品內含檔案項目。 */
export interface ProductContent {
  name: string;
  type: string;
  size: string;
}

/** 商品頁圖庫的預覽媒體項目（圖片 / 上傳影片 / YouTube 嵌入）。 */
export interface PreviewMediaItem {
  kind: 'image' | 'video' | 'youtube';
  /** image / video 為公開讀取 URL；youtube 為正規化 watch 網址。 */
  url: string;
  /** YouTube 影片 ID（縮圖與 embed 用，僅 kind = 'youtube'）。 */
  youtubeId?: string;
}

/** 市集販售的商品。 */
export interface Product {
  id: string;
  cat: string;
  hue: number;
  title: string;
  creator: string;
  handle: string;
  avatar: string;
  /** 商店頭像公開 URL（後端 StoreDto.avatarUrl）；未設定時退回 avatar 底色 + 縮寫。 */
  avatarUrl?: string | null;
  price: number;
  rating: number;
  ratingCount: number;
  sales: number;
  /** 詳情頁累計瀏覽次數（mock 資料未提供）。 */
  views?: number;
  tags: string[];
  blurb: string;
  desc: string[];
  files: ProductFile[];
  totalSize: string;
  formats: string[];
  contents: ProductContent[];
  previews: number;
  /** 店長精選旗標（後端 isStoreFeatured，由商店 Owner 標記）；店面首頁 spotlight 優先展示。 */
  featured?: boolean;
  /** 店長精選顯示排序（後端 storeFeaturedSortOrder，小者在前）；非精選商品此值無意義。 */
  featuredOrder?: number;
  /** 實際商品縮圖（webp import URL）；未設定時退回程式產生的佔位縮圖。 */
  image?: string;
  /** 預覽媒體清單（後端 Screenshot / PreviewVideo / ExternalVideo 資產，依 sortOrder 混排）；商品頁圖庫展示。 */
  previewMedia?: PreviewMediaItem[];
  /** 目前版本 ID（下單必填，來自 CatalogService 商品詳情的 currentVersion）。 */
  versionId?: string;
  /** 幣別（ISO 4217，如 "TWD"、"usd"）；下單與付款沿用，未設定時預設 usd。 */
  currency?: string;
}

// 顯示名稱改由 i18n 提供（key：`category.<id>`），此處僅保留 id 與圖示。
export const CATEGORIES: Category[] = [
  { id: 'music', glyph: 'note' },
  { id: 'photo', glyph: 'image' },
  { id: 'ebook', glyph: 'book' },
];

export const TAGS: Record<string, string[]> = {
  music: ['鋼琴', '爵士', '古典', '流行', '配樂', '吉他'],
  photo: ['風景', '人像', '街拍', '黑白', '旅行', '空拍'],
  ebook: ['設計', '攝影教學', '商業', '寫作', '行銷'],
};
