<script setup lang="ts">
/* ============================================================
   AboutView — 關於 Open Jam 靜態介紹頁（/about）
   說明平台用途：創作者子網域開店、消費者免註冊憑信箱購買。
   ============================================================ */
import { useShopStore } from '@/stores/shop.js';
import { CATEGORIES } from '@/data/products';
import { env } from '@/environment.js';
import AppNav from '@/layout/AppNav.vue';
import AppFooter from '@/layout/AppFooter.vue';

const store = useShopStore();

// 平台三大特色 — 對應 Controller/Service 的核心流程
const features = [
  {
    icon: 'bag',
    title: '一鍵開店',
    text: '創作者申請後即可在專屬子網域 <creator>.openjam.co 開設店面，上架樂譜、照片集或電子書。',
  },
  {
    icon: 'mail',
    title: '免註冊購買',
    text: '消費者無需建立帳號，結帳時憑電子信箱即可完成購買，下載連結直接寄送到信箱。',
  },
  {
    icon: 'download',
    title: '安全下載',
    text: '購買後由平台簽發具時效的下載連結，檔案經處理確認後才開放，保障創作者作品。',
  },
];

// 平台目前支援的數位商品分類
const categories = CATEGORIES;

function goWorkspace() { window.location.href = env.WORKSPACE_PAGE_URL; }
</script>

<template>
  <div class="oj-root" :class="'font-' + store.font" data-screen-label="關於 Open Jam">
    <!-- ============ NAV ============ -->
    <app-nav />

    <main class="page about-page">
      <nav class="breadcrumb" aria-label="麵包屑">
        <router-link to="/">市集</router-link>
        <app-icon name="chevron" :size="14" />
        <span>關於 Open Jam</span>
      </nav>

      <article class="about-card">
        <header class="about-head">
          <div class="about-badge">
            <app-icon name="sparkle" :size="24" />
          </div>
          <h1 class="about-title">關於 Open Jam</h1>
          <p class="about-lede">
            Open Jam 是台灣的創作者數位商品市集。我們讓創作者把樂譜、攝影作品與電子書直接賣給支持者，
            也讓每一位消費者用最少的步驟，買到值得收藏的數位作品。
          </p>
        </header>

        <section class="about-sec">
          <h2><span class="num">01</span> 平台是做什麼的</h2>
          <p>
            Open Jam 參考 Gumroad 的精神打造：創作者專注創作，平台負責開店、金流與檔案配送。
            創作者在自己的子網域開設店面、上架商品；消費者瀏覽市集、選購喜歡的作品，
            整個過程不需要繁瑣的註冊或下載 App。
          </p>
        </section>

        <section class="about-sec">
          <h2><span class="num">02</span> 它如何運作</h2>
          <div class="feature-grid">
            <div v-for="f in features" :key="f.title" class="feature-cell">
              <span class="feature-ic"><app-icon :name="f.icon" :size="20" /></span>
              <h3>{{ f.title }}</h3>
              <p>{{ f.text }}</p>
            </div>
          </div>
        </section>

        <section class="about-sec">
          <h2><span class="num">03</span> 你可以買到什麼</h2>
          <p>目前市集支援以下數位商品分類，並會持續擴充：</p>
          <ul class="cat-list">
            <li v-for="c in categories" :key="c.id">
              <app-icon :name="c.glyph" :size="17" />
              <span>{{ c.label }}</span>
            </li>
          </ul>
        </section>

        <div class="about-cta">
          <p>是創作者嗎？把你的作品帶到 Open Jam。</p>
          <button type="button" class="cta-btn" @click="goWorkspace">
            成為創作者 <app-icon name="chevron" :size="15" />
          </button>
        </div>
      </article>
    </main>

    <!-- ============ FOOTER ============ -->
    <app-footer />
  </div>
</template>

<style scoped>
.about-page { max-width: 820px; margin: 0 auto; padding-top: 30px; padding-bottom: 80px; }

