/* Icon markup for <app-icon name="...">
   手繪厚筆觸貼紙風（果醬罐 Neo-Brutalism 設計稿），單色 icon 用 currentColor，
   多色 icon（flame / star / funnel / mic / user / image）固定品牌色 + 深色描邊。 */
export interface IconDef {
  /** 預設 0 0 24 24 */
  vb?: string;
  /** <svg> 內部標記 */
  body: string;
}

const stroke = (d: string, w = 2.4) =>
  `<path d="${d}" fill="none" stroke="currentColor" stroke-width="${w}" stroke-linecap="round" stroke-linejoin="round"></path>`;

export const ICONS: Record<string, IconDef> = {
  /* 單音符（badge / eyebrow 通用） */
  note: {
    body:
      '<ellipse cx="7.5" cy="17.4" rx="3.4" ry="2.7" transform="rotate(-14 7.5 17.4)" fill="currentColor"></ellipse>' +
      '<path d="M10.8 17.4 V4.4 c3.6 0.9 5.6 2.8 5.6 6.2" fill="none" stroke="currentColor" stroke-width="2.4" stroke-linecap="round"></path>',
  },
  /* 雙音符 */
  beats: {
    body:
      '<ellipse cx="5.8" cy="18.2" rx="2.9" ry="2.3" fill="currentColor"></ellipse>' +
      '<ellipse cx="16.6" cy="16.7" rx="2.9" ry="2.3" fill="currentColor"></ellipse>' +
      '<path d="M8.6 18.2 V7 l10.8 -2.6 V16.7" fill="none" stroke="currentColor" stroke-width="2.4" stroke-linecap="round" stroke-linejoin="round"></path>',
  },
  /* 評分星（固定黃底深描邊） */
  star: {
    body: '<path d="M12 2.8 l2.7 5.7 6.2 0.8 -4.5 4.2 1.1 6.1 -5.5 -2.9 -5.5 2.9 1.1 -6.1 -4.5 -4.2 6.2 -0.8 z" fill="#ffde00" stroke="#1a1a1a" stroke-width="1.8" stroke-linejoin="round"></path>',
  },
  /* 火焰（熱門，固定橘底深描邊） */
  flame: {
    body: '<path d="M12.2 2.6 c0.5 2.9 3.2 4.4 4.3 7.2 a5.9 5.9 0 1 1 -11 1.6 c0.7 1 1.6 1.7 2.8 1.9 C7.2 9.8 10.5 6.7 12.2 2.6 z" fill="#ff6b35" stroke="#1a1a1a" stroke-width="1.8" stroke-linejoin="round"></path>',
  },
  /* 四角星光（編輯精選） */
  sparkle: {
    body: '<path d="M12 2.5 c0.9 4.6 2.9 6.6 7.5 7.5 c-4.6 0.9 -6.6 2.9 -7.5 7.5 c-0.9 -4.6 -2.9 -6.6 -7.5 -7.5 c4.6 -0.9 6.6 -2.9 7.5 -7.5 z" fill="currentColor"></path>',
  },
  /* 篩選漏斗（固定黃底深描邊） */
  funnel: {
    body: '<path d="M3.5 4.5 h17 L14 12.5 v6.5 l-4 -2.8 v-3.7 z" fill="#ffde00" stroke="#1a1a1a" stroke-width="2" stroke-linejoin="round"></path>',
  },
  /* 麥克風（熱銷排行，固定黃底深描邊） */
  mic: {
    body:
      '<path d="M7 3.5 h10 v5.5 a5 5 0 0 1 -10 0 z" fill="#ffde00" stroke="#1a1a1a" stroke-width="2" stroke-linejoin="round"></path>' +
      '<path d="M7 5 H3.8 a3.4 3.4 0 0 0 3.7 3.6 M17 5 h3.2 a3.4 3.4 0 0 1 -3.7 3.6" fill="none" stroke="#1a1a1a" stroke-width="2"></path>' +
      '<path d="M12 14 v3.5" stroke="#1a1a1a" stroke-width="2.2"></path>' +
      '<path d="M8 20.5 c0 -1.7 1.8 -3 4 -3 s4 1.3 4 3 z" fill="#ffde00" stroke="#1a1a1a" stroke-width="2" stroke-linejoin="round"></path>',
  },
  /* 人像（人氣創作者，固定綠底深描邊） */
  user: {
    body:
      '<circle cx="12" cy="7.5" r="4" fill="#b8ff9f" stroke="#1a1a1a" stroke-width="2"></circle>' +
      '<path d="M4.5 20.5 c0 -3.9 3.3 -6.4 7.5 -6.4 s7.5 2.5 7.5 6.4 z" fill="#b8ff9f" stroke="#1a1a1a" stroke-width="2" stroke-linejoin="round"></path>',
  },
  /* 相機（攝影分類，固定白底粉鏡頭） */
  image: {
    vb: '0 0 34 28',
    body:
      '<rect x="1" y="6" width="32" height="21" rx="4" fill="#ffffff" stroke="#1a1a1a" stroke-width="2.5"></rect>' +
      '<rect x="10" y="1" width="14" height="6" rx="3" fill="#ffffff" stroke="#1a1a1a" stroke-width="2.5"></rect>' +
      '<circle cx="17" cy="16" r="6" fill="#ff90e8" stroke="#1a1a1a" stroke-width="2.5"></circle>',
  },
  /* 翻開的書（電子書分類 / 文件連結） */
  book: {
    body: '<path d="M3.5 5 c2.8 -1.4 5.6 -1.4 8.5 0 v14.5 c-2.9 -1.4 -5.7 -1.4 -8.5 0 z M20.5 5 c-2.8 -1.4 -5.6 -1.4 -8.5 0 v14.5 c2.9 -1.4 5.7 -1.4 8.5 0 z" fill="none" stroke="currentColor" stroke-width="2" stroke-linejoin="round"></path>',
  },
  /* 條列文件（服務條款頁籤） */
  doc: {
    body:
      '<rect x="4.5" y="2.5" width="15" height="19" rx="3" fill="none" stroke="currentColor" stroke-width="2.2"></rect>' +
      '<path d="M8.5 8 h7 M8.5 12 h7 M8.5 16 h4.5" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round"></path>',
  },
  /* 盾牌打勾（隱私權政策頁籤） */
  shield: {
    body:
      '<path d="M12 2.8 l7.5 3 v6 c0 5 -3.4 8.3 -7.5 9.6 c-4.1 -1.3 -7.5 -4.6 -7.5 -9.6 v-6 z" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linejoin="round"></path>' +
      '<path d="M8.7 12 l2.3 2.3 4.3 -4.3" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round"></path>',
  },
  /* 對話泡泡（GitHub 討論區） */
  chat: {
    body:
      '<path d="M20.5 11.5 c0 4.4 -3.8 8 -8.5 8 c-1.1 0 -2.2 -0.2 -3.1 -0.6 L3.5 20.5 l1.7 -4 C4.1 15.1 3.5 13.4 3.5 11.5 c0 -4.4 3.8 -8 8.5 -8 s8.5 3.6 8.5 8 z" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linejoin="round"></path>' +
      '<circle cx="8.7" cy="11.5" r="1.3" fill="currentColor"></circle>' +
      '<circle cx="12" cy="11.5" r="1.3" fill="currentColor"></circle>' +
      '<circle cx="15.3" cy="11.5" r="1.3" fill="currentColor"></circle>',
  },
  /* 手繪實心愛心 */
  heart: {
    body: '<path d="M12 20.5 c-5.2 -3.6 -8.5 -6.8 -8.5 -10.3 c0 -2.6 2 -4.7 4.6 -4.7 c1.6 0 3 0.8 3.9 2.1 c0.9 -1.3 2.3 -2.1 3.9 -2.1 c2.6 0 4.6 2.1 4.6 4.7 c0 3.5 -3.3 6.7 -8.5 10.3 z" fill="currentColor"></path>',
  },
  play: { body: '<path d="M7 4.5 L19.5 12 L7 19.5 z" fill="currentColor"></path>' },
  search: {
    body:
      '<circle cx="10.5" cy="10.5" r="6.7" fill="none" stroke="currentColor" stroke-width="2.6"></circle>' +
      '<path d="M15.6 15.6 L21 21" stroke="currentColor" stroke-width="2.6" stroke-linecap="round"></path>',
  },
  arrow: { body: stroke('M4.5 12 h13.5 M12.5 6 l6 6 -6 6', 2.6) },
  arrowL: { body: stroke('M19.5 12 H6 M11.5 6 l-6 6 6 6', 2.6) },
  arrowU: { body: stroke('M12 19.5 V6 M6 11.5 l6 -6 6 6', 2.6) },
  arrowD: { body: stroke('M12 4.5 v13.5 M6 12.5 l6 6 6 -6', 2.6) },
  chevronD: { body: stroke('M5 9 l7 7 7 -7', 2.6) },
  close: { body: stroke('M6 6 l12 12 M18 6 L6 18', 2.8) },
  menu: { body: stroke('M4.5 6.5 h15 M4.5 12 h11.5 M4.5 17.5 h15', 2.6) },
  check: { body: stroke('M4.5 12.5 l5 5 L19.5 7', 2.8) },
  download: { body: stroke('M12 3.5 v10.5 M7 10 l5 4.5 5 -4.5 M4.5 20.5 h15') },
  home: { body: stroke('M4 11 L12 4 l8 7 M6 9.5 V20 h12 V9.5') },
  mail: {
    body:
      '<rect x="3" y="5" width="18" height="14" rx="3" fill="none" stroke="currentColor" stroke-width="2.2"></rect>' +
      '<path d="M4 7 l8 6 8 -6" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round"></path>',
  },
  bell: {
    body: stroke('M18 9 a6 6 0 1 0 -12 0 c0 7 -3 8 -3 8 h18 s-3 -1 -3 -8 M13.7 21 a2 2 0 0 1 -3.4 0', 2.2),
  },
  lock: { body: stroke('M6 11 V8 a6 6 0 0 1 12 0 v3 M5 11 h14 v9 H5 z', 2.2) },
  logout: { body: stroke('M9 21 H5 a2 2 0 0 1 -2 -2 V5 a2 2 0 0 1 2 -2 h4 M16 17 l5 -5 -5 -5 M21 12 H9', 2.2) },
  globe: {
    body: stroke(
      'M12 21 a9 9 0 1 0 0 -18 a9 9 0 0 0 0 18 z M3 12 h18 M12 3 c2.5 2.4 3.9 5.6 4 9 c-0.1 3.4 -1.5 6.6 -4 9 c-2.5 -2.4 -3.9 -5.6 -4 -9 c0.1 -3.4 1.5 -6.6 4 -9 z',
      2.2,
    ),
  },
  github: {
    body: stroke(
      'M9 19 c-5 1.5 -5 -2.5 -7 -3 m14 6 v-3.9 a3.4 3.4 0 0 0 -0.9 -2.6 c3 -0.3 6.2 -1.5 6.2 -6.8 a5.3 5.3 0 0 0 -1.5 -3.7 a4.9 4.9 0 0 0 -0.1 -3.7 s-1.2 -0.4 -3.9 1.5 a13.4 13.4 0 0 0 -7 0 C6.1 1.7 4.9 2.1 4.9 2.1 a4.9 4.9 0 0 0 -0.1 3.7 A5.3 5.3 0 0 0 3.3 9.5 c0 5.3 3.2 6.5 6.2 6.8 a3.4 3.4 0 0 0 -0.9 2.6 V23',
      2,
    ),
  },
};
