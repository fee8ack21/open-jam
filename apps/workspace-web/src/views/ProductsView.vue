<script setup lang="ts">
import { computed } from 'vue'
import { useDashboardStore } from '@/stores/dashboard'
import { JFmt as F, STATUS_LABEL } from '@/utils/format'
import type { Product } from '@/data'

const store = useDashboardStore()
const g = store
const s = store

const filters = computed(() => [
  { key: 'all', label: '全部', n: s.products.length },
  { key: 'live', label: '上架中', n: g.statusCount('live') },
  { key: 'review', label: '審核中', n: g.statusCount('review') },
  { key: 'draft', label: '草稿', n: g.statusCount('draft') },
  { key: 'off', label: '已下架', n: g.statusCount('off') },
])
const rows = computed(() => g.filteredProducts)

function statusLabel(s: string) { return STATUS_LABEL[s] || s }
function toggle(p: Product) { store.togglePublish(p.id) }
function canToggle(p: Product) { return p.status === 'live' || p.status === 'off' }
</script>

<template>
  <div data-screen-label="商品管理">
    <div class="page-head">
      <div>
        <p class="h-eyebrow">賣家工作室</p>
        <h1 class="h-title">商品管理</h1>
        <p class="h-sub">{{ s.products.length }} 件作品 · {{ g.statusCount('live') }} 件上架中</p>
      </div>
      <button class="cta-pop" @click="store.go('upload')"><app-icon name="plus" :size="16" :stroke="2.4" style="vertical-align:-3px; margin-right:4px;" />上架新商品</button>
    </div>

    <div style="display:flex; align-items:center; justify-content:space-between; gap:14px; margin-bottom:18px; flex-wrap:wrap;">
      <div class="tabs">
        <button v-for="f in filters" :key="f.key" :class="{ on: s.productFilter === f.key }" @click="store.setProductFilter(f.key)">
          {{ f.label }} <span class="tcount">{{ f.n }}</span>
        </button>
      </div>
    </div>

    <div class="card-pad" style="padding:8px 8px 4px;">
      <table class="tbl">
        <thead>
          <tr>
            <th>作品</th>
            <th class="hide-sm">狀態</th>
            <th class="num hide-sm">售出</th>
            <th class="num hide-sm">瀏覽</th>
            <th class="num">收入</th>
            <th style="width:160px; text-align:right;">上架</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="p in rows" :key="p.id">
            <td>
              <div class="prod-cell">
                <product-thumb :product="p" :glyph-size="22" :show-cat="false" hide-label />
                <div style="min-width:0;">
                  <div class="pc-title">{{ p.title }}</div>
                  <div class="pc-meta">{{ p.price === 0 ? '免費' : '$' + p.price }} · {{ p.formats[0] }} · 更新 {{ F.date(p.updated) }}</div>
                </div>
              </div>
            </td>
            <td class="hide-sm"><span class="pill" :class="p.status"><span class="pdot"></span>{{ statusLabel(p.status) }}</span></td>
            <td class="num hide-sm">{{ p.sales.toLocaleString('en-US') }}</td>
            <td class="num hide-sm">{{ F.compact(p.views) }}</td>
            <td class="num" style="font-weight:700;">{{ F.money(p.revenue) }}</td>
            <td>
              <div class="row-actions">
                <n-switch v-if="canToggle(p)" :value="p.status === 'live'" @update:value="toggle(p)" size="medium" />
                <span v-else style="font-size:12px; color:var(--text-faint); font-family:var(--oj-mono); margin-right:6px;">
                  {{ p.status === 'review' ? '審核中' : '草稿' }}
                </span>
                <button class="ic-act" title="編輯" @click="store.go('upload')"><app-icon name="edit" :size="17" /></button>
                <n-popover trigger="click" placement="bottom-end" :show-arrow="false" to=".oj-root">
                  <template #trigger><button class="ic-act" title="更多"><app-icon name="more" :size="18" /></button></template>
                  <div style="display:grid; gap:2px; min-width:150px; padding:2px;">
                    <button class="menu-item"><app-icon name="eye" :size="16" /> 在商城預覽</button>
                    <button class="menu-item"><app-icon name="copy" :size="16" /> 複製連結</button>
                    <button class="menu-item"><app-icon name="layers" :size="16" /> 建立副本</button>
                    <button class="menu-item danger"><app-icon name="trash" :size="16" /> 刪除作品</button>
                  </div>
                </n-popover>
              </div>
            </td>
          </tr>
        </tbody>
      </table>
      <div v-if="!rows.length" class="empty-box">
        <div class="eb-ic"><app-icon name="box" :size="30" /></div>
        <div class="eb-t">這個分類還沒有作品</div>
      </div>
    </div>
  </div>
</template>
