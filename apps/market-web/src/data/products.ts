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

export const TAGS: Record<string, string[]> = {
  music: ['鋼琴', '爵士', '古典', '流行', '配樂', '吉他'],
  photo: ['風景', '人像', '街拍', '黑白', '旅行', '空拍'],
  ebook: ['設計', '攝影教學', '商業', '寫作', '行銷'],
};

const F = (type: string, count: number): FileEntry => ({ type, count });

export const PRODUCTS: Product[] = [
  {
    id: 'p01', storeSlug: 'lenakeys', cat: 'music', hue: 256,
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
    id: 'p02', storeSlug: 'haru-frames', cat: 'photo', hue: 28,
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
    id: 'p03', storeSlug: 'marcusbuilds', cat: 'ebook', hue: 168,
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
    id: 'p04', storeSlug: 'sora-scores', cat: 'music', hue: 320,
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
    id: 'p05', storeSlug: 'diego-night', cat: 'photo', hue: 210,
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
    id: 'p06', storeSlug: 'priya-shoots', cat: 'ebook', hue: 44,
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
    id: 'p07', storeSlug: 'theo-strings', cat: 'music', hue: 12,
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
    id: 'p08', storeSlug: 'astrid-space', cat: 'photo', hue: 142,
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
    id: 'p09', storeSlug: 'yuki-writes', cat: 'ebook', hue: 286,
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
    id: 'p10', storeSlug: 'lenakeys', cat: 'music', hue: 198,
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
    id: 'p11', storeSlug: 'astrid-space', cat: 'photo', hue: 350,
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
    id: 'p12', storeSlug: 'marcusbuilds', cat: 'ebook', hue: 226,
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
  {
    id: 'p13', storeSlug: 'nocturne-lab', cat: 'music', hue: 234,
    title: 'Lo-Fi 夜間節拍包 Vol.1',
    creator: 'Kei Nakamura', handle: '@kei.nocturne', avatar: '#4c6ef5',
    price: 14, rating: 4.8, ratingCount: 220, sales: 1980,
    tags: ['配樂', '流行'],
    blurb: '24 段可商用 Lo-Fi 節拍與分軌，給 Vlog、直播與讀書背景。',
    desc: ['深夜錄音室誕生的 24 段 Lo-Fi 節拍，全數附分軌與無人聲版本，授權涵蓋 Vlog、Podcast 與直播背景使用。',
      'BPM 與調性都標示清楚，方便你剪輯時對拍。'],
    files: [F('WAV', 48), F('MP3', 24)],
    totalSize: '1.1 GB', formats: ['WAV 分軌', 'MP3'],
    contents: [
      { name: '節拍成品（24 段）', type: 'MP3', size: '180 MB' },
      { name: 'WAV 分軌（48 軌）', type: 'WAV', size: '920 MB' },
      { name: '商用授權書', type: 'PDF', size: '1 MB' },
    ],
    previews: 6,
  },
  {
    id: 'p14', storeSlug: 'lenakeys', cat: 'music', hue: 268,
    title: '爵士和聲入門：從 II–V–I 開始',
    creator: 'Lena Okafor', handle: '@lenakeys', avatar: '#6151f0',
    price: 22, rating: 4.9, ratingCount: 98, sales: 640,
    tags: ['爵士', '鋼琴'],
    blurb: '用 60 頁譜例把爵士和聲拆成可練的小步驟，附練習音檔。',
    desc: ['從最核心的 II–V–I 進行開始，逐步帶你認識延伸音、代理和弦與常用 voicing，60 頁譜例全數可彈可聽。',
      '每章附伴奏音檔，練習時像和真的節奏組合奏。'],
    files: [F('PDF', 1), F('AUDIO', 18)],
    totalSize: '210 MB', formats: ['PDF', 'MP3 伴奏'],
    contents: [
      { name: '和聲講義（60 頁）', type: 'PDF', size: '32 MB' },
      { name: '伴奏音檔（18 段）', type: 'AUDIO', size: '178 MB' },
    ],
    previews: 4,
  },
  {
    id: 'p15', storeSlug: 'theo-strings', cat: 'music', hue: 24,
    title: '木吉他伴奏節奏 20 型',
    creator: 'Theo Brandt', handle: '@theo.strings', avatar: '#d65a3a',
    price: 13, rating: 4.6, ratingCount: 132, sales: 860,
    tags: ['吉他', '流行'],
    blurb: '20 種常用伴奏節奏型，附譜例、慢速示範與練習套路。',
    desc: ['把彈唱最常用的 20 種節奏型整理成一冊：從基本刷法到切音、悶音與 percussive 技巧，每型都有慢速與原速示範音檔。'],
    files: [F('PDF', 1), F('AUDIO', 40)],
    totalSize: '320 MB', formats: ['PDF', 'MP3 示範'],
    contents: [
      { name: '節奏型譜例（44 頁）', type: 'PDF', size: '18 MB' },
      { name: '示範音檔（40 段）', type: 'AUDIO', size: '302 MB' },
    ],
    previews: 5,
  },
  {
    id: 'p16', storeSlug: 'sora-scores', cat: 'music', hue: 306,
    title: '弦樂四重奏婚禮曲集',
    creator: 'Sora Lim', handle: '@sora.scores', avatar: '#c94f9e',
    price: 26, rating: 4.9, ratingCount: 74, sales: 380,
    tags: ['古典', '配樂'],
    blurb: '10 首婚禮定番曲的弦樂四重奏編制，總譜與分譜齊備。',
    desc: ['為婚禮與典禮場合改編的 10 首弦樂四重奏，難度適中、聲部平衡，總譜與各聲部分譜齊備，並附排練建議。'],
    files: [F('PDF', 50)],
    totalSize: '96 MB', formats: ['PDF 總譜＋分譜'],
    contents: [
      { name: '總譜（10 首）', type: 'PDF', size: '40 MB' },
      { name: '分譜（四聲部 × 10 首）', type: 'PDF', size: '54 MB' },
      { name: '排練建議', type: 'PDF', size: '2 MB' },
    ],
    previews: 5,
  },
  {
    id: 'p17', storeSlug: 'nocturne-lab', cat: 'music', hue: 190,
    title: '城市環境音景素材庫',
    creator: 'Kei Nakamura', handle: '@kei.nocturne', avatar: '#4c6ef5',
    price: 19, rating: 4.7, ratingCount: 61, sales: 540,
    tags: ['配樂'],
    blurb: '60 段城市環境音：雨聲、車流、便利商店與深夜月台。',
    desc: ['以雙聲道現場收音的 60 段城市音景，涵蓋雨夜街道、捷運月台、便利商店與清晨市場，適合影像後期與遊戲環境音。'],
    files: [F('WAV', 60)],
    totalSize: '2.2 GB', formats: ['WAV 48kHz'],
    contents: [
      { name: '環境音素材（60 段）', type: 'WAV', size: '2.2 GB' },
      { name: '使用授權說明', type: 'PDF', size: '1 MB' },
    ],
    previews: 6,
  },
  {
    id: 'p18', storeSlug: 'lenakeys', cat: 'music', hue: 288,
    title: '流行鋼琴彈唱譜精選 15 首',
    creator: 'Lena Okafor', handle: '@lenakeys', avatar: '#6151f0',
    price: 17, rating: 4.8, ratingCount: 241, sales: 1320,
    tags: ['流行', '鋼琴'],
    blurb: '15 首流行彈唱編配，附和弦簡譜與完整鋼琴譜雙版本。',
    desc: ['精選 15 首傳唱度最高的流行曲目，每首同時提供和弦簡譜（自彈自唱）與完整鋼琴獨奏譜兩種版本，難度標示清楚。'],
    files: [F('PDF', 30)],
    totalSize: '88 MB', formats: ['PDF'],
    contents: [
      { name: '彈唱簡譜（15 首）', type: 'PDF', size: '36 MB' },
      { name: '鋼琴獨奏譜（15 首）', type: 'PDF', size: '52 MB' },
    ],
    previews: 5,
  },
  {
    id: 'p19', storeSlug: 'wanderlens', cat: 'photo', hue: 88,
    title: '台北巷弄・日常光影 50 張',
    creator: 'Mia Chen', handle: '@wanderlens', avatar: '#0ca678',
    price: 18, rating: 4.7, ratingCount: 103, sales: 720,
    tags: ['街拍', '旅行'],
    blurb: '50 張台北老城區街拍，4K 無浮水印，含拍攝地點筆記。',
    desc: ['大稻埕、赤峰街與萬華巷弄裡的日常光影，50 張 4K 精選作品，附每張的拍攝地點與時段筆記，也適合作為取景參考。'],
    files: [F('JPG', 50)],
    totalSize: '1.3 GB', formats: ['JPG 4K'],
    contents: [
      { name: '街拍作品（50 張・4K）', type: 'JPG', size: '1.3 GB' },
      { name: '拍攝地點筆記', type: 'PDF', size: '6 MB' },
    ],
    previews: 6,
  },
  {
    id: 'p20', storeSlug: 'haru-frames', cat: 'photo', hue: 174,
    title: '瀨戶內海・島嶼慢旅攝影集',
    creator: 'Haru Tanaka', handle: '@haru.frames', avatar: '#e0823c',
    price: 23, rating: 4.9, ratingCount: 87, sales: 460,
    tags: ['風景', '旅行'],
    blurb: '42 張瀨戶內海跳島實拍，海色與藝術祭的安靜時刻。',
    desc: ['直島、豐島與小豆島的慢旅記錄，42 張 4K 作品捕捉瀨戶內海特有的柔和海色與藝術祭的安靜角落。'],
    files: [F('JPG', 42)],
    totalSize: '980 MB', formats: ['JPG 4K'],
    contents: [
      { name: '島嶼作品（42 張・4K）', type: 'JPG', size: '970 MB' },
      { name: '行程與授權說明', type: 'PDF', size: '5 MB' },
    ],
    previews: 6,
  },
  {
    id: 'p21', storeSlug: 'diego-night', cat: 'photo', hue: 218,
    title: '雨夜反光・電影感街拍預設',
    creator: 'Diego Ramos', handle: '@diego.night', avatar: '#3b7fd4',
    price: 15, rating: 4.6, ratingCount: 198, sales: 1650,
    tags: ['街拍', '黑白'],
    blurb: '12 組 Lightroom 預設重現雨夜霓虹的電影感色調。',
    desc: ['從我的夜間街拍工作流萃取出 12 組 Lightroom 預設：濕潤路面的反光、霓虹的溢色與陰影裡的藍，一鍵套用再微調。',
      '附 6 張 RAW 練習檔與調整前後對照。'],
    files: [F('XMP', 12), F('RAW', 6)],
    totalSize: '420 MB', formats: ['Lightroom 預設', 'RAW 練習檔'],
    contents: [
      { name: '預設組（12 款）', type: 'XMP', size: '4 MB' },
      { name: 'RAW 練習檔（6 張）', type: 'RAW', size: '410 MB' },
      { name: '套用教學', type: 'PDF', size: '6 MB' },
    ],
    previews: 5,
  },
  {
    id: 'p22', storeSlug: 'wanderlens', cat: 'photo', hue: 130,
    title: '山岳雲海・空拍 4K 素材集',
    creator: 'Mia Chen', handle: '@wanderlens', avatar: '#0ca678',
    price: 34, rating: 4.8, ratingCount: 52, sales: 290,
    tags: ['空拍', '風景'],
    blurb: '30 段合歡山與奇萊的空拍雲海，4K 60fps 可商用。',
    desc: ['歷時兩年在合歡山、奇萊與能高越嶺道拍下的 30 段空拍素材，4K 60fps、D-Log 原檔與調色版本雙軌提供，授權可商用。'],
    files: [F('MP4', 60)],
    totalSize: '18 GB', formats: ['MP4 4K60', 'D-Log 原檔'],
    contents: [
      { name: '調色版素材（30 段）', type: 'MP4', size: '9 GB' },
      { name: 'D-Log 原檔（30 段）', type: 'MP4', size: '9 GB' },
      { name: '商用授權書', type: 'PDF', size: '1 MB' },
    ],
    previews: 6,
  },
  {
    id: 'p23', storeSlug: 'astrid-space', cat: 'photo', hue: 8,
    title: '黑白人像・情緒肖像集',
    creator: 'Astrid Holm', handle: '@astrid.space', avatar: '#2f9e6b',
    price: 27, rating: 4.9, ratingCount: 66, sales: 410,
    tags: ['人像', '黑白'],
    blurb: '36 張高對比黑白人像，附打光示意圖與後製流程。',
    desc: ['一年間拍下的 36 張黑白肖像，聚焦光影與情緒的張力。每張附打光示意圖，並收錄我的完整黑白後製流程筆記。'],
    files: [F('JPG', 36), F('PDF', 1)],
    totalSize: '760 MB', formats: ['JPG 4K', '後製筆記'],
    contents: [
      { name: '肖像作品（36 張・4K）', type: 'JPG', size: '740 MB' },
      { name: '打光示意與後製筆記', type: 'PDF', size: '18 MB' },
    ],
    previews: 6,
  },
  {
    id: 'p24', storeSlug: 'haru-frames', cat: 'photo', hue: 340,
    title: '春日花見・櫻花季寫真（免費）',
    creator: 'Haru Tanaka', handle: '@haru.frames', avatar: '#e0823c',
    price: 0, rating: 4.5, ratingCount: 420, sales: 3200,
    tags: ['風景', '旅行'],
    blurb: '免費下載：20 張京都櫻花季寫真，桌布尺寸即用。',
    desc: ['把哲學之道與鴨川的春天分享給你：20 張櫻花季精選，已裁切為常見桌布尺寸，完全免費，喜歡再回來逛逛其他作品。'],
    files: [F('JPG', 20)],
    totalSize: '240 MB', formats: ['JPG 桌布尺寸'],
    contents: [
      { name: '櫻花寫真（20 張）', type: 'JPG', size: '238 MB' },
      { name: '使用說明', type: 'PDF', size: '2 MB' },
    ],
    previews: 5,
  },
  {
    id: 'p25', storeSlug: 'marcusbuilds', cat: 'ebook', hue: 152,
    title: '一人公司：接案報價與合約指南',
    creator: 'Marcus Wei', handle: '@marcusbuilds', avatar: '#16a07a',
    price: 20, rating: 4.8, ratingCount: 155, sales: 980,
    tags: ['商業', '行銷'],
    blurb: '報價策略、合約條款與收款流程，96 頁實戰手冊。',
    desc: ['接案最怕報錯價、簽錯約。這本 96 頁手冊拆解報價策略、必備合約條款與收款流程，附可直接改用的合約與報價單範本。'],
    files: [F('PDF', 1), F('DOCX', 4)],
    totalSize: '28 MB', formats: ['PDF', 'Word 範本'],
    contents: [
      { name: '指南本文（96 頁）', type: 'PDF', size: '22 MB' },
      { name: '合約與報價單範本（4 份）', type: 'DOCX', size: '6 MB' },
    ],
    previews: 4,
  },
  {
    id: 'p26', storeSlug: 'yuki-writes', cat: 'ebook', hue: 262,
    title: '30 天寫作習慣養成手冊',
    creator: 'Yuki Sato', handle: '@yuki.writes', avatar: '#8b5cf6',
    price: 8, rating: 4.6, ratingCount: 302, sales: 1740,
    tags: ['寫作'],
    blurb: '每天 15 分鐘的寫作任務，30 天把輸出變成習慣。',
    desc: ['30 個循序漸進的每日寫作任務，每天只要 15 分鐘。附進度追蹤表與 30 個備用題目，卡關時隨時撿一個來寫。'],
    files: [F('PDF', 1), F('EPUB', 1)],
    totalSize: '14 MB', formats: ['PDF', 'EPUB'],
    contents: [
      { name: '手冊本文（72 頁）', type: 'PDF', size: '10 MB' },
      { name: 'EPUB 行動版', type: 'EPUB', size: '3 MB' },
      { name: '進度追蹤表', type: 'PDF', size: '1 MB' },
    ],
    previews: 4,
  },
  {
    id: 'p27', storeSlug: 'inkspire', cat: 'ebook', hue: 36,
    title: '插畫接案起步指南',
    creator: 'Ren Liu', handle: '@ren.inkspire', avatar: '#e8590c',
    price: 15, rating: 4.7, ratingCount: 91, sales: 560,
    tags: ['設計', '商業'],
    blurb: '從作品集到第一張訂單：插畫接案的完整起步地圖。',
    desc: ['整理我從零開始接案三年的所有經驗：作品集怎麼排、去哪裡找案子、怎麼溝通修改與收款，88 頁全彩圖文。'],
    files: [F('PDF', 1)],
    totalSize: '52 MB', formats: ['PDF'],
    contents: [
      { name: '指南本文（88 頁全彩）', type: 'PDF', size: '50 MB' },
      { name: '報價信範本', type: 'PDF', size: '2 MB' },
    ],
    previews: 4,
  },
  {
    id: 'p28', storeSlug: 'priya-shoots', cat: 'ebook', hue: 58,
    title: '光線的語言：自然光攝影教學',
    creator: 'Priya Nair', handle: '@priya.shoots', avatar: '#d8a017',
    price: 18, rating: 4.9, ratingCount: 187, sales: 1150,
    tags: ['攝影教學'],
    blurb: '讀懂窗光、逆光與陰天：110 頁自然光實戰教學。',
    desc: ['不用燈具也能拍出好照片。110 頁系統化拆解窗光、逆光、陰天與黃金時刻的用光思路，每章附實拍對照與練習任務。'],
    files: [F('PDF', 1), F('EPUB', 1)],
    totalSize: '120 MB', formats: ['PDF', 'EPUB'],
    contents: [
      { name: '教學本文（110 頁全彩）', type: 'PDF', size: '112 MB' },
      { name: 'EPUB 行動版', type: 'EPUB', size: '8 MB' },
    ],
    previews: 4,
  },
  {
    id: 'p29', storeSlug: 'inkspire', cat: 'ebook', hue: 12,
    title: 'Procreate 筆刷與上色技法',
    creator: 'Ren Liu', handle: '@ren.inkspire', avatar: '#e8590c',
    price: 12, rating: 4.8, ratingCount: 143, sales: 890,
    tags: ['設計'],
    blurb: '35 支自製筆刷＋上色流程教學，附分層練習檔。',
    desc: ['我日常插畫用的 35 支自製筆刷全數打包，搭配 60 頁上色流程教學與 3 個分層練習檔，跟著畫一次就上手。'],
    files: [F('BRUSH', 35), F('PDF', 1), F('PSD', 3)],
    totalSize: '340 MB', formats: ['Procreate 筆刷', 'PDF', 'PSD'],
    contents: [
      { name: '筆刷組（35 支）', type: 'BRUSH', size: '120 MB' },
      { name: '上色教學（60 頁）', type: 'PDF', size: '48 MB' },
      { name: '分層練習檔（3 個）', type: 'PSD', size: '172 MB' },
    ],
    previews: 5,
  },
  {
    id: 'p30', storeSlug: 'marcusbuilds', cat: 'ebook', hue: 204,
    title: '電子報經營從 0 到 1000 訂閱',
    creator: 'Marcus Wei', handle: '@marcusbuilds', avatar: '#16a07a',
    price: 0, rating: 4.5, ratingCount: 380, sales: 4100,
    tags: ['行銷', '寫作'],
    blurb: '免費電子書：用 12 週把電子報從 0 做到 1000 訂閱。',
    desc: ['一份可照做的 12 週行動計畫：定位、選題、導流與變現，每週一個明確目標。完全免費，附選題靈感清單 100 條。'],
    files: [F('PDF', 1), F('EPUB', 1)],
    totalSize: '16 MB', formats: ['PDF', 'EPUB'],
    contents: [
      { name: '行動計畫（58 頁）', type: 'PDF', size: '12 MB' },
      { name: 'EPUB 行動版', type: 'EPUB', size: '3 MB' },
      { name: '選題靈感清單', type: 'PDF', size: '1 MB' },
    ],
    previews: 3,
  },
];
