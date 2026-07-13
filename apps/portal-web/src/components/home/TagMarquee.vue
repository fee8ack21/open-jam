<script setup lang="ts">
/* ============================================================
   TagMarquee — hero 底部的全幅標籤跑馬燈（呼應 Epidemic Sound
   的品牌輪播）。標籤取自實際商品 tags，點擊即搜尋；hover 暫停。
   互動在 hero 熱門搜尋列已有等價入口，故對輔助技術隱藏
   （aria-hidden + tabindex=-1），純滑鼠捷徑。
   ============================================================ */
import { computed } from 'vue';

const props = defineProps<{ items: string[] }>();
const emit = defineEmits<{ pick: [tag: string] }>();

// 內容重複到足夠寬，確保 -50% 位移的無縫循環不露出空隙
const seq = computed(() => {
  if (!props.items.length) return [];
  const out: string[] = [];
  while (out.length < 18) out.push(...props.items);
  return out;
});
</script>

<template>
  <div v-if="seq.length" class="tag-marquee" aria-hidden="true">
    <div class="mq-track">
      <div v-for="n in 2" :key="n" class="mq-seq">
        <button
          v-for="(tag, i) in seq"
          :key="n + '-' + i"
          type="button"
          class="mq-item"
          tabindex="-1"
          @click="emit('pick', tag)"
        >
          <app-icon name="note" :size="13" />
          {{ tag }}
        </button>
      </div>
    </div>
  </div>
</template>
