import { ref, computed } from 'vue';
import { defineStore } from 'pinia';
import type { User } from 'oidc-client-ts';
import { login, logout, userManager, validateSession } from '@/oidc/auth';

/** 解碼 JWT payload（base64url），失敗回傳 null。 */
function decodeJwtPayload(token: string): Record<string, unknown> | null {
  try {
    const part = token.split('.')[1];
    const json = decodeURIComponent(
      atob(part.replace(/-/g, '+').replace(/_/g, '/'))
        .split('')
        .map((c) => '%' + c.charCodeAt(0).toString(16).padStart(2, '0'))
        .join(''),
    );
    return JSON.parse(json) as Record<string, unknown>;
  } catch {
    return null;
  }
}

export const useAuthStore = defineStore('auth', () => {
  const userIdentity = ref<User | null>(null);
  let loadPromise: Promise<void> | null = null;

  const isAuthenticated = computed(() => !!userIdentity.value && !userIdentity.value.expired);

  /**
   * access token 的 `ext` 物件，由 Hydra consent 注入 email / role（JWT 模式下位於 ext）。
   * id_token 不含任何 profile claims，故信箱與角色皆需由此取得。
   */
  const accessTokenExt = computed<Record<string, unknown> | null>(() => {
    const token = userIdentity.value?.access_token;
    if (!token) return null;
    const payload = decodeJwtPayload(token);
    return (payload?.ext as Record<string, unknown> | undefined) ?? payload ?? null;
  });

  const userEmail = computed<string | null>(
    () => (accessTokenExt.value?.email as string | undefined) ?? userIdentity.value?.profile?.email ?? null,
  );
  const userName = computed(() => userIdentity.value?.profile?.name ?? null);

  /**
   * 平台角色，取自 access token 的 `ext.role` claim（由 Hydra consent 注入）。
   * 值為 "User" / "Admin"；無 token 或解析失敗時為 null。
   */
  const userRole = computed<string | null>(
    () => (accessTokenExt.value?.role as string | undefined) ?? null,
  );

  /** 是否為一般使用者（role === "User"）；唯一能使用賣家/上架流程的角色。 */
  const isUser = computed(() => (userRole.value ?? '').toLowerCase() === 'user');

  async function getUserIdentity() {
    if (loadPromise) return loadPromise;

    loadPromise = (async () => {
      try {
        // 本地 token 不代表 Hydra session 仍存在；無本地 token 時也用 SSO session 嘗試自動登入。
        userIdentity.value = await validateSession();
      } catch {
        userIdentity.value = null;
      } finally {
        loadPromise = null;
      }
    })();

    return loadPromise;
  }

  function refreshWhenVisible(): void {
    if (document.visibilityState === 'visible') {
      void getUserIdentity();
    }
  }

  userManager.events.addUserLoaded((user) => {
    userIdentity.value = user;
  });
  userManager.events.addUserUnloaded(() => {
    userIdentity.value = null;
  });
  userManager.events.addUserSignedOut(() => {
    userIdentity.value = null;
  });

  window.addEventListener('storage', (event) => {
    if (event.key === 'logout-event') {
      userIdentity.value = null;
      return;
    }

    // 僅在「明確的登入事件」時重新驗證。不要監聽 oidc.user:* 的寫入，否則其他分頁
    // 每次 automaticSilentRenew 重寫該 key 都會觸發本分頁 silent renew，造成跨分頁
    // prompt=none 連鎖競態與已登入／未登入狀態抖動。
    if (event.key === 'login-event') {
      void getUserIdentity();
    }
  });

  if ('BroadcastChannel' in window) {
    const channel = new BroadcastChannel('auth');
    channel.onmessage = (event: MessageEvent<string>) => {
      if (event.data === 'login') void getUserIdentity();
      if (event.data === 'logout') userIdentity.value = null;
    };
  }

  // 分頁重新可見時做一次 SSO session 檢查（偵測在其他子網域登出）。
  // 不再額外監聽 window focus，避免每次視窗取得焦點都觸發 silent renew。
  document.addEventListener('visibilitychange', refreshWhenVisible);

  return {
    userIdentity,
    isAuthenticated,
    userEmail,
    userName,
    userRole,
    isUser,
    getUserIdentity,
    login,
    logout,
  };
});
