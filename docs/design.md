# 視覺設計準則

Open Jam 三個前端（portal-web / creator-web / workspace-web）共用同一套視覺系統 **「Jam Jar Pop」（v3 Neo-Brutalism）**：奶油紙底 + 墨線邊框 + 果醬糖果色 + 手寫註記字體 + 貼紙式硬底影，刻意營造「手作果醬罐」的工藝感與玩味，區別於一般 SaaS 後台的扁平風格。三個 app 各自的 `assets/styles/base.css` 內容幾乎一致（同一份設計 token 各自維護一份，**非共用套件**），修改視覺系統時三處需同步調整；workspace-web 因是管理後台，多數 token 主動收斂一號（見下方各節「後台變體」）。

> Auth（`src/Auth/wwwroot/css/site.css`）與 EmailService 郵件模板也獨立維護了同一套墨線 / 硬底影語彙（非共用此三個 app 的 token 檔），修改品牌視覺時如需求涵蓋登入頁或信件，需另外同步該兩處，不在本文件範圍內。

## 色彩

### 果醬色盤（`:root`）

```css
--c-violet: #8a5cf6;
--c-pink:   #ff90e8;
--c-pink-deep: #d6479b;
--c-orange: #ff6b35;
--c-lime:   #b8ff9f;
--c-cyan:   #7dd9ff;
--c-yellow: #ffde00;
```

六色為扁平飽和色。`--oj-primary`（= violet）/ `--oj-soft` / `--oj-wash` 是語意化別名，元件一律用語意變數，不要直接寫 `--c-violet`（品牌色僅用於強調色來源、分類 dot、漸層底如 `.cc-visual` 信用卡視覺 `linear-gradient(135deg, var(--c-violet), var(--c-pink-deep))`）。workspace-web 另保留 `--oj-primary-wash`（同 `--oj-wash`）供舊 scoped style 引用。

### 便條淡彩（`--t-*`）

```css
--t-pink: #ffe3f6;  --t-yellow: #fff3c4;  --t-green: #dff5d3;
--t-blue: #e4f6ff;  --t-violet: #e9dfff;
```

用於卡片縮圖底色（`--thumb-bg`）與區塊 band 背景，模擬便利貼色紙，不用於文字或邊框。

### 語意色 token

```css
--bg / --surface / --surface-2      /* 奶油紙背景 #fff9ec、卡片面（白）、次要面 */
--text / --text-soft / --text-faint /* 文字三階層，主色 #1a1a1a */
--border / --border-strong          /* border：紙感淡線（虛線分隔）；border-strong 為墨線主框顏色（一律 #1a1a1a） */
--shadow / --shadow-hover           /* 卡片/面板用的一般柔和陰影 */
```

**目前狀態**：三個 app 皆只有單一 `:root` token 組，**未實作深色模式**（先前 workspace-web 曾有的 `.oj-root.light/.dark` 雙 scope 與徽章切換開關已隨 v3 簡約化整併移除）。新增深色模式需三處重新設計 token 分層，非目前架構的既有分岔。

### Naive UI 主題覆寫

各 app `App.vue` 以 `n-config-provider :theme-overrides` 覆寫 Naive UI 預設主題，三處各自維護、**主色數值不完全一致**（歷史遺留，非刻意分流）：

| App | primaryColor | 備註 |
|---|---|---|
| portal-web | `#6151f0` | 與 `--c-violet` 不同（歷史遺留） |
| creator-web | `#8a5cf6` | 與 `--c-violet` 一致 |
| workspace-web | `#8a5cf6` | 與 `--c-violet` 一致；另指定 `:theme="null"`（已無內建深色主題切換可停用，純為避免 Naive UI 預設 light theme 疊加自訂 token） |

新增/調整品牌主色時，`--c-violet`（CSS token）與各 `App.vue` 的 `primaryColor`（Naive UI token）要一起改，兩套系統不會互相同步。

## 字體

```css
--oj-display: 'Space Grotesk', 'Noto Sans TC', sans-serif;  /* 標題 / 展示字 / 數字 / mono 感元素 */
--oj-font:    'Noto Sans TC', sans-serif;                    /* 內文 / UI（含中英混排，無獨立英文字體） */
--oj-mono:    'Space Grotesk', 'Noto Sans TC', sans-serif;   /* 與 --oj-display 同源，語意上標記數字/代碼感文字 */
--oj-hand:    'Caveat', cursive;                              /* 手寫註記（v3 新增） */
```

