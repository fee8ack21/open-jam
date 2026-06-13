<script lang="ts">
export default {
  name: 'Stars',
  props: {
    value: { type: Number, default: 0 },
    count: { type: Number, default: 0 },
    size: { type: Number, default: 13 },
  },
}
</script>

<template>
  <span class="rating">
    <j-icon name="star" :size="size" fill style="color:#f0a92b" />
    {{ value.toFixed(1) }}
    <span v-if="count" style="opacity:.7">({{ count }})</span>
  </span>
</template>
