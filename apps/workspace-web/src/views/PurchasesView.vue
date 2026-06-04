<script>
import { useDashboardStore } from '@/stores/dashboard'
import { JFmt } from '@/utils/format'

export default {
  name: 'PurchasesView',
  setup() { return { store: useDashboardStore(), F: JFmt } },
  computed: {
    list() { return this.store.purchases },
    spent() { return this.list.reduce((s, p) => s + p.price, 0) },
    fileCount() { return this.list.reduce((s, p) => s + p.product.contents.length, 0) },
  },
}
</script>

<template>
  <div data-screen-label="購買紀錄">
    <div class="page-head">
      <div>
        <p class="h-eyebrow">我的收藏庫</p>
        <h1 class="h-title">購買紀錄</h1>
        <p class="h-sub">{{ list.length }} 筆訂單 · 共 {{ fileCount }} 個可下載檔案 · 累計花費 {{ F.money(spent) }}</p>
      </div>
    </div>

    <div class="pur-list">
      <div v-for="pu in list" :key="pu.orderId" class="pur-row">
        <product-thumb :product="pu.product" :glyph-size="30" :show-cat="false" hide-label />
        <div class="pr-body">
          <div class="pr-title">{{ pu.product.title }}</div>
          <div class="pr-meta">
            <span style="display:inline-flex; align-items:center; gap:6px;">
              <span class="avatar" :style="{ background: pu.product.avatar, width:'18px', height:'18px', fontSize:'9px' }">{{ F.initials(pu.product.creator) }}</span>
              {{ pu.product.creator }}
            </span>
            <span style="color:var(--text-faint)">·</span>
            <span style="font-family:var(--oj-mono); font-size:12px;">{{ pu.product.formats.join(' · ') }} · {{ pu.product.totalSize }}</span>
            <span style="color:var(--text-faint)">·</span>
            <span style="font-family:var(--oj-mono); font-size:12px; color:var(--text-faint);">{{ F.date(pu.date) }} 購買</span>
          </div>
        </div>
        <div style="display:flex; flex-direction:column; align-items:flex-end; gap:10px; flex:none;">
          <span class="price" style="font-size:16px;">{{ pu.price === 0 ? '免費' : '$' + pu.price }}</span>
          <div style="display:flex; gap:8px;">
            <n-popover trigger="click" placement="bottom-end" :show-arrow="false" to=".oj-root">
              <template #trigger><n-button size="small" tertiary>{{ pu.product.contents.length }} 個檔案 <template #icon><j-icon name="chevronD" :size="14" /></template></n-button></template>
              <div style="display:grid; gap:2px; min-width:240px; padding:2px;">
                <button v-for="(f, i) in pu.product.contents" :key="i" class="menu-item" style="justify-content:space-between;">
                  <span style="display:flex; align-items:center; gap:9px; min-width:0;"><j-icon name="file" :size="16" /> <span style="overflow:hidden; text-overflow:ellipsis; white-space:nowrap;">{{ f.name }}</span></span>
                  <span style="font-family:var(--oj-mono); font-size:11px; color:var(--text-faint); flex:none;">{{ f.size }}</span>
                </button>
              </div>
            </n-popover>
            <n-button size="small" type="primary">
              <template #icon><j-icon name="download" :size="15" /></template>
              下載
            </n-button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
