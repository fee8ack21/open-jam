<script setup lang="ts">
/* ============================================================
   AppNav — 店面頂部導覽（果醬罐設計稿 Header）
   果醬罐 logo + 搜尋膠囊（黃色搜尋鈕）+ 語言 / 購物車圓鈕 +
   追蹤創作者膠囊（黑色追蹤鈕）。
   ============================================================ */
import { computed, onBeforeUnmount, ref, watch } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { useMessage } from 'naive-ui';
import { useI18n } from 'vue-i18n';
import { useShopStore } from '@/stores/shop';
import { useAuthStore } from '@/stores/auth';
import { SUPPORTED_LOCALES, setLocale, type Locale } from '@/i18n';
import BrandLogo from '@/components/BrandLogo.vue';
import AppIcon from '@/components/app-icon';

const store = useShopStore();
const auth = useAuthStore();
const route = useRoute();
const router = useRouter();
const message = useMessage();
const { t, locale } = useI18n();

/* ---------- 語系下拉（觸發鈕維持圓形 icon-btn，選單同 portal-web popover） ---------- */
const langOptions = computed(() =>
  SUPPORTED_LOCALES.map((code) => ({ label: t(`language.${code}`), code })),
);

const langOpen = ref(false);
const langRoot = ref<HTMLElement | null>(null);

function onDocPointer(e: PointerEvent) {
  if (langRoot.value && !langRoot.value.contains(e.target as Node)) closeLang();
}
function openLang() {
  if (langOpen.value) return;
  langOpen.value = true;
  document.addEventListener('pointerdown', onDocPointer, true);
}
function closeLang() {
  if (!langOpen.value) return;
  langOpen.value = false;
  document.removeEventListener('pointerdown', onDocPointer, true);
}
function toggleLang() {
  langOpen.value ? closeLang() : openLang();
}
function onSelectLang(code: Locale) {
  setLocale(code);
  closeLang();
}
onBeforeUnmount(() => {
  document.removeEventListener('pointerdown', onDocPointer, true);
});

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
// 登出（或換帳號的過渡期間）清掉先前預填的信箱，避免殘留舊帳號。
watch(
  () => auth.userEmail,
  (email) => {
    if (emailEdited.value) return;
    followEmail.value = email ?? '';
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

const goCheckout = () => router.push({ name: 'checkout' });
const onSearch = (v: string) => {
  store.search = v;
  if (route.name !== 'list') router.push({ name: 'list' });
};
const submitSearch = () => {
  mobileSearchOpen.value = false;
  if (route.name !== 'list') router.push({ name: 'list' });
};
</script>

<template>
  <header class="nav">
    <div class="nav-inner">
      <brand-logo />

      <div v-if="!minimal" class="nav-search" :class="{ 'is-open': mobileSearchOpen }">
        <form class="search-box" @submit.prevent="submitSearch">
          <span class="search-ic"><app-icon name="search" :size="16" /></span>
          <input class="search-input" type="text" :value="store.search"
                 @input="onSearch(($event.target as HTMLInputElement).value)"
                 :placeholder="t('nav.searchPlaceholder')" :aria-label="t('common.search')" />
          <button v-if="store.search" class="search-clear" type="button"
                  @click="onSearch('')" :aria-label="t('common.clear')"><app-icon name="close" :size="14" /></button>
          <button class="search-btn" type="submit">{{ t('nav.search') }}</button>
        </form>
      </div>

      <div class="nav-spacer"></div>

      <div class="nav-actions">
        <div ref="langRoot" class="lang">
          <button
            type="button"
            class="icon-btn"
            :class="{ 'lang-open': langOpen }"
            :title="t('language.label')"
            :aria-label="t('language.label')"
            aria-haspopup="menu"
            :aria-expanded="langOpen"
            @click="toggleLang"
          >
            <app-icon name="globe" :size="17" />
          </button>
          <transition name="lang-pop">
            <div v-if="langOpen" class="lang-menu" role="menu">
              <button
                v-for="opt in langOptions"
                :key="opt.code"
                type="button"
                class="lang-opt"
                :class="{ active: opt.code === locale }"
                role="menuitemradio"
                :aria-checked="opt.code === locale"
                @click="onSelectLang(opt.code)"
              >
                <span class="lang-opt-label">{{ opt.label }}</span>
                <app-icon v-if="opt.code === locale" name="check" class="lang-opt-check" :size="15" />
              </button>
            </div>
          </transition>
        </div>

        <template v-if="!minimal">
          <button type="button" class="icon-btn nav-icon-toggle" :class="{ active: mobileSearchOpen }"
                  @click="toggleSearch" :title="t('nav.searchTitle')">
            <app-icon name="search" :size="17" />
          </button>

          <button type="button" class="icon-btn" @click="goCheckout" :title="t('nav.cartTitle')">
            <app-icon name="cart" :size="18" />
            <span v-if="cartCount" class="cart-badge">{{ cartCount }}</span>
          </button>

          <button v-if="!subscribed" type="button" class="icon-btn nav-icon-toggle" :class="{ active: mobileFollowOpen }"
                  @click="toggleFollow" :title="t('nav.followTitle')">
            <app-icon name="mail" :size="17" />
          </button>
        </template>
      </div>

      <div v-if="!minimal" class="nav-follow" :class="{ 'is-open': mobileFollowOpen }">
        <form v-if="!subscribed" class="follow-form" @submit.prevent="subscribe">
          <span class="follow-icon"><app-icon name="mail" :size="15" /></span>
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
