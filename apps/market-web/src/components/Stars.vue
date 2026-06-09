<script setup lang="ts">
/* Stars — star rating label. Registered globally as <stars>. */
withDefaults(defineProps<{
  value: number;
  count?: number;
  size?: number;
}>(), {
  size: 13,
});
</script>

<template>
  <span class="rating">
    <j-icon name="star" :size="size" fill style="color: #f0a92b" />
    {{ value.toFixed(1) }}
    <span v-if="count" style="opacity: 0.7">({{ count }})</span>
  </span>
</template>
