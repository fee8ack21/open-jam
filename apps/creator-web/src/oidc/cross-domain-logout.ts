// ============================================================
// 跨子網域登出標記
//
// BroadcastChannel 與 localStorage 皆受同源限制，無法在 openjam.co 與
// <store>.openjam.co 之間同步登出。改用可被所有子網域讀取的「上層註冊網域」
// (.openjam.co) cookie 當登出標記：登出時寫入時間戳，各 app 以輪詢 + 分頁可見
// 時讀取，若標記時間晚於本分頁登入時間即代表「在其他子網域登出」，清除本地 session。
//
// 不依賴 iframe 第三方 cookie（probeSsoSession 受 Safari ITP / Chrome 限制），
// 故較該探測可靠。
// ============================================================

const COOKIE_NAME = 'oj_logout';
const MAX_AGE_SECONDS = 24 * 60 * 60; // 24h，足以涵蓋一次登出傳播並自動過期清除殘留標記

/**
 * 取得可供所有子網域共用的 cookie domain 片段（如 `; domain=.openjam.co`）。
 * localhost / 純 IP 不指定 domain（無共用上層網域，退化為單一 host）。
 */
function cookieDomain(): string {
  const host = window.location.hostname;
  if (host === 'localhost' || /^\d+\.\d+\.\d+\.\d+$/.test(host)) return '';
  const root = host.split('.').slice(-2).join('.'); // xiaoming.openjam.co → openjam.co
  return `; domain=.${root}`;
}

function secureFlag(): string {
  return window.location.protocol === 'https:' ? '; Secure' : '';
}

/** 標記「剛剛登出」，供其他子網域偵測。 */
export function markLoggedOut(): void {
  document.cookie = `${COOKIE_NAME}=${Date.now()}${cookieDomain()}; path=/; max-age=${MAX_AGE_SECONDS}; SameSite=Lax${secureFlag()}`;
}

/** 清除登出標記（明確登入後呼叫，避免殘留標記誤殺新 session）。 */
export function clearLoggedOut(): void {
  document.cookie = `${COOKIE_NAME}=${cookieDomain()}; path=/; max-age=0; SameSite=Lax${secureFlag()}`;
}

/** 讀取登出標記時間戳（ms）；無標記回傳 null。 */
export function getLogoutMark(): number | null {
  const match = document.cookie.match(new RegExp(`(?:^|; )${COOKIE_NAME}=(\\d+)`));
  if (!match) return null;
  const ts = Number(match[1]);
  return Number.isFinite(ts) ? ts : null;
}
