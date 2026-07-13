<script setup lang="ts">
/* ============================================================
   AppIcon — 手繪貼紙風 icon（果醬罐設計稿語彙，同 portal-web）
   各 icon 筆觸 / 填色已內建於 icon-paths.ts 的 markup。
   ============================================================ */
import { computed } from 'vue';
import { ICONS } from './icon-paths';

const props = withDefaults(
  defineProps<{
    name: string;
    size?: number | string;
  }>(),
  { size: 20 },
);

const def = computed(() => ICONS[props.name]);
const vb = computed(() => def.value?.vb ?? '0 0 24 24');
/* 非正方形 viewBox（如相機）以寬為準等比縮放 */
const dims = computed(() => {
  const [, , w, h] = vb.value.split(' ').map(Number);
  const size = Number(props.size);
  return { width: size, height: Math.round(((size * h) / w) * 100) / 100 };
});
</script>

<template>
  <svg
    :width="dims.width"
    :height="dims.height"
    :viewBox="vb"
    style="display: block; flex: none"
    v-html="def?.body ?? ''"
  ></svg>
</template>
