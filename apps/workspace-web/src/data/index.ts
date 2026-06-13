/* ============================================================
   Open Jam Creator Studio — sample data
   "ME" is a creator who both SELLS their own digital goods and
   BUYS from others. Swap these arrays for API calls when wiring
   up a real backend.
   ============================================================ */

/** 商品上架狀態。 */
export type ProductStatus = 'live' | 'draft' | 'off' | 'review'

/** 商品分類。 */
export interface Category {
  id: string
  label: string
  glyph: string
}

/** 登入中的創作者。 */
export interface Creator {
  name: string
  handle: string
  avatar: string
  bio: string
  email: string
  joined: string
  followers: number
}

/** 商品內含檔案項目。 */
export interface ProductContent {
  name: string
  type: string
  size: string
}

/** 我販售的商品。 */
export interface Product {
  id: string
  cat: string
  hue: number
  status: ProductStatus
  title: string
  creator: string
  handle: string
  avatar: string
  price: number
  rating: number
  ratingCount: number
  sales: number
  revenue: number
  views: number
  tags: string[]
  formats: string[]
  totalSize: string
  created: string
  updated: string
  contents: ProductContent[]
  previews: number
}

/** 我商品的訂單。 */
export interface Order {
  id: string
  productId: string
  buyer: string
  buyerHandle: string
  avatar: string
  amount: number
  status: string
  date: string
}

/** 購買紀錄中的商品快照。 */
export interface PurchaseProduct {
  id: string
  cat: string
  hue: number
  title: string
  creator: string
  handle: string
  avatar: string
  formats: string[]
  totalSize: string
  rating: number
  contents: ProductContent[]
}

/** 我向其他創作者購買的紀錄。 */
export interface Purchase {
  orderId: string
  date: string
  price: number
  product: PurchaseProduct
}

/** 願望清單項目。 */
export interface WishlistItem {
  id: string
  cat: string
  hue: number
  title: string
  creator: string
  handle: string
  avatar: string
  price: number
  rating: number
  ratingCount: number
  tags: string[]
  formats: string[]
  totalSize: string
}

/** 營收分析資料。 */
export interface Revenue {
  monthly: { label: string; value: number }[]
  byCategory: { label: string; value: number; color: string }[]
}

export const CATEGORIES: Category[] = [
  { id: 'music', label: '樂譜 / 音樂', glyph: 'note' },
  { id: 'photo', label: '攝影 / 照片集', glyph: 'image' },
  { id: 'ebook', label: '電子書 / 文件', glyph: 'book' },
]

export const CAT_DESC: Record<string, string> = {
  music: '樂譜、配樂、分軌音檔',
  photo: '照片集、RAW、預設',
  ebook: '電子書、範本、文件',
}

export const TAGS: Record<string, string[]> = {
  music: ['鋼琴', '爵士', '古典', '流行', '配樂', '吉他', '電子'],
  photo: ['風景', '人像', '街拍', '黑白', '旅行', '空拍', '微距'],
  ebook: ['設計', '攝影教學', '商業', '寫作', '行銷', '生產力'],
}

// ---- the signed-in creator ----
export const ME: Creator = {
  name: 'Mira Chen', handle: '@mira.studio', avatar: '#6c4cf1',
  bio: '視覺設計師與攝影創作者，分享可商用的素材與模板。',
  email: 'mira@studiomail.co',
  joined: '2024-03',
  followers: 3820,
}

