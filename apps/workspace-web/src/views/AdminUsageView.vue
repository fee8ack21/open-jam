<script setup lang="ts">
import { computed, onMounted } from 'vue'
import { storeToRefs } from 'pinia'
import { useResourceUsageStore } from '@/stores/resourceUsage'

const store = useResourceUsageStore()
const { byCreator } = storeToRefs(store)

/** 位元組轉人類可讀字串（GB / MB…）。 */
function fmtBytes(bytes: number) {
  if (!bytes) return '0 B'
  const units = ['B', 'KB', 'MB', 'GB', 'TB']
  const i = Math.min(Math.floor(Math.log(bytes) / Math.log(1024)), units.length - 1)
  const val = bytes / 1024 ** i
  return `${val.toFixed(i >= 3 ? 1 : 0)} ${units[i]}`
}

const kpis = computed(() => [
  {
    key: 'storage',
    label: '已用儲存空間',
    val: fmtBytes(store.usedBytes),
    ic: 'layers',
    bg: 'var(--c-violet)',
    sub: `上限 ${fmtBytes(store.quotaBytes)} · 已用 ${store.usedPercent}%`,
  },
  {
    key: 'files',
    label: '檔案總數',
    val: store.fileCount.toLocaleString('en-US'),
    ic: 'file',
    bg: 'var(--c-pink)',
    sub: `待清理孤兒檔 ${store.orphanFileCount} 個`,
  },
  {
    key: 'orphan',
    label: '孤兒檔占用',
    val: fmtBytes(store.orphanBytes),
    ic: 'download',
    bg: 'var(--c-orange)',
    sub: `待清理 ${store.orphanFileCount} 個`,
  },
  {
    key: 'private',
    label: '私有檔案用量',
    val: fmtBytes(store.privateBytes),
    ic: 'lock',
    bg: 'var(--c-cyan)',
    sub: `公開資產 ${fmtBytes(store.publicBytes)}`,
  },
])

/** 各創作者用量長條圖的最大值（決定長條比例）。 */
const creatorMax = computed(() => Math.max(...byCreator.value.map((s) => s.bytes)) || 1)
/** 公開 / 私有占比（百分比，總和 100）。 */
const split = computed(() => {
  const total = store.publicBytes + store.privateBytes || 1
  return {
    publicPct: Math.round((store.publicBytes / total) * 100),
    privatePct: Math.round((store.privateBytes / total) * 100),
  }
})

onMounted(store.load)
</script>

<template>
  <div data-screen-label="資源用量">
    <div class="page-intro">
      <p class="h-eyebrow">平台管理 · 資源</p>
      <h1 class="h-title">資源用量</h1>
      <p class="h-sub">
        平台儲存空間已使用 {{ store.usedPercent }}%（{{ fmtBytes(store.usedBytes) }} /
        {{ fmtBytes(store.quotaBytes) }})。
      </p>
    </div>

    <!-- KPI -->
    <div class="kpi-grid">
      <div v-for="k in kpis" :key="k.key" class="kpi">
        <div class="kpi-top">
          <div class="kpi-ic" :style="{ background: k.bg }"><app-icon :name="k.ic" :size="20" /></div>
          <span class="kpi-delta up" style="background:var(--oj-primary-wash); color:var(--oj-primary)">
            {{ k.sub }}
          </span>
        </div>
        <div class="kpi-val">{{ k.val }}</div>
        <div class="kpi-label">{{ k.label }}</div>
      </div>
    </div>

    <!-- 容量使用率 -->
    <div class="card-pad" style="margin-bottom:22px;">
      <div class="card-head">
        <div>
          <h3>儲存容量使用率</h3>
          <div class="ch-sub">已用 {{ fmtBytes(store.usedBytes) }} / 上限 {{ fmtBytes(store.quotaBytes) }}</div>
        </div>
        <span class="usage-pct" :class="{ warn: store.usedPercent >= 80 }">{{ store.usedPercent }}%</span>
      </div>
      <div class="usage-track">
        <span
          class="usage-fill"
          :class="{ warn: store.usedPercent >= 80 }"
          :style="{ width: Math.min(store.usedPercent, 100) + '%' }"></span>
      </div>
      <div class="usage-legend">
        <span class="lg"><i style="background:var(--c-cyan)"></i>公開展示資產 {{ fmtBytes(store.publicBytes) }}（{{ split.publicPct }}%)</span>
        <span class="lg"><i style="background:var(--c-violet)"></i>私有可下載檔 {{ fmtBytes(store.privateBytes) }}（{{ split.privatePct }}%)</span>
      </div>
    </div>

    <!-- 下載流量趨勢（尚未提供）+ 各創作者用量 -->
    <div class="dash-grid cols-2">
      <div class="card-pad">
        <div class="card-head">
          <div>
            <h3>下載流量與歷史趨勢</h3>
            <div class="ch-sub">需另建指標管線</div>
          </div>
        </div>
        <div class="usage-na">
          <app-icon name="chart" :size="30" />
          <p>StorageService 目前未追蹤下載流量與歷史用量快照，<br>此區待指標管線建立後提供。</p>
        </div>
      </div>

      <div class="card-pad">
        <div class="card-head"><h3>各創作者用量 Top {{ byCreator.length }}</h3></div>
        <div v-if="byCreator.length" class="bars">
          <div v-for="s in byCreator" :key="s.creatorId" class="bar-row">
            <span class="bl">{{ s.label }}</span>
            <span class="bar-track"><span class="bar-fill" :style="{ width: Math.max((s.bytes / creatorMax) * 100, 2) + '%', background: 'var(--c-violet)' }"></span></span>
            <span class="bv">{{ fmtBytes(s.bytes) }}</span>
          </div>
        </div>
        <div v-else class="usage-na" style="min-height:120px;"><p>尚無創作者用量資料。</p></div>
        <div v-if="byCreator.length" class="store-files-note">
          檔案數合計 {{ byCreator.reduce((n, s) => n + s.fileCount, 0).toLocaleString('en-US') }} 個
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.usage-track {
  height: 16px;
  border-radius: 999px;
  background: var(--surface-2);
  border: 1.5px solid var(--border);
  overflow: hidden;
  margin: 6px 0 14px;
}

.usage-fill {
  display: block;
  height: 100%;
  border-radius: 999px;
  background: var(--c-violet);
  transition: width 0.4s ease;
}

.usage-fill.warn {
  background: var(--c-orange);
}

.usage-pct {
  font-family: var(--oj-display);
  font-weight: 800;
  font-size: 22px;
  letter-spacing: -0.6px;
}

.usage-pct.warn {
  color: var(--c-orange);
}

.usage-legend {
  display: flex;
  flex-wrap: wrap;
  gap: 18px;
}

.usage-legend .lg {
  display: inline-flex;
  align-items: center;
  gap: 7px;
  font-size: 13px;
  font-weight: 600;
  color: var(--text-soft);
}

.usage-legend .lg i {
  width: 11px;
  height: 11px;
  border-radius: 4px;
  flex: none;
}

.store-files-note {
  margin-top: 18px;
  padding-top: 16px;
  border-top: 1.5px solid var(--border);
  font-size: 13px;
  font-weight: 600;
  color: var(--text-soft);
}

.usage-na {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 10px;
  min-height: 232px;
  text-align: center;
  color: var(--text-faint);
  font-size: 13px;
  line-height: 1.6;
}
</style>
