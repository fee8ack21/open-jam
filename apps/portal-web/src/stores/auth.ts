import { ref, computed, watch } from 'vue';
import { defineStore } from 'pinia';
import type { User } from 'oidc-client-ts';
import { login, logout, userManager, validateSession } from '@/oidc/auth.js';
import { getLogoutMark, clearLoggedOut } from '@/oidc/cross-domain-logout.js';

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
  const accessTokenExt = computed<Record<string, unknown> | null>(() => {
    const token = userIdentity.value?.access_token;
    if (!token) return null;
    const payload = decodeJwtPayload(token);
    return (payload?.ext as Record<string, unknown> | undefined) ?? payload ?? null;
  });
  const userEmail = computed<string | null>(
    () => (accessTokenExt.value?.email as string | undefined) ?? userIdentity.value?.profile?.email ?? null,
  );

  async function getUserIdentity(): Promise<void> {
    if (loadPromise) return loadPromise;

    loadPromise = (async () => {
      try {
        // 本地 token 不代表 Hydra session 仍存在；無本地 token 時也用 SSO session 嘗試自動登入。
        userIdentity.value = await validateSession();
      } catch (error) {
        console.error('getUserIdentity failed:', error);
        userIdentity.value = null;
      } finally {
        loadPromise = null;
      }
    })();

    return loadPromise;
  }

  function refreshWhenVisible(): void {
    if (document.visibilityState === 'visible') {
      checkCrossDomainLogout();
      void getUserIdentity();
    }
  }

  // 跨子網域登出：以 .openjam.co 共用 cookie 標記偵測「在其他子網域登出」。
  // sessionStartedAt 記錄本分頁建立登入的時間（僅首次建立時設定，silent renew 不更動），
  // 標記時間晚於它即代表登出發生在本分頁登入之後 → 清除本地 session。
  let sessionStartedAt: number | null = null;

  watch(userIdentity, (user) => {
    if (user && !user.expired) {
      if (sessionStartedAt === null) {
        sessionStartedAt = Date.now();
        // 清除早於本次登入的殘留標記（已過期或他人舊登出），避免混淆。
        const mark = getLogoutMark();
        if (mark !== null && mark < sessionStartedAt) clearLoggedOut();
      }
    } else {
      sessionStartedAt = null;
    }
  });

  function checkCrossDomainLogout(): void {
    const mark = getLogoutMark();
    if (
      mark !== null &&
      sessionStartedAt !== null &&
      mark > sessionStartedAt &&
      userIdentity.value &&
      !userIdentity.value.expired
    ) {
      void userManager.removeUser(); // 觸發 addUserUnloaded → userIdentity = null
    }
  }

  window.setInterval(checkCrossDomainLogout, 3000);

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
    getUserIdentity,
    login,
    logout,
  };
});