// ---- products I sell (status: live | draft | off | review) ----
export const MY_PRODUCTS: Product[] = [
  {
    id: 'm01', cat: 'photo', hue: 256, status: 'live',
    title: '城市清晨・極簡建築攝影集 40 張',
    creator: 'Mira Chen', handle: '@mira.studio', avatar: '#6c4cf1',
    price: 26, rating: 4.9, ratingCount: 184, sales: 612, revenue: 15912, views: 8420,
    tags: ['風景', '旅行'], formats: ['JPG 4K', 'Lightroom 預設'], totalSize: '1.1 GB',
    created: '2025-11-02', updated: '2026-05-18',
    contents: [
      { name: '高解析作品（40 張・4K）', type: 'JPG', size: '1.0 GB' },
      { name: 'Lightroom 預設組（5 款）', type: 'XMP', size: '2 MB' },
    ],
    previews: 6,
  },
  {
    id: 'm02', cat: 'ebook', hue: 168, status: 'live',
    title: '設計師的字體搭配速查手冊',
    creator: 'Mira Chen', handle: '@mira.studio', avatar: '#6c4cf1',
    price: 14, rating: 4.8, ratingCount: 246, sales: 1240, revenue: 17360, views: 12030,
    tags: ['設計', '生產力'], formats: ['PDF', 'Figma 範本'], totalSize: '58 MB',
    created: '2025-08-14', updated: '2026-04-30',
    contents: [
      { name: '速查手冊（72 頁）', type: 'PDF', size: '52 MB' },
      { name: 'Figma 範本檔', type: 'FIG', size: '6 MB' },
    ],
    previews: 4,
  },
  {
    id: 'm03', cat: 'photo', hue: 320, status: 'live',
    title: '霓虹夜景・去背 PNG 素材庫',
    creator: 'Mira Chen', handle: '@mira.studio', avatar: '#6c4cf1',
    price: 19, rating: 4.7, ratingCount: 92, sales: 308, revenue: 5852, views: 4110,
    tags: ['街拍', '微距'], formats: ['PNG 去背', 'JPG'], totalSize: '740 MB',
    created: '2026-01-09', updated: '2026-05-02',
    contents: [
      { name: 'PNG 去背素材（60 張）', type: 'PNG', size: '640 MB' },
      { name: '使用說明與授權', type: 'PDF', size: '4 MB' },
    ],
    previews: 6,
  },
  {
    id: 'm04', cat: 'ebook', hue: 44, status: 'draft',
    title: '攝影後製・色彩分級工作流程（草稿）',
    creator: 'Mira Chen', handle: '@mira.studio', avatar: '#6c4cf1',
    price: 22, rating: 0, ratingCount: 0, sales: 0, revenue: 0, views: 0,
    tags: ['攝影教學', '設計'], formats: ['PDF', '影片連結'], totalSize: '— ',
    created: '2026-05-21', updated: '2026-05-26',
    contents: [
      { name: '工作流程指南（撰寫中）', type: 'PDF', size: '— ' },
    ],
    previews: 0,
  },
  {
    id: 'm05', cat: 'music', hue: 198, status: 'review',
    title: '環境氛圍配樂・專注工作 8 段',
    creator: 'Mira Chen', handle: '@mira.studio', avatar: '#6c4cf1',
    price: 28, rating: 0, ratingCount: 0, sales: 0, revenue: 0, views: 0,
    tags: ['配樂', '電子'], formats: ['WAV', 'MP3'], totalSize: '520 MB',
    created: '2026-05-24', updated: '2026-05-28',
    contents: [
      { name: 'WAV 高音質（8 段）', type: 'WAV', size: '480 MB' },
      { name: 'MP3 行動版', type: 'MP3', size: '40 MB' },
    ],
    previews: 3,
  },
  {
    id: 'm06', cat: 'photo', hue: 142, status: 'off',
    title: '舊版・森林系桌布包 2024',
    creator: 'Mira Chen', handle: '@mira.studio', avatar: '#6c4cf1',
    price: 9, rating: 4.5, ratingCount: 60, sales: 410, revenue: 3690, views: 6200,
    tags: ['風景', '旅行'], formats: ['JPG 4K'], totalSize: '320 MB',
    created: '2024-10-01', updated: '2025-12-11',
    contents: [{ name: '桌布作品（24 張・4K）', type: 'JPG', size: '320 MB' }],
    previews: 5,
  },
]

