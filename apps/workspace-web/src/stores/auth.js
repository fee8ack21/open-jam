import { ref, computed } from 'vue';
import { defineStore } from 'pinia';
import { getUser, login, logout } from '@/services/oidc/auth.js';

export const useAuthStore = defineStore('auth', () => {
  const userIdentity = ref(null);

  const isAuthenticated = computed(() => !!userIdentity.value && !userIdentity.value.expired);
  const userEmail = computed(() => userIdentity.value?.profile?.email ?? null);
  const userName = computed(() => userIdentity.value?.profile?.name ?? null);

  async function getUserIdentity() {
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
    userName,
    getUserIdentity,
    login,
    logout,
  };
});
