import { ref, computed } from 'vue';
import { defineStore } from 'pinia';
import type { User } from 'oidc-client-ts';
import { getUser, login, logout, validateSession } from '@/oidc/auth';

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
    try {
      const user = await getUser();
      // 本地有未過期的 token 不代表 Hydra session 仍存在（可能已在其他子網域登出），
      // 需以 prompt=none silent check 向 Hydra 確認。
      userIdentity.value = user && !user.expired ? await validateSession() : user;
    } catch {
      userIdentity.value = null;
    }
  }

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
