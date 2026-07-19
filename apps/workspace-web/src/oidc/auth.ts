import { UserManager, WebStorageStateStore, Log } from 'oidc-client-ts';
import type { UserManagerSettings } from 'oidc-client-ts';
import { env } from '@/environment';
import { markLoggedOut, getLogoutMark } from '@/oidc/cross-domain-logout';

Log.setLogger(console);
Log.setLevel(Log.WARN);

const authChannel = 'BroadcastChannel' in window ? new BroadcastChannel('auth') : null;

const config: UserManagerSettings = {
  authority: env.OIDC_AUTHORITY,
  client_id: env.OIDC_CLIENT_ID,
  redirect_uri: new URL(
    'callback.html',
    new URL(import.meta.env.BASE_URL, document.location.origin),
  ).href,
  silent_redirect_uri: new URL(
    'silent-renew.html',
    new URL(import.meta.env.BASE_URL, document.location.origin),
  ).href,
  post_logout_redirect_uri: new URL(import.meta.env.BASE_URL, document.location.origin).href,
  response_type: 'code',
  scope: 'openid profile email offline_access',
  userStore: new WebStorageStateStore({ store: window.localStorage }),
  automaticSilentRenew: true,
  // 不啟用 monitorSession：Ory Hydra 的 discovery 不提供 check_session_iframe，
  // OP iframe session management 規格不支援，開了也是 no-op。跨子網域登出改由
  // 分頁重新可見時的 prompt=none silent check 偵測（見 validateSession）。
};

export function createUserManager() {
  const manager = new UserManager(config);

  if (authChannel) {
    authChannel.onmessage = (msg) => {
      if (msg.data === 'logout') {
        manager.removeUser().then(() => window.location.replace('/'));
      }
    };
  } else {
    window.addEventListener('storage', (event) => {
      if (event.key === 'logout-event') {
        manager.removeUser().then(() => window.location.replace('/'));
      }
    });
  }

  return manager;
}

export const userManager = createUserManager();

/**
 * 登出後的抑制窗口：登出分頁 broadcast 後才導航 Hydra end_session，SSO session
 * 銷毀需要時間；其他分頁收到廣播重載後若立刻 silent 登入（或 signinRedirect 被
 * 還活著的 session skip），會把自己重新登回去並清掉 oj_logout 標記，造成多分頁
 * 登出不同步。窗口內一律不自動登入、登入導轉強制顯示登入頁（prompt=login）。
 */
const LOGOUT_GRACE_MS = 60_000;

function isRecentlyLoggedOut(): boolean {
  const mark = getLogoutMark();
  return mark !== null && Date.now() - mark < LOGOUT_GRACE_MS;
}

export function login(redirectPath?: string) {
  const target = redirectPath ?? window.location.href;
  userManager
    .signinRedirect({ state: target, ...(isRecentlyLoggedOut() ? { prompt: 'login' } : {}) })
    .catch(console.error);
}

export async function logout() {
  const user = await userManager.getUser();
  if (!user) return;

  // 跨子網域登出標記（.openjam.co 共用 cookie），讓其他子網域分頁偵測並同步登出。
  markLoggedOut();

  if (authChannel) {
    authChannel.postMessage('logout');
  } else {
    localStorage.setItem('logout-event', Date.now().toString());
  }

  try {
    await userManager.signoutRedirect().catch(console.error);
  } catch {
    await userManager.removeUser();
  }
}

export async function getUser() {
  return await userManager.getUser();
}

/** Hydra 在 prompt=none 下回報需要互動，即代表 SSO session 已失效。 */
function isSessionEndedError(error: unknown): boolean {
  const code = (error as { error?: string } | null | undefined)?.error;
  return code === 'login_required' || code === 'interaction_required' || code === 'consent_required';
}

