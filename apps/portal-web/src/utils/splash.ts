/* ============================================================
   splash.ts — 全螢幕 loading 動畫（index.html #oj-splash）淡出
   時機：app 掛載、首個路由解析完成後呼叫。等網頁字型就緒
   （上限 2.5s）再淡出，避免內容以 fallback 字型閃現；splash
   自導覽起至少停留 1.1s 讓動畫讀得到。main.ts 另掛 6s 硬上限
   保底呼叫（路由 / 字型卡住也不擋死頁面），重複呼叫安全。
   ============================================================ */

const MIN_VISIBLE_MS = 1100;
const FONTS_TIMEOUT_MS = 2500;
const FADE_MS = 600;

let started = false;
let done = false;

function hide(el: HTMLElement): void {
  if (done) return;
  done = true;
  el.style.opacity = '0';
  el.style.pointerEvents = 'none';
  window.setTimeout(() => el.remove(), FADE_MS);
}

export function dismissSplash(options: { immediate?: boolean } = {}): void {
  const el = document.getElementById('oj-splash');
  if (!el || done) return;

  // 本 session 已播過 → index.html 讓 splash 延遲 450ms 才淡入：
  // 還沒亮相就直接移除（使用者從未看到）；已亮相則平滑淡出，
  // 不套用首次載入的最短停留
  if (document.documentElement.classList.contains('oj-splash-skip')) {
    done = true;
    if (getComputedStyle(el).opacity === '0') {
      el.remove();
      return;
    }
    el.style.animation = 'none';
    el.style.opacity = '0';
    el.style.pointerEvents = 'none';
    window.setTimeout(() => el.remove(), FADE_MS);
    return;
  }

  if (options.immediate) {
    hide(el);
    return;
  }
  if (started) return;
  started = true;

  // performance.now() 以導覽開始起算，最短停留涵蓋 bundle 下載時間
  const finish = () =>
    window.setTimeout(() => hide(el), Math.max(0, MIN_VISIBLE_MS - performance.now()));

  const fontsReady = document.fonts?.ready ?? Promise.resolve();
  Promise.race([
    fontsReady,
    new Promise((resolve) => window.setTimeout(resolve, FONTS_TIMEOUT_MS)),
  ]).then(finish);
}
