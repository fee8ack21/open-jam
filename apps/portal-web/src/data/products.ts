/* ============================================================
   Open Jam — catalogue 型別與分類定義
   Each product carries a `hue` used to tint its striped
   placeholder thumbnail.若帶 `image` 則改用實際商品縮圖。
   Categories: music · photo · ebook
   商品資料一律由 CatalogService API 取得（見 data/mapCatalog.ts
   與 stores/shop.ts 的 loadCatalog）。
   ============================================================ */

export interface FileEntry {
  type: string;
  count: number;
}

export interface ContentItem {
  name: string;
  type: string;
  size: string;
}

/** 圖庫預覽媒體項目（對應 creator-web 商品內頁圖庫，型別與語意一致）。 */
export interface PreviewMediaItem {
  kind: 'image' | 'video' | 'youtube';
  /** image / video 為公開讀取 URL；youtube 為正規化 watch 網址。 */
  url: string;
  /** YouTube 影片 ID（縮圖與 embed 用，僅 kind = 'youtube'）。 */
  youtubeId?: string;
}

export interface Product {
  id: string;
  storeSlug: string;
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
  tags: string[];
  blurb: string;
  desc: string[];
  files: FileEntry[];
  totalSize: string;
  formats: string[];
  contents: ContentItem[];
  /**
   * 預覽媒體（截圖 / 上傳影片 / YouTube 嵌入，依 sortOrder 混排），
   * 內容與 creator-web 商品內頁圖庫一致（縮圖另由 image 帶入排在最前）。
   * 列表摘要無資產資料，於 QuickView 開啟載入詳情時補齊。
   */
  previewMedia: PreviewMediaItem[];
  /** 實際商品縮圖（webp import URL）；未設定時退回程式產生的佔位縮圖。 */
  image?: string;
  /** 編輯精選：顯示於市集首頁頂部的「精選作品」輪播。 */
  featured?: boolean;
}

export interface Category {
  id: string;
  glyph: string;
}

// 顯示名稱改由 i18n 提供（key：`category.<id>`），此處僅保留 id 與圖示。
export const CATEGORIES: Category[] = [
  { id: 'music', glyph: 'note' },
  { id: 'photo', glyph: 'image' },
  { id: 'ebook', glyph: 'book' },
];