.about-card {
  background: var(--surface); border: 1.5px solid var(--border-strong); border-radius: var(--r-lg);
  box-shadow: 6px 6px 0 var(--border); padding: 34px 40px 40px;
}

.about-head { border-bottom: 1.5px solid var(--border); padding-bottom: 24px; margin-bottom: 28px; }
.about-badge {
  width: 48px; height: 48px; border-radius: 14px; display: grid; place-items: center; color: #fff;
  border: 1.5px solid var(--text); box-shadow: 3px 3px 0 var(--text); margin-bottom: 16px;
  background: linear-gradient(135deg, var(--c-violet), var(--c-pink));
}
.about-title { font-family: var(--oj-display); font-weight: 800; font-size: 32px; letter-spacing: -1px; margin: 0; color: var(--text); }
.about-lede { font-size: 16px; line-height: 1.8; color: var(--text-soft); margin: 14px 0 0; }

.about-sec { margin-bottom: 26px; }
.about-sec h2 { font-family: var(--oj-display); font-weight: 700; font-size: 18px; color: var(--text); margin: 0 0 9px; display: flex; align-items: baseline; gap: 10px; }
.about-sec h2 .num { font-family: var(--oj-mono); font-size: 13px; color: var(--oj-primary); font-weight: 600; }
.about-sec p { margin: 0; font-size: 15px; line-height: 1.78; color: var(--text-soft); }

.feature-grid { display: grid; grid-template-columns: repeat(3, 1fr); gap: 16px; margin-top: 14px; }
.feature-cell {
  border: 1.5px solid var(--border); border-radius: var(--r-md); padding: 18px 16px; background: var(--oj-wash);
}
.feature-ic {
  display: inline-grid; place-items: center; width: 38px; height: 38px; border-radius: 11px;
  background: var(--surface); border: 1.5px solid var(--text); box-shadow: 2px 2px 0 var(--text);
  color: var(--oj-primary); margin-bottom: 12px;
}
.feature-cell h3 { font-family: var(--oj-display); font-weight: 700; font-size: 15.5px; color: var(--text); margin: 0 0 6px; }
.feature-cell p { font-size: 13.5px; line-height: 1.7; color: var(--text-soft); margin: 0; }

.cat-list { list-style: none; margin: 12px 0 0; padding: 0; display: flex; flex-wrap: wrap; gap: 10px; }
.cat-list li {
  display: inline-flex; align-items: center; gap: 8px; padding: 8px 14px;
  border: 1.5px solid var(--border-strong); border-radius: 999px; background: var(--surface);
  font-family: var(--oj-display); font-weight: 600; font-size: 14px; color: var(--text);
}

.about-cta {
  margin-top: 32px; padding-top: 24px; border-top: 1.5px dashed var(--border);
  display: flex; align-items: center; justify-content: space-between; gap: 16px; flex-wrap: wrap;
}
.about-cta p { margin: 0; font-family: var(--oj-display); font-weight: 700; font-size: 15px; color: var(--text); }
.cta-btn {
  display: inline-flex; align-items: center; gap: 7px; cursor: pointer;
  font-family: var(--oj-display); font-weight: 700; font-size: 15px; color: #fff;
  background: var(--oj-primary); border: 1.5px solid var(--text); border-radius: var(--r-sm);
  box-shadow: 3px 3px 0 var(--text); padding: 10px 18px; transition: transform .12s, box-shadow .12s;
}
.cta-btn:hover { transform: translate(-1px, -1px); box-shadow: 4px 4px 0 var(--text); }
.cta-btn:active { transform: translate(2px, 2px); box-shadow: 1px 1px 0 var(--text); }

@media (max-width: 700px) {
  .feature-grid { grid-template-columns: 1fr; }
}
@media (max-width: 560px) {
  .about-card { padding: 24px 20px 30px; }
  .about-title { font-size: 26px; }
}
</style>
