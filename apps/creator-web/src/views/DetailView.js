/* ============================================================
   Open Jam — DetailView (route: /product/:id)
   gallery / preview · file contents · add to cart
   ============================================================ */
import { useShopStore } from '../stores/shop.js';
import { CATEGORIES } from '../data/products.js';
import ProductThumb from '../components/ProductThumb.js';
import JIcon from '../components/JIcon.js';
import Stars from '../components/Stars.js';

const FILE_COLORS = {
  PDF: '#e0573e', MIDI: '#6151f0', MSCZ: '#7a6cff', AUDIO: '#c94f9e', WAV: '#c94f9e',
  JPG: '#2f9e6b', PNG: '#16a07a', RAW: '#3b7fd4', XMP: '#d8a017', XLSX: '#1f9d57',
  EPUB: '#8b5cf6', GP: '#d65a3a', TXT: '#888', NOTION: '#444', FIG: '#e0573e', DEFAULT: '#6151f0',
};

export default {
  name: 'DetailView',
  components: { ProductThumb, JIcon, Stars },
  setup() { return { store: useShopStore() }; },
  data() { return { active: 0 }; },
  computed: {
    p() { return this.store.product(this.$route.params.id); },
    fav() { return this.p && this.store.isFav(this.p.id); },
    inCart() { return this.p && this.store.inCart(this.p.id); },
    initials() { return this.p ? this.p.creator.split(' ').map((s) => s[0]).slice(0, 2).join('') : ''; },
    catLabel() { return this.p ? (CATEGORIES.find((c) => c.id === this.p.cat) || {}).label : ''; },
    totalFiles() { return this.p ? this.p.files.reduce((n, f) => n + f.count, 0) : 0; },
  },
  watch: {
    '$route.params.id'() { this.active = 0; },
  },
  methods: {
    fileColor(t) { return FILE_COLORS[t] || FILE_COLORS.DEFAULT; },
    goList() { this.$router.push({ name: 'list' }); },
    addCart() { this.store.addToCart(this.p.id); },
    buyNow() { if (!this.inCart) this.store.addToCart(this.p.id); this.$router.push({ name: 'checkout' }); },
    goCart() { this.$router.push({ name: 'checkout' }); },
  },
  template: `
    <div class="page page-pad" data-screen-label="產品詳細頁" v-if="p">
      <div class="breadcrumb">
        <a @click="goList">探索</a>
        <j-icon name="chevron" :size="14" />
        <a @click="goList">{{ catLabel }}</a>
        <j-icon name="chevron" :size="14" />
        <span style="color:var(--text-soft)">{{ p.title }}</span>
      </div>

      <div class="detail-grid">
        <!-- ---------- left: gallery + content ---------- -->
        <div>
          <product-thumb :product="p" class="gallery-main" :seed="active" :glyph-size="120"
                         :label="'預覽 ' + (active + 1) + ' / ' + p.previews" />
          <div class="gallery-thumbs">
            <product-thumb v-for="i in p.previews" :key="i" :product="p" :seed="i - 1"
                           :class="{ on: active === i - 1 }" :show-cat="false" :glyph-size="26"
                           hide-label @click="active = i - 1" />
          </div>

          <!-- description -->
          <div style="margin-top:44px">
            <h2 class="section-title">關於這份作品</h2>
            <div class="prose">
              <p v-for="(para, i) in p.desc" :key="i">{{ para }}</p>
            </div>
          </div>

          <!-- preview (locked) -->
          <div style="margin-top:40px">
            <h2 class="section-title">內容預覽</h2>
            <div class="preview-locked">
              <div class="lock-veil">
                <div style="width:46px;height:46px;border-radius:50%;display:grid;place-items:center;background:var(--surface);box-shadow:var(--shadow);color:var(--oj-primary)">
                  <j-icon name="lock" :size="22" />
                </div>
                <span class="lock-txt">完整內容於購買後立即解鎖</span>
              </div>
              <product-thumb :product="p" :seed="active + 2"
                             style="aspect-ratio:16/7; border-radius:var(--r-lg);"
                             :glyph-size="80" label="預覽（部分模糊）" :show-cat="false" />
            </div>
          </div>

          <!-- contents -->
          <div style="margin-top:40px">
            <h2 class="section-title">內容物（{{ totalFiles }} 個檔案）</h2>
            <div class="file-list">
              <div v-for="(f, i) in p.contents" :key="i" class="file-row">
                <div class="file-ic" :style="{ background: fileColor(f.type) }">{{ f.type }}</div>
                <div style="flex:1; min-width:0;">
                  <div class="file-name">{{ f.name }}</div>
                  <div class="file-meta">{{ f.type }} · {{ f.size }}</div>
                </div>
                <j-icon name="lock" :size="16" style="color:var(--text-faint)" />
              </div>
            </div>
          </div>
        </div>

        <!-- ---------- right: buy card ---------- -->
        <div>
          <div class="buy-card">
            <div style="display:flex; align-items:center; justify-content:space-between;">
              <span class="chip">{{ catLabel }}</span>
              <button class="fav" :class="{ on: fav }" style="position:static;"
                      @click="store.toggleFav(p.id)">
                <j-icon name="heart" :size="18" :fill="fav" />
              </button>
            </div>

            <h1 class="detail-title">{{ p.title }}</h1>

            <div class="creator-row">
              <span class="avatar" :style="{ background: p.avatar }">{{ initials }}</span>
              <div>
                <div class="creator-name">{{ p.creator }}</div>
                <div class="creator-handle">{{ p.handle }}</div>
              </div>
            </div>

            <div style="display:flex; align-items:baseline; justify-content:space-between; margin:20px 0 4px;">
              <span class="price-xl" :class="{ free: p.price === 0 }">{{ p.price === 0 ? '免費' : '$' + p.price }}</span>
              <stars :value="p.rating" :count="p.ratingCount" :size="15" />
            </div>

            <div style="display:flex; flex-direction:column; gap:10px; margin-top:22px;">
              <n-button type="primary" size="large" block strong @click="buyNow">
                {{ p.price === 0 ? '免費取得' : '立即購買' }}
              </n-button>
              <n-button v-if="!inCart" size="large" block secondary @click="addCart">
                <template #icon><j-icon name="cart" :size="18" /></template>
                加入購物車
              </n-button>
              <n-button v-else size="large" block secondary @click="goCart">
                <template #icon><j-icon name="check" :size="18" /></template>
                已在購物車 · 前往結帳
              </n-button>
            </div>

            <div class="trust">
              <j-icon name="shield" :size="14" /> 安全付款 · 購買後立即下載
            </div>

            <div class="spec-list" style="margin-top:22px;">
              <div class="spec-row">
                <span class="spec-k"><j-icon name="file" :size="16" /> 檔案數量</span>
                <span class="spec-v">{{ totalFiles }} 個</span>
              </div>
              <div class="spec-row">
                <span class="spec-k"><j-icon name="bag" :size="16" /> 檔案格式</span>
                <span class="spec-v">{{ p.formats.join(' · ') }}</span>
              </div>
              <div class="spec-row">
                <span class="spec-k"><j-icon name="download" :size="16" /> 總大小</span>
                <span class="spec-v">{{ p.totalSize }}</span>
              </div>
              <div class="spec-row">
                <span class="spec-k"><j-icon name="user" :size="16" /> 已售出</span>
                <span class="spec-v">{{ p.sales.toLocaleString() }} 份</span>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>`,
};
