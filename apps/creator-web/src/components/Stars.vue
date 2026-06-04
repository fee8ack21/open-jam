<script>
import JIcon from './JIcon.vue';

export default {
  name: 'Stars',
  components: { JIcon },
  props: {
    value: Number,
    count: Number,
    size: { type: Number, default: 13 },
  },
};
</script>

<template>
  <span class="rating">
    <j-icon name="star" :size="size" fill style="color:#f0a92b" />
    {{ value.toFixed(1) }}
    <span v-if="count" style="opacity:.7">({{ count }})</span>
  </span>
</template>
