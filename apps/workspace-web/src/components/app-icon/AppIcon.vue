<script setup lang="ts">
import { computed } from 'vue'
import { ICON_PATHS } from './icon-paths'

const props = withDefaults(defineProps<{
  name?: string
  size?: number | string
  fill?: boolean
  stroke?: number
}>(), {
  size: 20,
  stroke: 1.8,
})

const d = computed(() => ICON_PATHS[props.name ?? ''] || '')
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
