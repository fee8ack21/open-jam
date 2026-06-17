/* ============================================================
   Open Jam — sample catalogue
   Each product carries a `hue` used to tint its striped
   placeholder thumbnail.若帶 `image` 則改用實際商品縮圖。
   Categories: music · photo · ebook
   ============================================================ */

import thumbSilver from '@/assets/images/mock/products/390kosmbz3zg1apt8bi5sf24a3fh.webp';
import thumbLicks from '@/assets/images/mock/products/9cigyxdf9nhxhnueksa2pv17fwih.webp';
import thumbColorful from '@/assets/images/mock/products/misi5oskyotvume2a5869wcca0f0.webp';
import thumbAutumn from '@/assets/images/mock/products/udifsfncosj8km5jxmeif3x2y4sr.webp';
import thumbSurf from '@/assets/images/mock/products/6u49mdf7jaizziz1ts6u9smeymsi.webp';
import thumbInterior from '@/assets/images/mock/products/ccbnhspiu9ljrv2mrf0q051xbts9.webp';
import thumbDragonTales from '@/assets/images/mock/products/9egb3vtgm6w1cbt814ua9modb43d.webp';
import thumbBloodFamily from '@/assets/images/mock/products/p09nkc7x9namoz8q894vyf6j5k0g.webp';
import thumbGoodDeeds from '@/assets/images/mock/products/qxm27a5sv3xag01kvbvlta249g2a.webp';

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
  label: string;
  glyph: string;
}

export const CATEGORIES: Category[] = [
  { id: 'music', label: '樂譜 / 音樂', glyph: 'note' },
  { id: 'photo', label: '攝影 / 照片集', glyph: 'image' },
  { id: 'ebook', label: '電子書 / 文件', glyph: 'book' },
];

export const TAGS: Record<string, string[]> = {
  music: ['鋼琴', '爵士', '古典', '流行', '配樂', '吉他'],
  photo: ['風景', '人像', '街拍', '黑白', '旅行', '空拍'],
  ebook: ['設計', '攝影教學', '商業', '寫作', '行銷'],
};

const F = (type: string, count: number): FileEntry => ({ type, count });

