<script>
import { ICON_PATHS } from './icon-paths'

export default {
  name: 'JIcon',
  props: {
    name: String,
    size: { type: [Number, String], default: 20 },
    fill: Boolean,
    stroke: { type: Number, default: 1.8 },
  },
  computed: {
    d() { return ICON_PATHS[this.name] || '' },
  },
}
</script>

<template>
  <svg :width="size" :height="size" viewBox="0 0 24 24"
       :fill="fill ? 'currentColor' : 'none'"
       :stroke="fill ? 'none' : 'currentColor'"
       :stroke-width="fill ? 0 : stroke" stroke-linecap="round" stroke-linejoin="round"
       style="display:block; flex:none;">
    <path :d="d"></path>
  </svg>
</template>
