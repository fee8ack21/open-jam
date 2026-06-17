import { ref, computed } from 'vue';
import { defineStore } from 'pinia';
import type { User } from 'oidc-client-ts';
import { getUser, login, logout, validateSession } from '@/oidc/auth.js';

export const useAuthStore = defineStore('auth', () => {
  const userIdentity = ref<User | null>(null);

  const isAuthenticated = computed(() => !!userIdentity.value && !userIdentity.value.expired);
  const userEmail = computed<string | null>(() => userIdentity.value?.profile?.email ?? null);

  async function getUserIdentity(): Promise<void> {
    try {
      const user = await getUser();

      if (user && !user.expired) {
        userIdentity.value = user;
        return;
      }

      // 本地沒有 user 或 token 過期時，嘗試靠 auth.openjam.co 的 SSO session silent signin
      userIdentity.value = await validateSession();
    } catch (error) {
      console.error('getUserIdentity failed:', error);
      userIdentity.value = null;
    }

    console.log('userIdentity:', userIdentity);
  }

  return {
    userIdentity,
    isAuthenticated,
    userEmail,
    getUserIdentity,
    login,
    logout,
  };
});
