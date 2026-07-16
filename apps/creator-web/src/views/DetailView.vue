<script setup lang="ts">
import { computed, onMounted, ref, watch, watchEffect } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { useI18n } from 'vue-i18n';
import { useShopStore } from '@/stores/shop';
import { useAuthStore } from '@/stores/auth';
import { CATEGORIES, type PreviewMediaItem } from '@/data/products';
import ProductThumb from '@/components/ProductThumb.vue';
import AppIcon from '@/components/app-icon';
import Stars from '@/components/Stars.vue';
import ReviewList from '@/components/ReviewList.vue';

/* 檔案格式色塊（果醬色盤；深色底配白字） */
const FILE_COLORS: Record<string, string> = {
  PDF: '#ff6b35', MIDI: '#8a5cf6', MSCZ: '#8a5cf6', AUDIO: '#d6479b', WAV: '#d6479b',
  JPG: '#18a999', PNG: '#18a999', RAW: '#3b7fd4', XMP: '#d8a017', XLSX: '#18a999',
  EPUB: '#8a5cf6', GP: '#ff6b35', TXT: '#888888', NOTION: '#444444', FIG: '#ff6b35', DEFAULT: '#8a5cf6',
};

const store = useShopStore();
const auth = useAuthStore();
const route = useRoute();
const router = useRouter();
const { t } = useI18n();

const active = ref(0);

const p = computed(() => store.product(String(route.params.id)));

// 圖庫：封面 + 預覽媒體（圖片 / 上傳影片 / YouTube 嵌入）；無任何項目時退回色調佔位
const gallery = computed<PreviewMediaItem[]>(() => {
  if (!p.value) return [];
  const items: PreviewMediaItem[] = [];
  if (p.value.image) items.push({ kind: 'image', url: p.value.image });
  items.push(...(p.value.previewMedia ?? []));
  return items;
});
// 目前選中的圖庫項目（超出範圍時回到第一項）
const activeItem = computed(() => gallery.value[active.value] ?? gallery.value[0]);
// 主圖（僅圖片項目）：以目前選中的圖片覆寫 image
const galleryProduct = computed(() => {
  if (!p.value) return null;
  return activeItem.value?.kind === 'image' ? { ...p.value, image: activeItem.value.url } : p.value;
});
const ytEmbedUrl = (id: string) => `https://www.youtube-nocookie.com/embed/${id}`;
const ytThumbUrl = (id: string) => `https://i.ytimg.com/vi/${id}/hqdefault.jpg`;
const fav = computed(() => (p.value ? store.isFav(p.value.id) : false));
const inCart = computed(() => (p.value ? store.inCart(p.value.id) : false));
const initials = computed(() => (p.value ? p.value.creator.split(' ').map((s) => s[0]).slice(0, 2).join('') : ''));
const catLabel = computed(() =>
  p.value && CATEGORIES.some((c) => c.id === p.value!.cat) ? t('category.' + p.value.cat) : '',
);
const totalFiles = computed(() => (p.value ? p.value.files.reduce((n, f) => n + f.count, 0) : 0));
// 「關於這份作品」段落：詳細介紹（空行分段）優先，未填時退回一句話簡介；皆空則整段隱藏
const aboutParas = computed(() => {
  if (!p.value) return [];
  if (p.value.desc.length) return p.value.desc;
  return p.value.blurb ? [p.value.blurb] : [];
});

// 載入完成前不可判定為不存在（避免直接連結進入時誤導向 not-found）
const ready = ref(false);
async function ensureLoaded(id: string) {
  ready.value = false;
  await store.loadCatalog();
  await store.loadProduct(id);
  ready.value = true;
  store.recordView(id); // 進頁瀏覽 +1（fire-and-forget，不阻塞畫面）
}
onMounted(() => ensureLoaded(String(route.params.id)));
watch(() => route.params.id, (id) => { active.value = 0; ensureLoaded(String(id)); });
watchEffect(() => { if (ready.value && !p.value) router.replace({ name: 'not-found' }); });

