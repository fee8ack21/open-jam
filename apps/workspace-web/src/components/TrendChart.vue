<script lang="ts">
import { defineComponent, type PropType } from 'vue'

/** 折線圖資料點。 */
interface TrendPoint {
  label: string
  value: number
}

export default defineComponent({
  name: 'TrendChart',
  props: {
    data: { type: Array as PropType<TrendPoint[]>, default: () => [] },
    height: { type: Number, default: 220 },
    currency: { type: Boolean, default: true },
  },
  data() { return { hover: -1, W: 720 } },
  computed: {
    H() { return this.height },
    pad() { return { t: 18, r: 14, b: 28, l: 46 } },
    max() { return Math.max(...this.data.map(d => d.value)) * 1.18 || 1 },
    pts() {
      const { t, r, b, l } = this.pad
      const iw = this.W - l - r, ih = this.H - t - b
      const n = this.data.length
      return this.data.map((d, i) => ({
        x: l + (n === 1 ? iw / 2 : (iw * i) / (n - 1)),
        y: t + ih - (d.value / this.max) * ih,
        d,
      }))
    },
    linePath() { return this.pts.map((p, i) => `${i ? 'L' : 'M'}${p.x.toFixed(1)} ${p.y.toFixed(1)}`).join(' ') },
    areaPath() {
      const { b } = this.pad; const base = this.H - b
      return `${this.linePath} L${this.pts[this.pts.length - 1].x.toFixed(1)} ${base} L${this.pts[0].x.toFixed(1)} ${base} Z`
    },
    gridLines() {
      const { t, b, l, r } = this.pad; const ih = this.H - t - b; const rows = 4
      return Array.from({ length: rows + 1 }, (_, i) => {
        const y = t + (ih * i) / rows
        const v = Math.round((this.max * (rows - i)) / rows)
        return { y, x1: l, x2: this.W - r, label: this.fmt(v) }
      })
    },
  },
  methods: {
    fmt(v: number) {
      if (v >= 1000) return (this.currency ? '$' : '') + (v / 1000).toFixed(v % 1000 === 0 ? 0 : 1) + 'k'
      return (this.currency ? '$' : '') + v
    },
  },
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
