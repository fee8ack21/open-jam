import { ref, computed } from 'vue';
import { defineStore } from 'pinia';
import { getUser, login, logout } from '@/services/oidc/auth.js';

export const useAuthStore = defineStore('auth', () => {
  const userIdentity = ref(null);
  const loading = ref(false);

  const isAuthenticated = computed(() => !!userIdentity.value && !userIdentity.value.expired);
  const accessToken = computed(() => userIdentity.value?.access_token ?? null);
  const userEmail = computed(() => userIdentity.value?.profile?.email ?? null);
  const userName = computed(() =>
    userIdentity.value?.profile?.name ?? userIdentity.value?.profile?.email ?? null,
  );

  async function getUserIdentity() {
    loading.value = true;
    try {
      userIdentity.value = await getUser();
    } catch {
      userIdentity.value = null;
    } finally {
      loading.value = false;
    }
  }

  return {
    userIdentity,
    loading,
    isAuthenticated,
    accessToken,
    userEmail,
    userName,
    getUserIdentity,
    login,
    logout,
  };
});
