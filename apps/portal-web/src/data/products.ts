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

export interface Product {
  id: string;
  storeSlug: string;
  cat: string;
  hue: number;
  title: string;
  creator: string;
  handle: string;
  avatar: string;
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
  previews: number;
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