- 中文一律吃 `Noto Sans TC`；三套字體 stack 都以它作為 fallback，不要在中英混排時另外指定中文字體。
- `--oj-hand`（Caveat）是 v3 最具識別度的新增元素：用於 `.hand-note` 手寫強調文字、免費商品價格貼紙（`.price.free`）等「手寫感額外註記」，字重固定 700、常搭配 `rotate(-2deg)` 等小角度傾斜；**不要用在需要精確辨識的內容**（金額、表單、正式文案）。
- 標題常見手法：`font-weight: 900`，大字級搭配 `letter-spacing: -1px` 左右收緊；一般 UI 文字多用 `font-weight: 700`。
- 過往「Bricolage Grotesque + JetBrains Mono」字體堆疊與 eyebrow 用 `--oj-mono` + uppercase 的手法已不再是主要工藝感來源，目前工藝感改由手寫字 + 墨線 + 貼紙傾斜共同承擔。

## 間距、圓角與墨線寬

```css
--bw: 1.5px;   /* 墨線主框寬（v3 token 化，前身為寫死的 2px；2026-07 簡約化統一收斂為 1.5px） */
--nav-h: 76px; /* portal-web / workspace-web；creator-web 為 72px */
--maxw: 1160px; /* 內容最大寬度，creator-web 有定義；portal/workspace 於各版面自訂或用 --page padding */
```

| Token | portal-web / creator-web | workspace-web（後台變體，收斂一號） |
|---|---|---|
| `--r-lg`（卡片、面板、大型容器） | 20px | 12px |
| `--r-md`（縮圖、輸入框、次級容器） | 14px | 10px |
| `--r-sm`（icon 按鈕、logo mark） | 10px | 8px |
| `--r-pill`（膠囊/圓角矩形按鈕） | 不需要，直接寫 `999px`（真膠囊） | 10px（**圓角矩形取代膠囊**，後台按鈕/標籤一律用此 token） |

圓角越大用在越「主要」的容器；workspace-web 因是資訊密度較高的後台，膠囊形狀全面改為圓角矩形（`--r-pill`），portal-web / creator-web（消費端）維持真膠囊 `border-radius: 999px`（直接寫死，非 token）。頭像類（`.avatar` / `.hero-avatar`）三處皆維持正圓 `50%`，不受此變體影響。

## 陰影系統

v3 有兩套並行、用途分明的陰影語言：

### 1. 軟投影階（`--pop-*`）— 浮層與 hover 升級

```css
--pop-1: 0 4px 10px rgba(26,26,26,.1);    --pop-1-h: 0 8px 16px rgba(26,26,26,.16);
--pop-2: 0 8px 20px rgba(26,26,26,.1);    --pop-2-h: 0 12px 26px rgba(26,26,26,.16);
--pop-3: 0 10px 24px rgba(26,26,26,.12);  --pop-3-h: 0 18px 34px rgba(26,26,26,.18);
```

用於 popover / dropdown / dialog / 表單 focus 態，以及卡片 hover（見下）。`-h` 尾碼是對應的 hover/active 加深版本，搭配元件 `translateY(-Npx)` 位移一起用，**不是**靠陰影本身位移擴大。

### 2. 貼紙式硬底影（`--ink-drop`）— 靜態底座，不隨 hover 生長

```css
--ink-drop: 0 3px 0 rgba(26,26,26,.9);     /* 主要：卡片、CTA 按鈕、highlight 貼紙、preview 鎖定框 */
--ink-drop-sm: 0 2px 0 rgba(26,26,26,.85); /* 次要：file icon、小型 CTA、選中態縮圖 */
```

無 blur、無 x 偏移，純粹的「紙貼在墨線上」垂直偏移，模擬貼紙厚度。**這是本次（2026-07）簡約化的重點變更**：舊版硬影是 5~6px 偏移、且會在 hover 時整體 + 元件位移一起放大（`translate(-2px,-2px)` + 陰影同步生長）；v3 簡約化後改為固定 2~3px 的靜態薄影，**hover 不再讓 ink-drop 生長**，互動回饋改由「元件 `translateY` 上浮 + 軟投影 `--pop-*-h` 加深」這條路徑承擔（例：`.card:hover`、`.cat-pill:hover`、`.file-row:hover`）。新增元件時不要再模仿舊版「陰影跟著位移同步放大」的寫法。