// ---- incoming orders (sales of my products) ----
export const ORDERS: Order[] = [
  { id: 'JUN-8FK2A', productId: 'm02', buyer: 'Diego Ramos', buyerHandle: '@diego.night', avatar: '#3b7fd4', amount: 14, status: 'paid', date: '2026-05-29T14:22:00' },
  { id: 'JUN-7QW1B', productId: 'm01', buyer: 'Astrid Holm', buyerHandle: '@astrid.space', avatar: '#2f9e6b', amount: 26, status: 'paid', date: '2026-05-29T09:10:00' },
  { id: 'JUN-7M5C9', productId: 'm02', buyer: 'Yuki Sato', buyerHandle: '@yuki.writes', avatar: '#8b5cf6', amount: 14, status: 'paid', date: '2026-05-28T20:48:00' },
  { id: 'JUN-6TZ4D', productId: 'm03', buyer: 'Theo Brandt', buyerHandle: '@theo.strings', avatar: '#d65a3a', amount: 19, status: 'paid', date: '2026-05-28T11:30:00' },
  { id: 'JUN-6PL8E', productId: 'm01', buyer: 'Priya Nair', buyerHandle: '@priya.shoots', avatar: '#d8a017', amount: 26, status: 'refunded', date: '2026-05-27T16:05:00' },
  { id: 'JUN-5RX2F', productId: 'm02', buyer: 'Sora Lim', buyerHandle: '@sora.scores', avatar: '#c94f9e', amount: 14, status: 'paid', date: '2026-05-27T08:51:00' },
  { id: 'JUN-5HB3G', productId: 'm03', buyer: 'Marcus Wei', buyerHandle: '@marcusbuilds', avatar: '#16a07a', amount: 19, status: 'paid', date: '2026-05-26T19:14:00' },
  { id: 'JUN-4KD9H', productId: 'm02', buyer: 'Lena Okafor', buyerHandle: '@lenakeys', avatar: '#6151f0', amount: 14, status: 'paid', date: '2026-05-26T10:02:00' },
  { id: 'JUN-4AN1J', productId: 'm01', buyer: 'Haru Tanaka', buyerHandle: '@haru.frames', avatar: '#e0823c', amount: 26, status: 'paid', date: '2026-05-25T22:40:00' },
]

// ---- products I bought from other creators ----
export const PURCHASES: Purchase[] = [
  {
    orderId: 'JUN-PB12K', date: '2026-05-20', price: 24,
    product: {
      id: 'p02', cat: 'photo', hue: 28, title: '京都の秋・古都紅葉攝影集',
      creator: 'Haru Tanaka', handle: '@haru.frames', avatar: '#e0823c',
      formats: ['JPG 4K', 'Lightroom 預設'], totalSize: '1.2 GB', rating: 4.8,
      contents: [
        { name: '高解析作品（48 張・4096px）', type: 'JPG', size: '1.1 GB' },
        { name: 'Lightroom 預設組（6 款）', type: 'XMP', size: '2 MB' },
      ],
    },
  },
  {
    orderId: 'JUN-PB09M', date: '2026-04-28', price: 0,
    product: {
      id: 'p03', cat: 'ebook', hue: 168, title: '獨立創作者的定價心法',
      creator: 'Marcus Wei', handle: '@marcusbuilds', avatar: '#16a07a',
      formats: ['PDF', 'EPUB'], totalSize: '18 MB', rating: 4.7,
      contents: [
        { name: '定價心法（64 頁）', type: 'PDF', size: '12 MB' },
        { name: 'EPUB 行動版', type: 'EPUB', size: '4 MB' },
      ],
    },
  },
  {
    orderId: 'JUN-PA77X', date: '2026-04-11', price: 32,
    product: {
      id: 'p04', cat: 'music', hue: 320, title: '極簡電影配樂模板（Cinematic Pack）',
      creator: 'Sora Lim', handle: '@sora.scores', avatar: '#c94f9e',
      formats: ['PDF', 'WAV 分軌', 'MIDI'], totalSize: '640 MB', rating: 5.0,
      contents: [
        { name: '總譜（8 段）', type: 'PDF', size: '28 MB' },
        { name: 'WAV 分軌（24 軌）', type: 'WAV', size: '590 MB' },
      ],
    },
  },
  {
    orderId: 'JUN-P9920', date: '2026-03-02', price: 16,
    product: {
      id: 'p12', cat: 'ebook', hue: 226, title: '品牌字體搭配指南 + 範本',
      creator: 'Marcus Wei', handle: '@marcusbuilds', avatar: '#16a07a',
      formats: ['PDF', 'Figma 範本'], totalSize: '64 MB', rating: 4.9,
      contents: [
        { name: '字體搭配指南（80 頁）', type: 'PDF', size: '58 MB' },
        { name: 'Figma 範本檔', type: 'FIG', size: '6 MB' },
      ],
    },
  },
]

