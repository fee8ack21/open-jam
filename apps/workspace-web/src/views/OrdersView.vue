<script setup lang="ts">
import { computed, ref } from 'vue'
import { useDashboardStore } from '@/stores/dashboard'
import { JFmt as F, STATUS_LABEL } from '@/utils/format'
import type { Product } from '@/data'

const store = useDashboardStore()
const g = store
const s = store

const tab = ref('all')

const rows = computed(() => {
  let list = s.orders
  if (tab.value !== 'all') list = list.filter(o => o.status === tab.value)
  return list
})
const paidTotal = computed(() => g.paidOrders.reduce((s, o) => s + o.amount, 0))

function statusLabel(s: string) { return STATUS_LABEL[s] || s }
function prod(id: string): Product { return store.product(id) || ({} as Product) }
function dt(iso: string) { const d = new Date(iso); return d.toISOString().slice(5, 10).replace('-', '/') + ' ' + d.toTimeString().slice(0, 5) }
</script>

<template>
  <div data-screen-label="訂單管理">
    <div class="page-head">
      <div>
        <p class="h-eyebrow">賣家工作室</p>
        <h1 class="h-title">訂單管理</h1>
        <p class="h-sub">{{ g.paidOrders.length }} 筆已付款 · 收入合計 {{ F.money(paidTotal) }}</p>
      </div>
    </div>

    <div style="margin-bottom:18px;">
      <div class="tabs">
        <button :class="{ on: tab === 'all' }" @click="tab = 'all'">全部 <span class="tcount">{{ s.orders.length }}</span></button>
        <button :class="{ on: tab === 'paid' }" @click="tab = 'paid'">已付款 <span class="tcount">{{ g.paidOrders.length }}</span></button>
        <button :class="{ on: tab === 'refunded' }" @click="tab = 'refunded'">已退款 <span class="tcount">{{ s.orders.filter(o => o.status==='refunded').length }}</span></button>
      </div>
    </div>

    <div class="card-pad" style="padding:8px 8px 4px;">
      <table class="tbl">
        <thead>
          <tr>
            <th>訂單編號</th>
            <th>買家</th>
            <th class="hide-sm">作品</th>
            <th class="hide-sm">狀態</th>
            <th class="hide-sm">時間</th>
            <th class="num">金額</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="o in rows" :key="o.id">
            <td style="font-family:var(--oj-mono); font-size:12.5px; color:var(--text-soft);">{{ o.id }}</td>
            <td>
              <div style="display:flex; align-items:center; gap:10px;">
                <span class="avatar" :style="{ background: o.avatar, width:'30px', height:'30px', fontSize:'11px' }">{{ F.initials(o.buyer) }}</span>
                <div style="min-width:0;">
                  <div style="font-weight:600; font-size:13.5px;">{{ o.buyer }}</div>
                  <div style="font-family:var(--oj-mono); font-size:11px; color:var(--text-faint);">{{ o.buyerHandle }}</div>
                </div>
              </div>
            </td>
            <td class="hide-sm" style="max-width:240px;">
              <div style="display:-webkit-box; -webkit-line-clamp:1; -webkit-box-orient:vertical; overflow:hidden; font-size:13.5px; color:var(--text-soft);">{{ prod(o.productId).title }}</div>
            </td>
            <td class="hide-sm"><span class="pill" :class="o.status"><span class="pdot"></span>{{ statusLabel(o.status) }}</span></td>
            <td class="hide-sm" style="font-family:var(--oj-mono); font-size:12px; color:var(--text-faint);">{{ dt(o.date) }}</td>
            <td class="num" style="font-weight:700;" :style="o.status === 'refunded' ? 'color:var(--text-faint); text-decoration:line-through' : ''">{{ F.money(o.amount) }}</td>
          </tr>
        </tbody>
      </table>
      <div v-if="!rows.length" class="empty-box">
        <div class="eb-ic"><app-icon name="receipt" :size="30" /></div>
        <div class="eb-t">沒有符合的訂單</div>
      </div>
    </div>
  </div>
</template>
