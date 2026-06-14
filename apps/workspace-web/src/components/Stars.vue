<script setup lang="ts">
withDefaults(defineProps<{
  value?: number
  count?: number
  size?: number
}>(), {
  value: 0,
  count: 0,
  size: 13,
})
</script>

<template>
  <span class="rating">
    <app-icon name="star" :size="size" fill style="color:#f0a92b" />
    {{ value.toFixed(1) }}
    <span v-if="count" style="opacity:.7">({{ count }})</span>
  </span>
</template>