搭配貼紙傾斜（`rotate(-6deg)` ~ `rotate(4deg)` 不等，愛心收藏鈕、hl 標籤、polaroid 卡片、file icon 交錯左右傾都各自取值），是這套視覺語言表現「非機械感」的主要手法。

## 元件慣例

- **卡片**（`.card`）：`border: var(--bw) solid var(--border-strong)`、`border-radius: var(--r-lg)`。portal-web / creator-web：預設 `box-shadow: none`，hover 時 `translateY(-5px) rotate(-.5deg)` + `box-shadow: var(--pop-3-h)`。workspace-web（後台變體）：預設就帶靜態 `box-shadow: var(--ink-drop)`，hover 只做 `translateY(-3px) rotate(-.4deg)` 位移、不疊加/更換陰影（後台密度高，簡化 hover 回饋層級）。
- **CTA 按鈕**：`.cta-ink`（主要）純黑底 + 黃字 + 膠囊，hover `translateY(-3px) rotate(-.5deg)` + `box-shadow: 0 12px 26px rgba(26,26,26,.3)`；`.cta-line`（次要）白底墨線框，hover 背景換成 `var(--hover-c, var(--c-yellow))`。**舊版「yellow→orange 漸層 CTA」已不存在**，全站主 CTA 統一為黑底黃字。
- **Pill / Chip**：`.cat-pill` / `.tag-toggle` / `.chip` 膠囊形（workspace-web 為圓角矩形，見上）。選中態（`.on`）預設全站統一為「黑底黃字」；portal-web 的分類 pill 額外保留少數個別分類色覆寫（`.c-music` 紫 / `.c-photo` 粉 / `.c-cyan` 青），creator-web 已收斂為單一黑底黃字、不再逐分類上色 —— 新增分類相關 pill 時以「統一黑底黃字」為預設，除非明確要做 portal-web 那種分類強調色。
- **品牌 Logo（果醬罐 hover 動效）**：`BrandLogo.vue`（portal-web / creator-web，`src/components/BrandLogo.vue`）與 workspace-web 側欄 `.side-brand`（`src/layout/AppSidebar.vue` + `workspace.css`，2026-07 新增，同語彙）共用同一套 SVG 結構與 class 命名（`.oj-lid` / `.oj-splash` / `.oj-smear`）：hover 時罐蓋（`.oj-lid`）彈開旋轉、果醬噴濺（`.oj-splash`）浮現、字樣被抹上果醬光澤（`.oj-smear`）。三處各自 inline 這組 CSS（無共用元件），新增/微調時三處要一起改；`prefers-reduced-motion: reduce` 時整組 transition 直接關閉（`transition: none`），非漸進裝飾。
- **Naive UI 表單元件（兩種力度）**：
  - portal-web / creator-web（消費端）：`.n-input` 預設**不畫額外邊框**（吃 Naive UI 原生），只在 `:focus-within` 時加 `box-shadow: var(--pop-1)` + `translateY(-1px)`；錯誤態用 `box-shadow` 暈染（`color-mix` 混 `--c-pink-deep`），維持留白乾淨的消費端觀感。
  - workspace-web（後台）：全面覆寫為「墨線恆常可見」——`--n-border` 系列一律強制 `var(--bw) solid var(--border-strong)`（不論 hover/focus/active 都是同一條墨線），圓角改 `--r-pill`，focus/active 才疊加 `--pop-1` 陰影；錯誤態邊框直接轉 `--c-pink-deep`。新增表單元件時依所在 app 選對應力度，不要混用。
- **Icon（兩種架構，非統一）**：
  - portal-web / creator-web：`src/components/app-icon/icon-paths.ts` 每個 icon 的 `body`（SVG 內容字串）**自帶 stroke/fill**，`AppIcon.vue` 純粹依 `vb`（預設 `'0 0 24 24'`，非正方形時依寬高比例縮放）渲染，**無 `stroke` prop**可調。
  - workspace-web：`AppIcon.vue` 額外暴露 `stroke` prop（預設 `1.8`，2026-07 簡約化前為 `2.1`），icon 皆為統一線寬的線稿（`stroke-linecap/linejoin: round`），可透過 prop 動態調整筆觸粗細。
  新增 icon 時比照對應 app 既有筆畫風格繪製，不要跨 app 直接複製 path data（stroke 語意不同）。

