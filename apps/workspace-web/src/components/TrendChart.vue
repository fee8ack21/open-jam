<script setup lang="ts">
import { computed, ref } from 'vue'

/** 折線圖資料點。 */
interface TrendPoint {
  label: string
  value: number
}

const props = withDefaults(defineProps<{
  data?: TrendPoint[]
  height?: number
  currency?: boolean
}>(), {
  data: () => [],
  height: 220,
  currency: true,
})

const hover = ref(-1)
const W = 720

function fmt(v: number) {
  if (v >= 1000) return (props.currency ? '$' : '') + (v / 1000).toFixed(v % 1000 === 0 ? 0 : 1) + 'k'
  return (props.currency ? '$' : '') + v
}

const H = computed(() => props.height)
const pad = computed(() => ({ t: 18, r: 14, b: 28, l: 46 }))
const max = computed(() => Math.max(...props.data.map(d => d.value)) * 1.18 || 1)
const pts = computed(() => {
  const { t, r, b, l } = pad.value
  const iw = W - l - r, ih = H.value - t - b
  const n = props.data.length
  return props.data.map((d, i) => ({
    x: l + (n === 1 ? iw / 2 : (iw * i) / (n - 1)),
    y: t + ih - (d.value / max.value) * ih,
    d,
  }))
})
const linePath = computed(() => pts.value.map((p, i) => `${i ? 'L' : 'M'}${p.x.toFixed(1)} ${p.y.toFixed(1)}`).join(' '))
const areaPath = computed(() => {
  const { b } = pad.value; const base = H.value - b
  return `${linePath.value} L${pts.value[pts.value.length - 1].x.toFixed(1)} ${base} L${pts.value[0].x.toFixed(1)} ${base} Z`
})
const gridLines = computed(() => {
  const { t, b, l, r } = pad.value; const ih = H.value - t - b; const rows = 4
  return Array.from({ length: rows + 1 }, (_, i) => {
    const y = t + (ih * i) / rows
    const v = Math.round((max.value * (rows - i)) / rows)
    return { y, x1: l, x2: W - r, label: fmt(v) }
  })
})
</script>

<template>
  <div class="chart-wrap">
    <svg class="chart-svg" :viewBox="'0 0 ' + W + ' ' + H" preserveAspectRatio="xMidYMid meet"
         @mouseleave="hover = -1">
      <defs>
        <linearGradient id="areaFill" x1="0" y1="0" x2="0" y2="1">
          <stop offset="0%" stop-color="var(--c-violet)" stop-opacity="0.32" />
          <stop offset="100%" stop-color="var(--c-violet)" stop-opacity="0.02" />
        </linearGradient>
      </defs>
      <g class="chart-grid">
        <line v-for="(g,i) in gridLines" :key="i" :x1="g.x1" :y1="g.y" :x2="g.x2" :y2="g.y" />
      </g>
      <text v-for="(g,i) in gridLines" :key="'t'+i" class="chart-axis" :x="pad.l - 10" :y="g.y + 3" text-anchor="end">{{ g.label }}</text>
      <path class="chart-area" :d="areaPath" />
      <path class="chart-line" :d="linePath" />
      <g v-for="(p,i) in pts" :key="'p'+i">
        <text class="chart-axis" :x="p.x" :y="H - 9" text-anchor="middle">{{ p.d.label }}</text>
        <rect :x="p.x - (W/data.length)/2" y="0" :width="W/data.length" :height="H - pad.b" fill="transparent"
              @mouseenter="hover = i" style="cursor:crosshair" />
        <circle class="chart-dot" :class="{ hot: hover === i }" :cx="p.x" :cy="p.y" :r="hover === i ? 6 : 4" />
        <g v-if="hover === i">
          <line :x1="p.x" :y1="p.y" :x2="p.x" :y2="H - pad.b" stroke="var(--c-pink)" stroke-width="1.5" stroke-dasharray="3 4" />
          <g :transform="'translate(' + Math.min(Math.max(p.x, 54), W - 54) + ',' + Math.max(p.y - 16, 18) + ')'">
            <rect x="-46" y="-26" width="92" height="26" rx="8" fill="var(--text)" />
            <text x="0" y="-8" text-anchor="middle" fill="var(--surface)" font-family="var(--oj-mono)" font-size="12.5" font-weight="600">{{ fmt(p.d.value) }}</text>
          </g>
        </g>
      </g>
    </svg>
  </div>
</template>
