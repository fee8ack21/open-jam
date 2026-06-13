import { ref, computed } from 'vue';
import { defineStore } from 'pinia';
import type { User } from 'oidc-client-ts';
import { getUser, login, logout, validateSession } from '@/services/oidc/auth';

export const useAuthStore = defineStore('auth', () => {
  const userIdentity = ref<User | null>(null);

  const isAuthenticated = computed(() => !!userIdentity.value && !userIdentity.value.expired);
  const userEmail = computed(() => userIdentity.value?.profile?.email ?? null);
  const userName = computed(() => userIdentity.value?.profile?.name ?? null);

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
    getUserIdentity,
    login,
    logout,
  };
});
