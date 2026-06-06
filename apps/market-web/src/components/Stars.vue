<script setup>
/* Stars — star rating label. Registered globally as <stars>. */
defineProps({
  value: Number,
  count: Number,
  size: { type: Number, default: 13 },
});
</script>

<template>
  <span class="rating">
    <j-icon name="star" :size="size" fill style="color: #f0a92b" />
    {{ value.toFixed(1) }}
    <span v-if="count" style="opacity: 0.7">({{ count }})</span>
  </span>
</template>
