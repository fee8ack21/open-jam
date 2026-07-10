<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { useMessage } from 'naive-ui';
import { useI18n } from 'vue-i18n';
import { useShopStore } from '@/stores/shop';
import { useAuthStore } from '@/stores/auth';
import { SUPPORTED_LOCALES, setLocale, type Locale } from '@/i18n';
import AppIcon from '@/components/app-icon';

// 逐字拆解以套用交錯的彈跳動畫（同 portal-web BrandLogo）；空白換成 nbsp 保留字距（inline-block 下一般空白會塌陷）
const brandLetters = [...'Open Jam'].map((ch) => (ch === ' ' ? ' ' : ch));

const store = useShopStore();
const auth = useAuthStore();
const route = useRoute();
const router = useRouter();
const message = useMessage();
const { t, locale } = useI18n();

const langOptions = computed(() =>
  SUPPORTED_LOCALES.map((code) => ({ label: t(`language.${code}`), key: code })),
);
function onSelectLang(key: string) { setLocale(key as Locale); }

const followEmail = ref('');
const emailEdited = ref(false);      // 使用者是否手動改過信箱欄位
const submitting = ref(false);
const mobileSearchOpen = ref(false);
const mobileFollowOpen = ref(false);

const subscribed = computed(() => store.following);
const cartCount = computed(() => store.cartCount);
// 404 等頁面只顯示 Logo，隱藏搜尋／購物車／追蹤等互動欄位
const minimal = computed(() => route.name === 'not-found');

// 已登入則以登入信箱預填（仍可手動改動）；使用者一旦動過欄位就不再覆蓋。
watch(
  () => auth.userEmail,
  (email) => {
    if (email && !emailEdited.value) followEmail.value = email;
  },
  { immediate: true },
);
const onEmailInput = () => { emailEdited.value = true; };

const subscribe = async () => {
  if (submitting.value) return;
  const v = followEmail.value.trim();
  if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(v)) {
    message.warning(t('nav.msgInvalidEmail'));
    return;
  }
  submitting.value = true;
  try {
    await store.followStore(v);
    mobileFollowOpen.value = false;
    message.success(t('nav.msgFollowSuccess'));
  } catch {
    message.error(t('nav.msgFollowError'));
  } finally {
    submitting.value = false;
  }
};
const toggleSearch = () => { mobileSearchOpen.value = !mobileSearchOpen.value; mobileFollowOpen.value = false; };
const toggleFollow = () => { mobileFollowOpen.value = !mobileFollowOpen.value; mobileSearchOpen.value = false; };

const goList = () => { if (route.name !== 'list') router.push({ name: 'list' }); };
const goCheckout = () => router.push({ name: 'checkout' });
const onSearch = (v: string) => {
  store.search = v;
  if (route.name !== 'list') router.push({ name: 'list' });
};
</script>

<template>
  <header class="nav">
    <div class="nav-inner">
      <div class="brand" @click="goList" role="link" aria-label="Open Jam">
        <span class="brand-mark" aria-hidden="true">
          <svg width="19" height="19" viewBox="0 0 24 24" fill="none">
            <path d="M15 16.4V4.5c3.7 1 5 3.9 2 6.8" stroke="#fff" stroke-width="2.3" stroke-linecap="round" stroke-linejoin="round" fill="none"></path>
            <ellipse cx="10.4" cy="16.8" rx="4.7" ry="3.5" fill="#fff" transform="rotate(-22 10.4 16.8)"></ellipse>
          </svg>
        </span>
        <span class="brand-name" aria-hidden="true">
          <span
            v-for="(ch, i) in brandLetters"
            :key="i"
            class="brand-letter"
            :style="{ '--i': i }"
            >{{ ch }}</span
          >
        </span>
      </div>

      <div v-if="!minimal" class="nav-search" :class="{ 'is-open': mobileSearchOpen }">
        <div class="search-box">
          <span class="follow-icon"><app-icon name="search" :size="17" /></span>
          <input class="search-input" type="text" :value="store.search"
                 @input="onSearch(($event.target as HTMLInputElement).value)"
                 :placeholder="t('nav.searchPlaceholder')" :aria-label="t('common.search')" />
          <button v-if="store.search" class="search-clear" type="button"
                  @click="onSearch('')" :aria-label="t('common.clear')"><app-icon name="close" :size="15" /></button>
        </div>
      </div>

      <div class="nav-spacer"></div>

      <div class="nav-actions">
        <n-dropdown trigger="click" :options="langOptions" :value="locale" @select="onSelectLang">
          <div class="icon-btn" :title="t('language.label')">
            <app-icon name="globe" :size="20" />
          </div>
        </n-dropdown>

        <template v-if="!minimal">
          <div class="icon-btn nav-icon-toggle" :class="{ active: mobileSearchOpen }"
               @click="toggleSearch" :title="t('nav.searchTitle')">
            <app-icon name="search" :size="20" />
          </div>

          <div class="icon-btn" @click="goCheckout" :title="t('nav.cartTitle')">
            <app-icon name="cart" :size="20" />
            <span v-if="cartCount" class="cart-badge">{{ cartCount }}</span>
          </div>

          <div v-if="!subscribed" class="icon-btn nav-icon-toggle" :class="{ active: mobileFollowOpen }"
               @click="toggleFollow" :title="t('nav.followTitle')">
            <app-icon name="mail" :size="20" />
          </div>
        </template>
      </div>

      <div v-if="!minimal" class="nav-follow" :class="{ 'is-open': mobileFollowOpen }">
        <form v-if="!subscribed" class="follow-form" @submit.prevent="subscribe">
          <span class="follow-icon"><app-icon name="mail" :size="16" /></span>
          <input class="follow-input" type="email" v-model="followEmail" @input="onEmailInput"
                 :placeholder="t('nav.followPlaceholder')" :aria-label="t('nav.followEmailAria')" :disabled="submitting" />
          <button class="follow-btn" type="submit" :disabled="submitting">{{ submitting ? t('nav.following') : t('nav.follow') }}</button>
        </form>
        <div v-else class="follow-done">
          <app-icon name="check" :size="16" /> {{ t('nav.followed') }}
        </div>
      </div>
    </div>
  </header>
</template>