## 動效

- **互動 hover**：`translateY` 上浮 + 陰影升級（`--pop-*-h`，見上）+ 輕微 `rotate` 貼紙傾斜是預設語言；`transition` 多用 `var(--ease-pop)`（`cubic-bezier(.34, 1.56, .64, 1)`，回彈感明顯的 easing，全站共用單一 token，不要另開新的 easing 曲線）。
- **Landing storytelling（portal-web `/landing`，`LandingView.vue`，依「Open Jam Landing v3」設計稿）**：六場景固定舞台捲動敘事（入口漂浮卡 → 音樂鍵盤試彈 WebAudio → 攝影相紙顯影 → 電子書翻頁試讀 → 市集規則卡 → 墨黑收合網格 + 雙 CTA）。**目前以原生 `scroll` 事件 + `requestAnimationFrame` 驅動**（非 GSAP/ScrollTrigger；`package.json` 仍列 `gsap` 依賴但目前程式碼未 import，屬待清理的多餘依賴，非現行機制的一部分）。`prefers-reduced-motion: reduce` 時整頁退化為一般直向排列（無固定舞台、無鏡頭轉場），互動元件（鋼琴 / 顯影 / 翻頁）仍可操作 —— 這是可及性 fallback，不是漸進增強裝飾，改動時務必保留這個分岔。若要改動或除錯捲動邏輯，直接讀 `LandingView.vue` 內的 `<script setup>` 頂部長註解與 `frame()` / `raf` 相關程式碼，不要套用舊版 GSAP pitfalls（已隨改寫失效）。

## 檔案位置

```
apps/<app>/src/assets/styles/base.css     # 設計 token、全域元件 class（三份需同步維護）
apps/portal-web/src/assets/styles/market.css      # 前台市集版面
apps/creator-web/src/assets/styles/list.css       # 創作者店面商品列表版面
apps/workspace-web/src/assets/styles/workspace.css # 後台版面（含側欄 .side-brand 果醬 hover）
apps/<app>/src/App.vue                    # Naive UI theme-overrides
apps/portal-web/src/components/BrandLogo.vue      # 果醬罐 logo（portal-web，creator-web 同名同結構）
apps/creator-web/src/components/BrandLogo.vue
apps/workspace-web/src/layout/AppSidebar.vue       # 後台側欄（logo hover 語彙 inline 於 workspace.css）
apps/<app>/src/components/app-icon/icon-paths.ts  # icon 線稿資料（portal/creator 為 baked body；workspace 額外吃 stroke prop）
apps/portal-web/src/views/LandingView.vue          # storytelling landing 動效與專屬 CSS
```

## 新增視覺元素時的檢查清單

1. 顏色只用語意 token（`--text` / `--oj-primary` 等），不要硬寫 hex（果醬六色與便條淡彩 `--t-*` 除外，且僅用於強調色/漸層/縮圖底來源）。
2. 邊框寬度用 `var(--bw)`，不要寫死 `1.5px` / `2px` 等數值。
3. Hover 態走「`translateY` 上浮 + `--pop-*-h` 軟投影加深」；**不要**再讓 `--ink-drop` 硬影跟著 hover 生長（v3 已改為靜態底座，見陰影系統一節）。
4. 貼紙傾斜（`rotate`）小角度使用（約 `-6deg` ~ `4deg`），用在 highlight 貼紙、收藏鈕、file icon、polaroid 卡等強調元素，不要套用在大面積版面容器上。
5. 手寫字 `--oj-hand`（Caveat）僅用於輕量註記/免費標籤等裝飾性文字，正式內容（金額精確值、表單、法務文案）不要用。
6. 若元素會出現在三個 app 中的一個以上，記得同步修改對應的 `base.css` 與 `App.vue` theme-overrides（無共用套件，三處各自維護一份）；workspace-web 若涉及圓角/膠囊，記得改用其「收斂一號 + `--r-pill` 圓角矩形」變體而非直接沿用 portal/creator 的真膠囊寫法。
7. 新增表單元件時依 app 對應力度：消費端（portal/creator）走「預設無框、focus 才加陰影」；後台（workspace）走「墨線恆常可見」。
8. 目前無深色模式（token 皆定義在單一 `:root`，無 `.light`/`.dark` scope）；如需重新引入，三個 app 都要從頭設計分層，非既有分岔的延伸。
