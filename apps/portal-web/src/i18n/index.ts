/* ============================================================
   i18n — vue-i18n bootstrap for portal-web.

   Messages are NOT hand-edited here: each locale JSON under
   ./locales/ is generated from i18n.xlsx (app root) by
   `pnpm gen:i18n`. Translators edit the Excel workbook; run the
   script to regenerate the JSON the app consumes.
   ============================================================ */
import { createI18n } from 'vue-i18n';

import zhTW from './locales/zh-TW.json';
import en from './locales/en.json';

export const SUPPORTED_LOCALES = ['zh-TW', 'en'] as const;
export type Locale = (typeof SUPPORTED_LOCALES)[number];

export const DEFAULT_LOCALE: Locale = 'zh-TW';
const STORAGE_KEY = 'openjam.market.locale';

function isSupported(value: string | null | undefined): value is Locale {
  return !!value && (SUPPORTED_LOCALES as readonly string[]).includes(value);
}

/** Resolve the startup locale: saved choice → browser preference → default. */
function detectLocale(): Locale {
  try {
    const saved = localStorage.getItem(STORAGE_KEY);
    if (isSupported(saved)) return saved;
  } catch {
    /* localStorage unavailable (private mode) — fall through */
  }
  const nav = navigator.language;
  if (nav?.toLowerCase().startsWith('zh')) return 'zh-TW';
  if (isSupported(nav)) return nav;
  if (nav?.toLowerCase().startsWith('en')) return 'en';
  return DEFAULT_LOCALE;
}

const i18n = createI18n({
  legacy: false,
  locale: detectLocale(),
  fallbackLocale: DEFAULT_LOCALE,
  messages: { 'zh-TW': zhTW, en },
});

/** Switch the active locale, persist it, and reflect it on <html lang>. */
export function setLocale(locale: Locale): void {
  i18n.global.locale.value = locale;
  document.documentElement.setAttribute('lang', locale);
  try {
    localStorage.setItem(STORAGE_KEY, locale);
  } catch {
    /* ignore persistence failures */
  }
}

// reflect the detected locale on first load
document.documentElement.setAttribute('lang', i18n.global.locale.value);

export default i18n;
