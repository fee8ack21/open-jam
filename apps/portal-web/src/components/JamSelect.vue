<script setup lang="ts" generic="T extends string">
/* ============================================================
   JamSelect — 果醬罐風格下拉選單
   視覺與互動比照 AppNav 的語系下拉（.lang-btn / .lang-menu）：
   膠囊觸發鈕 + 硬底影選單卡 + 粉色列與勾勾標記目前選項，
   點擊外部關閉，並沿用同一組 lang-pop 進出場動畫。
   ============================================================ */
import { computed, onBeforeUnmount, ref } from 'vue';

const props = withDefaults(
  defineProps<{
    modelValue: T;
    options: { label: string; value: T }[];
    ariaLabel?: string;
    /** 選單對齊邊；預設貼齊觸發鈕右緣（同語系下拉） */
    align?: 'left' | 'right';
    /** 觸發鈕撐滿容器寬度（側欄篩選用） */
    block?: boolean;
  }>(),
  { ariaLabel: undefined, align: 'right', block: false },
);
const emit = defineEmits<{ 'update:modelValue': [T] }>();

const open = ref(false);
const root = ref<HTMLElement | null>(null);
const currentLabel = computed(() => props.options.find((o) => o.value === props.modelValue)?.label ?? '');

function onDocPointer(e: PointerEvent) {
  if (root.value && !root.value.contains(e.target as Node)) close();
}
function openMenu() {
  if (open.value) return;
  open.value = true;
  document.addEventListener('pointerdown', onDocPointer, true);
}
function close() {
  if (!open.value) return;
  open.value = false;
  document.removeEventListener('pointerdown', onDocPointer, true);
}
function toggle() {
  open.value ? close() : openMenu();
}
function select(value: T) {
  emit('update:modelValue', value);
  close();
}

onBeforeUnmount(() => {
  document.removeEventListener('pointerdown', onDocPointer, true);
});
</script>

<template>
  <div ref="root" class="jsel" :class="{ 'is-block': block }">
    <button
      type="button"
      class="jsel-btn"
      :class="{ open }"
      :title="ariaLabel"
      :aria-label="ariaLabel"
      aria-haspopup="menu"
      :aria-expanded="open"
      @click="toggle"
    >
      <span class="jsel-val">{{ currentLabel }}</span>
      <app-icon name="chevronD" class="jsel-caret" :size="12" />
    </button>
    <transition name="lang-pop">
      <div v-if="open" class="jsel-menu" :class="'a-' + align" role="menu">
        <button
          v-for="opt in options"
          :key="opt.value"
          type="button"
          class="jsel-opt"
          :class="{ active: opt.value === modelValue }"
          role="menuitemradio"
          :aria-checked="opt.value === modelValue"
          @click="select(opt.value)"
        >
          <span class="jsel-opt-label">{{ opt.label }}</span>
          <app-icon v-if="opt.value === modelValue" name="check" class="jsel-opt-check" :size="15" />
        </button>
      </div>
    </transition>
  </div>
</template>
