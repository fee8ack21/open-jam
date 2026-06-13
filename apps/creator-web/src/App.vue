<script lang="ts">
import { computed, onMounted, ref } from 'vue';
import { useShopStore } from './stores/shop';
import AppNav from './components/AppNav.vue';
import JIcon from './components/JIcon.vue';

export default {
  name: 'App',
  components: { AppNav, JIcon },
  setup() {
    const store = useShopStore();
    const tweaksOpen = ref(false);

    const naiveTheme = computed(() => null);
    const overrides = computed(() => ({
      common: {
        primaryColor: '#6c4cf1',
        primaryColorHover: '#8a72ff',
        primaryColorPressed: '#5638d8',
        primaryColorSuppl: '#8a72ff',
        borderRadius: '13px',
        borderRadiusSmall: '9px',
        fontFamily: "'Space Grotesk', 'Noto Sans TC', sans-serif",
        fontWeightStrong: '700',
      },
      Button: { fontWeight: '700' },
      Input: {
        borderRadius: '12px',
        border: '1.5px solid var(--border-strong)',
        borderHover: '1.5px solid var(--c-violet)',
        borderFocus: '1.5px solid var(--c-violet)',
        borderError: '1.5px solid var(--c-pink)',
        borderFocusError: '1.5px solid var(--c-pink)',
        borderHoverError: '1.5px solid var(--c-pink)',
        boxShadowFocus: 'none',
        boxShadowFocusError: 'none',
        color: 'var(--surface)',
        colorFocus: 'var(--surface)',
        caretColor: '#6c4cf1',
        heightMedium: '42px',
        heightLarge: '46px',
      },
      InternalSelection: {
        borderRadius: '12px',
        border: '1.5px solid var(--border-strong)',
        borderHover: '1.5px solid var(--c-violet)',
        borderActive: '1.5px solid var(--c-violet)',
        borderFocus: '1.5px solid var(--c-violet)',
        boxShadowActive: 'none',
        boxShadowFocus: 'none',
        color: 'var(--surface)',
        colorActive: 'var(--surface)',
        heightMedium: '42px',
      },
      Slider: {
        fillColor: '#6c4cf1',
        fillColorHover: '#8a72ff',
        handleColor: '#ffffff',
        dotBorderActive: '1.5px solid #6c4cf1',
      },
      Switch: { railColorActive: '#6c4cf1' },
    }));

    onMounted(() => {
      window.addEventListener('message', (e) => {
        const t = e && e.data && e.data.type;
        if (t === '__activate_edit_mode') tweaksOpen.value = true;
        else if (t === '__deactivate_edit_mode') tweaksOpen.value = false;
      });
      window.parent.postMessage({ type: '__edit_mode_available' }, '*');
    });

    const dismissTweaks = () => {
      tweaksOpen.value = false;
      window.parent.postMessage({ type: '__edit_mode_dismissed' }, '*');
    };

    const fontClass = computed(() => 'font-' + store.font);

    return { store, naiveTheme, overrides, tweaksOpen, dismissTweaks, fontClass };
  },
};
</script>

<template>
  <n-config-provider :theme="naiveTheme" :theme-overrides="overrides">
  <n-message-provider>
    <div class="oj-root" :class="['light', fontClass]">

      <app-nav />

      <main>
        <router-view />
      </main>

      <div v-show="tweaksOpen" class="tweaks-panel">
        <div class="tweaks-head">
          <span>Tweaks</span>
          <button class="tweaks-x" @click="dismissTweaks"><j-icon name="close" :size="16" /></button>
        </div>
        <div class="tweaks-body">
          <div class="tweaks-section">展示字體</div>
          <div class="tweaks-row">
            <span>標題字型</span>
            <div class="seg">
              <button :class="{ on: store.font === 'sora' }" @click="store.setFont('sora')">Bricolage</button>
              <button :class="{ on: store.font === 'grotesk' }" @click="store.setFont('grotesk')">Unbounded</button>
            </div>
          </div>
        </div>
      </div>

    </div>
  </n-message-provider>
  </n-config-provider>
</template>
