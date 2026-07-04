<script setup lang="ts">
/* ============================================================
   訂單下載頁 — 訂單完成信中的連結導向此頁。
   訪客不需登入：訂單 ID（不可猜測的 GUID）即為下載憑證，
   後端於 CatalogService downloads 端點以 ?orderId= 驗證
   訂單已完成且包含該商品後簽發短效下載 URL。
   ============================================================ */
import { computed, onMounted, ref } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { useI18n } from 'vue-i18n';
import { catalogApi, orderApi } from '@/api';
import { OrderStatus, type OrderResponse } from '@/api/order-service';
import type { PurchasedVersionAssetDto } from '@/api/catalog-service';
import AppIcon from '@/components/app-icon';

const route = useRoute();
const router = useRouter();
const { t } = useI18n();

const orderId = computed(() => String(route.params.orderId ?? ''));

const loading = ref(true);
const notFound = ref(false);
const order = ref<OrderResponse | null>(null);
/** 各訂單項目的可下載檔案（key = OrderItem.id）；null 表示該項目載入失敗。 */
const downloads = ref<Record<string, PurchasedVersionAssetDto[] | null>>({});

const isCompleted = computed(() => order.value?.status === OrderStatus.Completed);

/** 檔案大小人類可讀格式。 */
function formatSize(bytes?: number): string {
  if (!bytes || bytes <= 0) return '';
  const units = ['B', 'KB', 'MB', 'GB'];
  let n = bytes;
  let i = 0;
  while (n >= 1024 && i < units.length - 1) { n /= 1024; i++; }
  return `${n >= 10 || i === 0 ? Math.round(n) : n.toFixed(1)} ${units[i]}`;
}

/** 最低貨幣單位金額 → 顯示字串（如 1990 → "19.90 USD"）。 */
function formatAmount(minor?: number, currency?: string | null): string {
  const amount = ((minor ?? 0) / 100).toFixed(2).replace(/\.00$/, '');
  return `${amount} ${(currency ?? '').toUpperCase()}`;
}

async function loadDownloads(o: OrderResponse) {
  await Promise.all(
    (o.items ?? []).map(async (item) => {
      const key = item.id ?? '';
      try {
        const res = await catalogApi.catalogVersions.listPurchasedDownloads(
          item.catalogId!, item.catalogVersionId!, { orderId: orderId.value });
        downloads.value[key] = res.data;
      } catch {
        downloads.value[key] = null;
      }
    }),
  );
}

async function load() {
  loading.value = true;
  notFound.value = false;
  try {
    const res = await orderApi.orders.get(orderId.value);
    order.value = res.data;
    if (order.value.status === OrderStatus.Completed) await loadDownloads(order.value);
  } catch {
    notFound.value = true;
  } finally {
    loading.value = false;
  }
}

onMounted(load);

const goList = () => router.push({ name: 'list' });
</script>

<template>
  <div class="page page-pad" :data-screen-label="t('order.screenLabel')">

    <!-- LOADING -->
    <div v-if="loading" style="display:flex; justify-content:center; padding:80px 0;">
      <n-spin size="large" />
    </div>

    <!-- NOT FOUND -->
    <div v-else-if="notFound || !order" class="success-wrap">
      <h1 class="h-title" style="text-align:center">{{ t('order.errorTitle') }}</h1>
      <p class="h-sub" style="text-align:center">{{ t('order.errorDesc') }}</p>
      <div style="display:flex; justify-content:center; margin-top:24px;">
        <n-button size="large" type="primary" @click="goList">{{ t('order.backToStore') }}</n-button>
      </div>
    </div>

    <!-- ORDER -->
    <div v-else style="max-width:720px; margin:0 auto;">
      <p class="h-eyebrow">{{ t('order.orderNumber') }} {{ order.orderNumber }}</p>
      <h1 class="h-title">{{ t('order.title') }}</h1>
      <p class="h-sub">
        {{ t('order.total') }}：<b style="color:var(--text)">{{ formatAmount(order.totalAmount, order.currency) }}</b>
      </p>

      <!-- 未完成付款 -->
      <n-alert v-if="!isCompleted" type="warning" style="margin-top:24px;" :title="t('order.pendingTitle')">
        {{ t('order.pendingDesc') }}
      </n-alert>

      <!-- 項目與下載 -->
      <div v-else class="panel" style="margin-top:26px; text-align:left;">
        <div v-for="item in order.items" :key="item.id" style="padding:16px 0; border-bottom:1px solid var(--line, rgba(128,128,128,0.15));">
          <div style="display:flex; justify-content:space-between; align-items:baseline; gap:12px;">
            <div style="font-weight:600;">{{ item.catalogName }}</div>
            <div style="font-size:13px; color:var(--text-faint); font-family:var(--oj-mono); white-space:nowrap;">
              {{ formatAmount(item.unitPrice, order.currency) }}
            </div>
          </div>

          <!-- 檔案清單 -->
          <div v-if="downloads[item.id ?? '']?.length" style="margin-top:10px; display:flex; flex-direction:column; gap:8px;">
            <div
              v-for="file in downloads[item.id ?? '']"
              :key="file.id"
              style="display:flex; justify-content:space-between; align-items:center; gap:12px;"
            >
              <span style="font-size:13.5px; font-family:var(--oj-mono); overflow:hidden; text-overflow:ellipsis;">
                {{ file.fileName }}
                <span style="color:var(--text-faint); margin-left:8px;">{{ formatSize(file.fileSize) }}</span>
              </span>
              <n-button size="small" type="primary" secondary tag="a" :href="file.downloadUrl ?? undefined" target="_blank" rel="noopener">
                <template #icon><app-icon name="download" :size="15" /></template>
                {{ t('order.download') }}
              </n-button>
            </div>
          </div>
          <p v-else-if="downloads[item.id ?? ''] === null" style="margin:10px 0 0; font-size:13px; color:var(--err, #e5484d);">
            {{ t('order.loadFilesError') }}
          </p>
          <p v-else style="margin:10px 0 0; font-size:13px; color:var(--text-faint);">
            {{ t('order.noFiles') }}
          </p>
        </div>

        <p style="margin:16px 0 0; font-size:12.5px; color:var(--text-faint);">
          {{ t('order.linkNote') }}
        </p>
      </div>

      <div style="display:flex; gap:12px; margin-top:24px;">
        <n-button size="large" @click="goList">{{ t('order.backToStore') }}</n-button>
        <n-button v-if="isCompleted" size="large" secondary @click="load">{{ t('order.refresh') }}</n-button>
      </div>
    </div>
  </div>
</template>
