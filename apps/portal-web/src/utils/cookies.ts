/** 判斷指定名稱的 cookie 是否存在。 */
export function hasCookie(name: string): boolean {
  return document.cookie.split('; ').some((c) => c.startsWith(name + '='));
}

/** 寫入 cookie，`days` 為有效天數。 */
export function setCookie(name: string, value: string, days: number): void {
  const d = new Date();
  d.setTime(d.getTime() + days * 864e5);
  document.cookie = `${name}=${value}; expires=${d.toUTCString()}; path=/; SameSite=Lax`;
}