const fileColor = (t: string) => FILE_COLORS[t] || FILE_COLORS.DEFAULT;
const goList = () => router.push({ name: 'list' });
const addCart = () => { if (p.value) store.addToCart(p.value.id); };
const buyNow = () => { if (p.value && !inCart.value) store.addToCart(p.value.id); router.push({ name: 'checkout' }); };
const goCart = () => router.push({ name: 'checkout' });
</script>

<template>
  <div class="page page-pad" :data-screen-label="t('detail.screenLabel')" v-if="p">
    <div class="breadcrumb">
      <a @click="goList">{{ t('common.explore') }}</a>
      <span class="sep">›</span>
      <a @click="goList">{{ catLabel }}</a>
      <span class="sep">›</span>
      <span>{{ p.title }}</span>
    </div>

    <div class="detail-grid">
      <!-- left: gallery + content -->
      <div>
        <!-- 主展示區：依項目類型切換圖片 / 影片播放器 / YouTube 嵌入（key 確保切換時銷毀重建、停止播放） -->
        <video v-if="activeItem?.kind === 'video'" :key="activeItem.url"
               class="gallery-main gallery-media" :src="activeItem.url" controls preload="metadata"></video>
        <iframe v-else-if="activeItem?.kind === 'youtube' && activeItem.youtubeId" :key="activeItem.youtubeId"
                class="gallery-main gallery-media" :src="ytEmbedUrl(activeItem.youtubeId)"
                title="YouTube video player" frameborder="0" allowfullscreen
                allow="accelerometer; clipboard-write; encrypted-media; gyroscope; picture-in-picture"></iframe>
        <product-thumb v-else :product="galleryProduct ?? p" class="gallery-main" :seed="active" :glyph-size="120"
                       :label="gallery.length > 1 ? t('detail.previewLabel', { current: active + 1, total: gallery.length }) : ''" />
        <div v-if="gallery.length > 1" class="gallery-thumbs">
          <template v-for="(item, i) in gallery" :key="item.url + i">
            <product-thumb v-if="item.kind === 'image'" :product="{ ...p, image: item.url }"
                           :class="{ on: active === i }" :show-cat="false" :glyph-size="26"
                           hide-label @click="active = i" />
            <button v-else type="button" class="thumb media-thumb" :class="{ on: active === i }" @click="active = i">
              <img v-if="item.kind === 'youtube' && item.youtubeId" :src="ytThumbUrl(item.youtubeId)" alt="" loading="lazy" />
              <video v-else :src="item.url" preload="metadata" muted></video>
              <span class="media-play"><app-icon name="play" :size="16" /></span>
            </button>
          </template>
        </div>

        <div v-if="aboutParas.length" style="margin-top:44px">
          <h2 class="section-title">{{ t('detail.aboutTitle') }}</h2>
          <div class="prose">
            <p v-for="(para, i) in aboutParas" :key="i">{{ para }}</p>
          </div>
        </div>

        <div style="margin-top:44px">
          <i18n-t keypath="detail.contentsTitle" tag="h2" class="section-title" scope="global">
            <template #count><small>{{ t('detail.contentsCount', { count: totalFiles }) }}</small></template>
          </i18n-t>
          <div class="file-list">
            <div v-for="(f, i) in p.contents" :key="i" class="file-row">
              <div class="file-ic" :style="{ background: fileColor(f.type) }">{{ f.type }}</div>
              <div style="flex:1; min-width:0;">
                <div class="file-name">{{ f.name }}</div>
                <div class="file-meta">{{ f.type }}・{{ f.size }}</div>
              </div>
              <span class="file-lock"><app-icon name="lock" :size="15" /></span>
            </div>
          </div>
        </div>

        <review-list :catalog-id="p.id" />
      </div>

      <!-- right: buy card -->
      <div class="buy-card">
          <div style="display:flex; align-items:center; justify-content:space-between;">
            <span class="chip" style="background:var(--t-green)">{{ catLabel }}</span>
            <button v-if="!auth.isAdmin" class="fav" :class="{ on: fav }" :title="t('detail.favTitle')"
                    @click="store.toggleFav(p.id)">
              <app-icon :name="fav ? 'heart' : 'heartLine'" :size="18" />
            </button>
          </div>

          <h1 class="detail-title">{{ p.title }}</h1>

          <div class="creator-row">
            <span class="avatar" :style="p.avatarUrl ? undefined : { background: p.avatar }">
              <img v-if="p.avatarUrl" :src="p.avatarUrl" alt="" />
              <template v-else>{{ initials }}</template>
            </span>
            <div>
              <div class="creator-name">{{ p.creator }}</div>
              <div class="creator-handle">{{ p.handle }}</div>
            </div>
          </div>

          <div class="price-row">
            <span class="price-xl" :class="{ free: p.price === 0 }">{{ p.price === 0 ? t('common.free') : '$' + p.price }}</span>
            <stars :value="p.rating" :count="p.ratingCount" :size="14" />
          </div>

          <button type="button" class="cta-ink block" @click="buyNow">
            <app-icon :name="p.price === 0 ? 'arrowD' : 'cart'" :size="15" />
            {{ p.price === 0 ? t('detail.getFree') : t('detail.buyNow') }}
          </button>
          <button v-if="!inCart" type="button" class="cta-line block" style="margin-top:12px" @click="addCart">
            <app-icon name="cart" :size="15" />
            {{ t('detail.addToCart') }}
          </button>
          <button v-else type="button" class="cta-line block" style="margin-top:12px; --hover-c:var(--t-green)" @click="goCart">
            <app-icon name="check" :size="15" />
            {{ t('detail.inCart') }}
          </button>

          <div class="trust">
            <app-icon name="shield" :size="14" /> {{ t('detail.trust') }}
          </div>

          <div class="spec-list">
            <div class="spec-row">
              <span class="spec-k"><app-icon name="doc" :size="13" /> {{ t('detail.specFiles') }}</span>
              <i18n-t keypath="detail.specFilesValue" tag="span" class="spec-v" scope="global">
                <template #count><b>{{ totalFiles }}</b></template>
              </i18n-t>
            </div>
            <div class="spec-row">
              <span class="spec-k"><app-icon name="copy" :size="13" /> {{ t('detail.specFormats') }}</span>
              <span class="spec-v"><b>{{ p.formats.join('・') }}</b></span>
            </div>
            <div class="spec-row">
              <span class="spec-k"><app-icon name="download" :size="13" /> {{ t('detail.specSize') }}</span>
              <span class="spec-v"><b>{{ p.totalSize }}</b></span>
            </div>
            <div class="spec-row">
              <span class="spec-k"><app-icon name="tag" :size="13" /> {{ t('detail.specSales') }}</span>
              <i18n-t keypath="detail.specSalesValue" tag="span" class="spec-v" scope="global">
                <template #count><b>{{ p.sales.toLocaleString() }}</b></template>
              </i18n-t>
            </div>
            <div class="spec-row">
              <span class="spec-k"><app-icon name="eye" :size="13" /> {{ t('detail.specViews') }}</span>
              <i18n-t keypath="detail.specViewsValue" tag="span" class="spec-v" scope="global">
                <template #count><b>{{ (p.views ?? 0).toLocaleString() }}</b></template>
              </i18n-t>
            </div>
          </div>

          <div v-if="p.price === 0" class="hand-note" style="display:block; text-align:center; font-size:22px; margin-top:14px;">
            {{ t('detail.handNoteFree') }}
          </div>
        </div>
    </div>
  </div>
</template>

<style scoped>
/* 影片 / YouTube 主展示區：沿用 .gallery-main 外框（16/10），深色底避免 letterbox 白邊 */
.gallery-media { display: block; width: 100%; background: #1a1a1a; object-fit: contain; }

/* 影片 / YouTube 縮圖磚：深色底 + 中央播放徽章（外框樣式沿用 .gallery-thumbs .thumb） */
.media-thumb { padding: 0; background: #1a1a1a; }
.media-thumb img,
.media-thumb video { width: 100%; height: 100%; object-fit: cover; display: block; }
.media-play {
  position: absolute; top: 50%; left: 50%; transform: translate(-50%, -50%); z-index: 2;
  width: 34px; height: 34px; border-radius: 999px; display: grid; place-items: center;
  background: rgba(255, 255, 255, .92); border: var(--bw) solid var(--border-strong);
  color: var(--text); pointer-events: none;
}
.media-play svg { margin-left: 2px; }
</style>
