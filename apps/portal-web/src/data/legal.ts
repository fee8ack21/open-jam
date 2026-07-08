/* ============================================================
   Open Jam — 法律文件結構（meta）
   服務條款 / 隱私政策的「文案」改由 i18n 提供（key：`legal.<doc>.*`，
   見 src/i18n/locales/）。此處僅保留與語系無關的結構資訊：圖示與
   最後更新日期，供 portal-web 的 /terms、/privacy 頁面渲染。
   ============================================================ */

export type LegalKey = 'terms' | 'privacy';

export interface LegalMeta {
  key: LegalKey;
  /** AppIcon 名稱 */
  icon: string;
  /** 最後更新日期 */
  updated: string;
}

export const LEGAL_META: Record<LegalKey, LegalMeta> = {
  terms: { key: 'terms', icon: 'note', updated: '2026-05-31' },
  privacy: { key: 'privacy', icon: 'shield', updated: '2026-05-31' },
};
