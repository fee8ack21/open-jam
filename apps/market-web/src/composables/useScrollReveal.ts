/* ============================================================
   useScrollReveal — 區塊進場動畫。掃描 `.rv` 元素，進入視窗
   時加上 `.rv-in`（CSS 端僅於 prefers-reduced-motion:
   no-preference 才隱藏未進場元素，動效退場安全）。
   商品非同步載入後新出現的區塊，由呼叫端再次 scan()。
   ============================================================ */
import { onBeforeUnmount, onMounted } from 'vue';

export function useScrollReveal(): { scan: () => void } {
  let io: IntersectionObserver | null = null;
  const seen = new WeakSet<Element>();

  function scan(): void {
    const els = document.querySelectorAll('.rv:not(.rv-in)');
    if (!io) {
      els.forEach((el) => el.classList.add('rv-in'));
      return;
    }
    els.forEach((el) => {
      if (seen.has(el)) return;
      seen.add(el);
      io!.observe(el);
    });
  }

  onMounted(() => {
    if ('IntersectionObserver' in window) {
      io = new IntersectionObserver(
        (entries) => {
          for (const e of entries) {
            if (!e.isIntersecting) continue;
            e.target.classList.add('rv-in');
            io!.unobserve(e.target);
          }
        },
        { threshold: 0.1, rootMargin: '0px 0px -40px' },
      );
    }
    scan();
  });
  onBeforeUnmount(() => io?.disconnect());

  return { scan };
}