// ---- wishlist (saved from storefront, not yet bought) ----
export const WISHLIST: WishlistItem[] = [
  { id: 'w1', cat: 'photo', hue: 210, title: '城市夜行・霓虹街拍 RAW 套組', creator: 'Diego Ramos', handle: '@diego.night', avatar: '#3b7fd4', price: 21, rating: 4.6, ratingCount: 144, tags: ['街拍', '黑白'], formats: ['RAW', 'JPG', '調色筆記'], totalSize: '2.4 GB' },
  { id: 'w2', cat: 'music', hue: 256, title: '午夜爵士鋼琴：12 首即興譜集', creator: 'Lena Okafor', handle: '@lenakeys', avatar: '#6151f0', price: 18, rating: 4.9, ratingCount: 312, tags: ['爵士', '鋼琴'], formats: ['PDF', 'MIDI', 'MuseScore'], totalSize: '184 MB' },
  { id: 'w3', cat: 'ebook', hue: 44, title: '手機攝影構圖 30 講', creator: 'Priya Nair', handle: '@priya.shoots', avatar: '#d8a017', price: 12, rating: 4.8, ratingCount: 376, tags: ['攝影教學', '設計'], formats: ['PDF', 'EPUB'], totalSize: '96 MB' },
  { id: 'w4', cat: 'photo', hue: 142, title: '北歐極簡室內・空間攝影 40 張', creator: 'Astrid Holm', handle: '@astrid.space', avatar: '#2f9e6b', price: 28, rating: 4.9, ratingCount: 110, tags: ['空拍', '人像'], formats: ['JPG 4K'], totalSize: '880 MB' },
  { id: 'w5', cat: 'ebook', hue: 286, title: '寫作者的 Notion 系統範本', creator: 'Yuki Sato', handle: '@yuki.writes', avatar: '#8b5cf6', price: 9, rating: 4.5, ratingCount: 264, tags: ['寫作', '設計'], formats: ['Notion 範本', 'PDF 指南'], totalSize: '6 MB' },
]

// ---- revenue analytics ----
export const REVENUE: Revenue = {
  // last 8 months net revenue
  monthly: [
    { label: '10月', value: 3120 }, { label: '11月', value: 4280 },
    { label: '12月', value: 5640 }, { label: '1月', value: 5010 },
    { label: '2月', value: 6320 }, { label: '3月', value: 7180 },
    { label: '4月', value: 8240 }, { label: '5月', value: 9460 },
  ],
  // category revenue split
  byCategory: [
    { label: '攝影 / 照片', value: 25454, color: 'var(--c-pink)' },
    { label: '電子書 / 文件', value: 17360, color: 'var(--c-cyan)' },
    { label: '樂譜 / 音樂', value: 0, color: 'var(--c-violet)' },
  ],
}