export const PRODUCTS: Product[] = [
  {
    id: 'p01', cat: 'music', hue: 256,
    title: '午夜爵士鋼琴：12 首即興譜集',
    creator: 'Lena Okafor', handle: '@lenakeys', avatar: '#6151f0',
    price: 18, rating: 4.9, ratingCount: 312, sales: 1840,
    tags: ['爵士', '鋼琴'],
    blurb: '12 首為深夜練習而生的爵士鋼琴譜，含完整和弦標記與即興導引。',
    desc: ['這套譜集收錄 12 首原創爵士小品，難度橫跨中級到進階，每一首都附上完整的和弦級數標記、左手 voicing 建議，以及一段即興 solo 的引導範例。',
      '所有譜面以高解析 PDF 輸出，並附 MIDI 與 MuseScore 原始檔，方便你調整調性與速度。適合自學者、教學者與想擴充曲庫的演奏者。'],
    files: [F('PDF', 12), F('MIDI', 12), F('MSCZ', 12)],
    totalSize: '184 MB', formats: ['PDF', 'MIDI', 'MuseScore'],
    contents: [
      { name: '完整譜集（12 首・88 頁）', type: 'PDF', size: '42 MB' },
      { name: 'MIDI 伴奏與分軌', type: 'MIDI', size: '6 MB' },
      { name: 'MuseScore 可編輯原始檔', type: 'MSCZ', size: '12 MB' },
      { name: '練習錄音 Demo（mp3）', type: 'AUDIO', size: '124 MB' },
    ],
    previews: 5,
    image: thumbSilver,
    featured: true,
  },
  {
    id: 'p02', cat: 'photo', hue: 28,
    title: '京都の秋・古都紅葉攝影集',
    creator: 'Haru Tanaka', handle: '@haru.frames', avatar: '#e0823c',
    price: 24, rating: 4.8, ratingCount: 208, sales: 920,
    tags: ['風景', '旅行'],
    blurb: '48 張京都深秋實拍，4K 無浮水印，附 Lightroom 色彩預設。',
    desc: ['於十一月走訪嵐山、東福寺與永觀堂，捕捉古都最濃的一抹秋色。本攝影集收錄 48 張精選作品，全數 4K 解析、無浮水印，可作為桌布、列印或創作參考。',
      '額外附贈 6 組 Lightroom 預設，重現作品中溫潤的紅金色調。'],
    files: [F('JPG', 48), F('XMP', 6)],
    totalSize: '1.2 GB', formats: ['JPG 4K', 'Lightroom 預設'],
    contents: [
      { name: '高解析作品（48 張・4096px）', type: 'JPG', size: '1.1 GB' },
      { name: 'Lightroom 預設組（6 款）', type: 'XMP', size: '2 MB' },
      { name: '列印建議與授權說明', type: 'PDF', size: '4 MB' },
    ],
    previews: 6,
    image: thumbAutumn,
    featured: true,
  },
  {
    id: 'p03', cat: 'ebook', hue: 168,
    title: '獨立創作者的定價心法',
    creator: 'Marcus Wei', handle: '@marcusbuilds', avatar: '#16a07a',
    price: 0, rating: 4.7, ratingCount: 540, sales: 6100,
    tags: ['商業', '行銷'],
    blurb: '免費電子書：用一套框架，為你的數位產品找到對的價格。',
    desc: ['定價是創作者最常焦慮的環節。這本 64 頁的電子書用一套可操作的框架，帶你拆解價值錨點、心理價格與分級策略，並附 3 個真實案例拆解。',
      '完全免費，只希望你把更好的作品帶到世界面前。'],
    files: [F('PDF', 1), F('EPUB', 1)],
    totalSize: '18 MB', formats: ['PDF', 'EPUB'],
    contents: [
      { name: '定價心法（64 頁）', type: 'PDF', size: '12 MB' },
      { name: 'EPUB 行動版', type: 'EPUB', size: '4 MB' },
      { name: '定價試算表', type: 'XLSX', size: '2 MB' },
    ],
    previews: 4,
    image: thumbDragonTales,
    featured: true,
  },
  {
    id: 'p04', cat: 'music', hue: 320,
    title: '極簡電影配樂模板（Cinematic Pack）',
    creator: 'Sora Lim', handle: '@sora.scores', avatar: '#c94f9e',
    price: 32, rating: 5.0, ratingCount: 96, sales: 410,
    tags: ['配樂', '古典'],
    blurb: '8 段情緒配樂的譜面＋分軌，給短片、Podcast 與遊戲。',
    desc: ['8 段為畫面而寫的情緒配樂，涵蓋懸疑、溫柔、史詩等氛圍。每段都提供總譜、分軌音檔與授權，可直接用於短片、Podcast 與獨立遊戲。'],
    files: [F('PDF', 8), F('WAV', 24), F('MIDI', 8)],
    totalSize: '640 MB', formats: ['PDF', 'WAV 分軌', 'MIDI'],
    contents: [
      { name: '總譜（8 段）', type: 'PDF', size: '28 MB' },
      { name: 'WAV 分軌（24 軌）', type: 'WAV', size: '590 MB' },
      { name: 'MIDI 檔', type: 'MIDI', size: '4 MB' },
      { name: '商用授權書', type: 'PDF', size: '1 MB' },
    ],
    previews: 5,
    image: thumbLicks,
    featured: true,
  },
  {
    id: 'p05', cat: 'photo', hue: 210,
    title: '城市夜行・霓虹街拍 RAW 套組',
    creator: 'Diego Ramos', handle: '@diego.night', avatar: '#3b7fd4',
    price: 21, rating: 4.6, ratingCount: 144, sales: 680,
    tags: ['街拍', '黑白'],
    blurb: '36 張 RAW + JPG 夜間街拍，含可商用授權與調色思路。',
    desc: ['橫跨東京、香港與首爾的夜間街頭，36 張霓虹光影作品，提供 RAW 與 JPG 雙格式，附我的調色節點截圖與思路筆記。'],
    files: [F('RAW', 36), F('JPG', 36)],
    totalSize: '2.4 GB', formats: ['RAW', 'JPG', '調色筆記'],
    contents: [
      { name: 'RAW 原始檔（36 張）', type: 'RAW', size: '2.1 GB' },
      { name: 'JPG 成品（36 張）', type: 'JPG', size: '280 MB' },
      { name: '調色思路筆記', type: 'PDF', size: '8 MB' },
    ],
    previews: 6,
    image: thumbSurf,
    featured: true,
  },
  {
    id: 'p06', cat: 'ebook', hue: 44,
    title: '手機攝影構圖 30 講',
    creator: 'Priya Nair', handle: '@priya.shoots', avatar: '#d8a017',
    price: 12, rating: 4.8, ratingCount: 376, sales: 2300,
    tags: ['攝影教學', '設計'],
    blurb: '只用手機，30 堂課練成構圖直覺。圖文＋範例對照。',
    desc: ['不談昂貴器材，只練最重要的「看見」。30 個構圖主題，每講都有正反範例對照與當天就能練的小任務，120 頁全彩圖文。'],
    files: [F('PDF', 1), F('EPUB', 1)],
    totalSize: '96 MB', formats: ['PDF', 'EPUB'],
    contents: [
      { name: '構圖 30 講（120 頁全彩）', type: 'PDF', size: '88 MB' },
      { name: 'EPUB 行動版', type: 'EPUB', size: '8 MB' },
    ],
    previews: 4,
    image: thumbBloodFamily,
  },
  {
    id: 'p07', cat: 'music', hue: 12,
    title: '原聲吉他指彈譜・溫暖系 10 首',
    creator: 'Theo Brandt', handle: '@theo.strings', avatar: '#d65a3a',
    price: 15, rating: 4.7, ratingCount: 188, sales: 1020,
    tags: ['吉他', '流行'],
    blurb: '10 首指彈改編，含六線譜、五線譜與示範影片連結。',
    desc: ['將 10 首耳熟能詳的旋律改編為原聲吉他指彈版，同時提供六線譜（TAB）與五線譜，難度標示清楚，並附我的逐句示範影片連結。'],
    files: [F('PDF', 10), F('GP', 10)],
    totalSize: '72 MB', formats: ['PDF', 'Guitar Pro'],
    contents: [
      { name: '指彈譜集（六線＋五線）', type: 'PDF', size: '54 MB' },
      { name: 'Guitar Pro 原始檔', type: 'GP', size: '16 MB' },
      { name: '示範影片連結清單', type: 'TXT', size: '1 MB' },
    ],
    previews: 5,
    image: thumbColorful,
  },
  {
    id: 'p08', cat: 'photo', hue: 142,
    title: '北歐極簡室內・空間攝影 40 張',
    creator: 'Astrid Holm', handle: '@astrid.space', avatar: '#2f9e6b',
    price: 28, rating: 4.9, ratingCount: 110, sales: 540,
    tags: ['空拍', '人像'],
    blurb: '40 張北歐風室內空間影像，4K，適合設計提案與社群。',
    desc: ['以自然光與留白為核心，40 張北歐極簡室內空間作品。乾淨的構圖與中性色調，特別適合設計提案、品牌情緒板與社群素材。'],
    files: [F('JPG', 40)],
    totalSize: '880 MB', formats: ['JPG 4K'],
    contents: [
      { name: '空間作品（40 張・4K）', type: 'JPG', size: '870 MB' },
      { name: '授權與使用說明', type: 'PDF', size: '3 MB' },
    ],
    previews: 6,
    image: thumbInterior,
    featured: true,
  },
  {
    id: 'p09', cat: 'ebook', hue: 286,
    title: '寫作者的 Notion 系統範本',
    creator: 'Yuki Sato', handle: '@yuki.writes', avatar: '#8b5cf6',
    price: 9, rating: 4.5, ratingCount: 264, sales: 1560,
    tags: ['寫作', '設計'],
    blurb: '一套管理靈感、草稿與發佈的 Notion 範本，附導覽影片。',
    desc: ['一套我每天都在用的寫作系統：靈感收集、草稿看板、發佈日曆與作品庫一次整合。匯入即用，並附 12 分鐘導覽影片連結。'],
    files: [F('NOTION', 1), F('PDF', 1)],
    totalSize: '6 MB', formats: ['Notion 範本', 'PDF 指南'],
    contents: [
      { name: 'Notion 範本連結與匯入檔', type: 'NOTION', size: '1 MB' },
      { name: '設定指南', type: 'PDF', size: '5 MB' },
    ],
    previews: 3,
    image: thumbGoodDeeds,
  },
  {
    id: 'p10', cat: 'music', hue: 198,
    title: '兒童鋼琴啟蒙・趣味練習本',
    creator: 'Lena Okafor', handle: '@lenakeys', avatar: '#6151f0',
    price: 11, rating: 4.8, ratingCount: 142, sales: 760,
    tags: ['鋼琴', '古典'],
    blurb: '40 頁圖像化練習本，把音階與節奏變成遊戲。',
    desc: ['為 5–9 歲初學者設計的鋼琴練習本，用圖像與貼紙任務把枯燥的音階、節奏練習變成遊戲，40 頁可列印。'],
    files: [F('PDF', 2)],
    totalSize: '46 MB', formats: ['PDF 可列印'],
    contents: [
      { name: '練習本（40 頁・全彩）', type: 'PDF', size: '40 MB' },
      { name: '貼紙與獎勵卡', type: 'PDF', size: '6 MB' },
    ],
    previews: 4,
  },
  {
    id: 'p11', cat: 'photo', hue: 350,
    title: '花卉微距・植物標本影像庫',
    creator: 'Astrid Holm', handle: '@astrid.space', avatar: '#2f9e6b',
    price: 19, rating: 4.6, ratingCount: 88, sales: 320,
    tags: ['風景', '黑白'],
    blurb: '60 張花卉與植物微距作品，純白背景，去背即用。',
    desc: ['60 張在純白背景上拍攝的花卉與植物微距作品，附去背 PNG，可直接用於拼貼、版面與品牌設計。'],
    files: [F('JPG', 60), F('PNG', 60)],
    totalSize: '1.6 GB', formats: ['JPG', 'PNG 去背'],
    contents: [
      { name: 'JPG 作品（60 張）', type: 'JPG', size: '900 MB' },
      { name: 'PNG 去背版（60 張）', type: 'PNG', size: '700 MB' },
    ],
    previews: 6,
  },
  {
    id: 'p12', cat: 'ebook', hue: 226,
    title: '品牌字體搭配指南 + 範本',
    creator: 'Marcus Wei', handle: '@marcusbuilds', avatar: '#16a07a',
    price: 16, rating: 4.9, ratingCount: 174, sales: 880,
    tags: ['設計', '商業'],
    blurb: '80 頁字體搭配心法，附 20 組可商用配對與 Figma 範本。',
    desc: ['從字體分類、對比到實戰搭配，80 頁系統化拆解，附 20 組精選可商用字體配對，以及一份可直接套用的 Figma 範本。'],
    files: [F('PDF', 1), F('FIG', 1)],
    totalSize: '64 MB', formats: ['PDF', 'Figma 範本'],
    contents: [
      { name: '字體搭配指南（80 頁）', type: 'PDF', size: '58 MB' },
      { name: 'Figma 範本檔', type: 'FIG', size: '6 MB' },
    ],
    previews: 4,
  },
];
