import { ref, computed } from 'vue';
import { defineStore } from 'pinia';
import type { User } from 'oidc-client-ts';
import { getUser, login, logout } from '@/services/oidc/auth.js';

export const useAuthStore = defineStore('auth', () => {
  const userIdentity = ref<User | null>(null);

  const isAuthenticated = computed(() => !!userIdentity.value && !userIdentity.value.expired);
  const userEmail = computed<string | null>(() => userIdentity.value?.profile?.email ?? null);

  async function getUserIdentity(): Promise<void> {
    try {
      userIdentity.value = await getUser();
    } catch {
      userIdentity.value = null;
    }
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