/** 產生符合 PKCE 格式的隨機 base64url 字串（僅供探測，不會交換 code）。 */
function randomB64Url(bytes = 32): string {
  const arr = new Uint8Array(bytes);
  crypto.getRandomValues(arr);
  let s = '';
  for (const b of arr) s += String.fromCharCode(b);
  return btoa(s).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');
}

type SsoProbeResult = 'active' | 'logged-out' | 'unknown';

/**
 * 以隱藏 iframe 對 Hydra 授權端點做 prompt=none 探測，真正確認 SSO session 是否仍存在。
 *
 * 為何不用 signinSilent：本地有 refresh token 時 signinSilent 走 refresh token grant，
 * 而 refresh grant 不檢查 Hydra SSO session，會掩蓋「在其他子網域登出」的情況。直打
 * 授權端點才會檢查 session（hydra 與本站同為 *.openjam.co same-site，SSO cookie 會隨送）。
 *
 * 回傳 'logged-out' → Hydra 已無 session（含跨子網域登出）；'active' → 仍登入；
 * 'unknown' → 其他錯誤 / 逾時，保留現狀不動。
 */
function probeSsoSession(timeoutMs = 8000): Promise<SsoProbeResult> {
  return new Promise((resolve) => {
    const params = new URLSearchParams({
      client_id: config.client_id,
      response_type: 'code',
      scope: 'openid',
      redirect_uri: config.silent_redirect_uri as string,
      prompt: 'none',
      state: randomB64Url(16),
      nonce: randomB64Url(16),
      code_challenge: randomB64Url(32),
      code_challenge_method: 'S256',
    });
    const url = `${env.OIDC_AUTHORITY.replace(/\/$/, '')}/oauth2/auth?${params.toString()}`;

    const iframe = document.createElement('iframe');
    iframe.style.display = 'none';
    let settled = false;
    let pollId = 0;
    let timeoutId = 0;

    const finish = (result: SsoProbeResult) => {
      if (settled) return;
      settled = true;
      window.clearInterval(pollId);
      window.clearTimeout(timeoutId);
      try { iframe.remove(); } catch { /* ignore */ }
      resolve(result);
    };

    pollId = window.setInterval(() => {
      let href: string | null = null;
      try {
        href = iframe.contentWindow?.location.href ?? null; // 導回本站後才同源可讀
      } catch {
        return; // 仍停在 hydra（跨來源），繼續等
      }
      if (!href || !href.startsWith(window.location.origin) || !href.includes('silent-renew')) return;
      const query = new URLSearchParams(href.split('?')[1] ?? '');
      if (query.get('error') === 'login_required') finish('logged-out');
      else if (query.get('code')) finish('active');
      else finish('unknown');
    }, 200);

    timeoutId = window.setTimeout(() => finish('unknown'), timeoutMs);
    document.body.appendChild(iframe);
    iframe.src = url;
  });
}

/**
 * 確認登入狀態：偵測「在其他子網域登出」（本地 token 雖未過期，但 Hydra session 已被銷毀）。
 *
 * - 已有未過期的本地登入 → 用 prompt=none 授權端點探測 SSO session；僅在明確 `logged-out`
 *   時才清除（暫時性 / 未知錯誤保留現狀，避免 header 閃爍）。
 * - 無本地登入或已過期 → 以 signinSilent 嘗試用 SSO session 自動登入 / 續期。
 */
export async function validateSession() {
  const current = await userManager.getUser();

  if (current && !current.expired) {
    const probe = await probeSsoSession();
    if (probe === 'logged-out') {
      await userManager.removeUser();
      return null;
    }
    return current;
  }

  // 剛登出（本站分頁或其他子網域）：不做 silent 自動登入，避免搶在 Hydra
  // session 銷毀完成前把自己重新登回去。
  if (isRecentlyLoggedOut()) return null;

  try {
    return await userManager.signinSilent();
  } catch (error) {
    if (isSessionEndedError(error)) {
      await userManager.removeUser();
      return null;
    }
    return await userManager.getUser();
  }
}
