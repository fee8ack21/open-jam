<script setup lang="ts">
/* ============================================================
   ListPager — 列表分頁器。

   包裝 naive-ui n-pagination（頁碼），並在旁附上「每頁筆數」
   選擇器：下拉選單只呈現數字，前後的「每頁」/「筆」為純文字。
   頁碼與頁面大小皆為受控屬性，分別以 update:page /
   update:pageSize 事件回拋，由呼叫端（view / store）決定如何
   重新載入資料。
   ============================================================ */
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'

const props = withDefaults(
  defineProps<{
    /** 目前頁碼（1-based） */
    page: number
    /** 總頁數 */
    pageCount: number
    /** 目前每頁筆數 */
    pageSize: number
    /** 可選的每頁筆數選項 */
    pageSizes?: number[]
  }>(),
  { pageSizes: () => [10, 20, 50, 100] },
)

defineEmits<{
  (e: 'update:page', value: number): void
  (e: 'update:pageSize', value: number): void
}>()

const { t } = useI18n()

// 下拉只顯示數字，前後綴文字另以純文字呈現
const sizeOptions = computed(() =>
  props.pageSizes.map((n) => ({ label: String(n), value: n })),
)
const prefix = computed(() => t('common.perPagePrefix'))
const suffix = computed(() => t('common.perPageSuffix'))
</script>

<template>
  <div class="list-pager">
    <div class="list-pager__size">
      <span v-if="prefix" class="list-pager__text">{{ prefix }}</span>
      <n-select
        :value="pageSize"
        :options="sizeOptions"
        size="small"
        :consistent-menu-width="false"
        :menu-props="{ class: 'list-pager__menu' }"
        class="list-pager__select"
        @update:value="$emit('update:pageSize', $event)" />
      <span v-if="suffix" class="list-pager__text">{{ suffix }}</span>
    </div>
    <n-pagination
      :page="page"
      :page-count="pageCount"
      @update:page="$emit('update:page', $event)" />
  </div>
</template>

<style scoped>
.list-pager {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 16px;
}

.list-pager__size {
  display: flex;
  align-items: center;
  gap: 8px;
}

.list-pager__text {
  font-size: 13px;
  color: var(--text-soft);
  white-space: nowrap;
}

.list-pager__select {
  width: 72px;
}

/* base.css 全域給 .n-base-selection 加了硬派位移陰影（2px 2px 0），
   此處的每頁筆數選擇器不需要，覆寫掉（需 !important 蓋過 .oj-root 兩層 class） */
.list-pager__select :deep(.n-base-selection),
.list-pager__select :deep(.n-base-selection:focus-within),
.list-pager__select :deep(.n-base-selection.n-base-selection--active),
.list-pager__select :deep(.n-base-selection.n-base-selection--focus) {
  box-shadow: none !important;
}
</style>

<!-- 下拉選單 teleport 至 body，需非 scoped 樣式才能移除其陰影
     （base.css 的 .oj-root .n-select-menu 帶 !important，故此處也需 !important） -->
<style>
.list-pager__menu.n-select-menu {
  box-shadow: none !important;
  border: 1px solid var(--border);
}
</style>
