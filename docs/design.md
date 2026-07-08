# 視覺設計準則

Open Jam 三個前端（portal-web / creator-web / workspace-web）共用同一套視覺系統 **「Creative Studio Pop」**：糖果色系（violet/pink/orange/lime/cyan/yellow）+ 高對比展示字體 + 硬邊色塊陰影（hard shadow），刻意帶玩味與手作感，區別於一般 SaaS 後台的扁平風格。三個 app 各自的 `assets/styles/base.css` 內容幾乎一致（同一份設計 token 各自維護一份，非共用套件），修改視覺系統時三處需同步調整。

## 色彩

### 品牌色板（`:root`）

```css
--c-violet: #6c4cf1;   /* 主色 */
--c-pink:   #ff4d9d;
--c-orange: #ff7a2f;
--c-lime:   #aef03e;
--c-cyan:   #1fd6c6;
--c-yellow: #ffc83a;
```

六色為扁平飽和色，不做漸層混色使用（漸層時固定用 `linear-gradient(135deg, var(--c-violet), var(--c-pink))` 這組主打組合，用於 logo mark、CTA、卡片縮圖底色）。`--oj-primary` / `--oj-primary-soft`（或 `--oj-soft`，portal-web 命名不一致）/ `--oj-primary-wash` 是語意化別名，指向 violet，元件一律用語意變數不要直接寫 `--c-violet`。

### 語意色 token（依 light / dark scope）

`.oj-root`（或 `.oj-root.light` / `.oj-root.dark`，見下）定義：

```css
--bg / --surface / --surface-2      /* 背景、卡片面、次要面 */
--text / --text-soft / --text-faint /* 文字三階層 */
--border / --border-strong           /* 邊框：一般 1.5px 淡邊；strong 用於卡片重點外框與 hard shadow 顏色來源 */
--shadow / --shadow-hover             /* 卡片預設陰影（見下方陰影系統） */
```

**目前狀態**：workspace-web 的 `base.css` 已定義完整 `.oj-root.light` / `.oj-root.dark` 兩組 token 與對應的背景放射漸層，但 `App.vue` 的 `rootClass` 目前寫死 `'light'`（尚未接出使用者可切換的深色模式開關）。portal-web / creator-web 尚未拆 light/dark scope，只有單一 `.oj-root` token 組（等同 light）。新增深色模式需三個 app 一併補齊。

### Naive UI 主題覆寫

各 app `App.vue` 以 `n-config-provider :theme-overrides` 覆寫 Naive UI 預設主題，三處各自維護、**主色數值不完全一致**（歷史遺留，非刻意分流）：

| App | primaryColor | 備註 |
|---|---|---|
| portal-web | `#6151f0` | |
| creator-web | `#6c4cf1` | 與 `--c-violet` 一致 |
| workspace-web | `#5639d6` | 另指定 `:theme="null"` 停用 Naive UI 內建深色主題切換，改完全交給 `.oj-root` CSS 變數控制 |

新增/調整品牌主色時，`--c-violet`（CSS token）與各 App.vue 的 `primaryColor`（Naive UI token）要一起改，兩套系統不會互相同步。

## 字體

```css
--oj-display: 'Bricolage Grotesque', 'Noto Sans TC', sans-serif;  /* 標題 / 展示字 */
--oj-font:    'Space Grotesk', 'Noto Sans TC', sans-serif;        /* 內文 / UI */
--oj-mono:    'JetBrains Mono', ui-monospace, monospace;          /* 標籤、eyebrow、數字、代碼感元素 */
```

- 展示字體有第二套可切換選項：加 `.font-grotesk` class 於祖先元素，`--oj-display` 會切成 `'Unbounded'`（更幾何、更粗獷），workspace-web 的個人化設定（`store.font`）即透過此機制切換標題字體觀感。
- 中文一律吃 `Noto Sans TC`，三套字體 stack 都以它作為 fallback，不要在中英混排時另外指定中文字體。
- 標題常見手法：`letter-spacing` 用負值收緊（`-0.3px` ~ `-2px`，字級越大負值越大），`font-weight: 700/800`。
- Eyebrow / 分類標籤 / 檔案 meta 一律用 `--oj-mono` + `uppercase` + `letter-spacing: .1em~.14em`，是這套視覺語言中「工藝感」的主要來源，不要用內文字體代替。

## 間距與圓角

```css
--nav-h: 72px;
--r-lg: 22px;  /* 卡片、面板、大型容器 */
--r-md: 16px;  /* 縮圖、輸入框、次級容器 */
--r-sm: 11px;  /* icon 按鈕、logo mark */
--maxw: 1240px; /* 內容最大寬度（workspace-web 有定義，portal/creator 於各版面自訂） */
```

圓角越大用在越「主要」的容器；不要混用非 token 的隨手數值（如 `18px`）除非該元件有特殊比例需求。

## 陰影系統（Pop Hard Shadow）

這是整套視覺系統最具識別度的部分：不用模糊陰影表現層級，而是用**硬邊位移陰影**（無 blur，offset 對齊 border 顏色）模擬紙感貼合。

```css
--pop-1: 2px 2px 0 var(--text);   --pop-1-h: 4px 4px 0 var(--text);
--pop-2: 3px 3px 0 var(--text);   --pop-2-h: 5px 5px 0 var(--text);
--pop-3: 4px 4px 0 var(--text);   --pop-3-h: 6px 6px 0 var(--text);
```

規則：**resting → hover 一律 +2px** 朝陰影方向生長，同時元件本身 `translate(-2px, -2px)`，讓陰影視覺上「補滿」位移出來的空間（例：`.cat-pill:hover` / `.cart-badge` 所在的按鈕、`.card:hover`）。active/按下狀態則反向 `translate(1px, 1px)` 並縮回 `1px 1px 0`，模擬按壓貼底。

搭配一般柔和陰影（卡片預設 `--shadow` / hover `--shadow-hover`，用 `color-mix` 混入 accent 色做輝光）作為次要層級，两套陰影語言不要混用在同一元件的同一狀態。

## 元件慣例

- **卡片**（`.card`）：`border: 1.5px solid var(--border)`，hover 時 `translateY(-7px) rotate(-.5deg)` + 邊框轉 `--border-strong` + 陰影升級。輕微旋轉（`-0.5deg` ~ `1.5deg`）是這套風格刻意營造「非機械感」的手法，多處重複出現（`.hl` highlight 底色塊、`.card:hover`）。
- **CTA 按鈕**（`.cta-pop`）：漸層底（yellow→orange）+ 深色文字 + 1.5px 深色邊框 + hard shadow，是全站最高強調層級，一頁不宜出現太多個。
- **Pill / Chip**（`.cat-pill` / `.tag-toggle` / `.chip`）：膠囊形、每個分類有專屬強調色（music=violet、photo=pink、ebook=cyan），選中態才上色，未選中為描邊/淡底。
- **Naive UI 表單元件**：全域覆寫 `.n-input` / `.n-base-selection` 使其融入 hard-shadow 語言（focus 時陰影變 violet 且位移，錯誤態陰影變 pink），見各 `base.css` 底部「pop-style form controls」區塊。新增自訂表單元件時要延續這個 focus/error 視覺，不要用 Naive UI 預設的 box-shadow ring。
- **Icon**：三個 app 各自維護 `src/components/app-icon/icon-paths.ts`，統一以單一 24×24 viewBox 的線性 path data 定義（`stroke`，非 fill icon），透過 `<app-icon name="...">` 使用。新增 icon 時比照現有 path 的筆畫粗細與風格手繪／複製，不要混入不同來源的 icon set（如直接貼 Material/Feather 的 fill 版本）。

## 動效

- **互動 hover**：位移 + 陰影生長（見上）是預設語言，`transition` 統一走 `.14s`~`.22s`，多用 `cubic-bezier(.2,.8,.2,1)` 表現彈性但不誇張的回彈。
- **Landing storytelling（portal-web `/`，`LandingView.vue`）**：用 GSAP + ScrollTrigger 做長捲動敘事頁，非一般頁面動效，獨立記錄如下：
  - 全頁疊加 `feTurbulence` SVG 顆粒紋理（`.l-grain`，fixed 滿版）與細格線，營造印刷/工作室質感，呼應 [Begonia Design](https://begonia-design.com.tw) 的參考手法。
  - 各段落標題搭配巨型出血大字（`-webkit-text-stroke` 描邊、無填色或極淡填色，字級 `clamp(90px, 14~15vw, 210~230px)`），置於段落背景層，隨捲動水平飄移。
  - 章節式 pin：`.ls-stage` 預設一般直排 CSS 版面；僅在使用者未設定 `prefers-reduced-motion: reduce` 時，JS 才加上 `.ls-anim`，讓子章節改為絕對定位堆疊由 ScrollTrigger 接管，**這是可及性 fallback 機制，不是漸進增強的裝飾，改動時務必保留這個分岔**。
  - 品牌跑馬燈（`.tag-marquee` / `.mq-track`）：純 CSS `animation` 無限捲動，`hover` 暫停。
  - 已知踩坑（詳見 `[[project-landing-page]]` 記憶，改動 landing 前先讀）：
    1. pin 區塊不可用父容器負 margin 做滿版，ScrollTrigger 量測寬度會偏，改用各區塊自帶 side padding。
    2. ScrollTrigger `snap` 預設 `directional: true` + 慣性投影，快速捲動或點 rail 導覽會被甩到頭尾章，需顯式關閉 `directional` / `inertia`。
    3. grid 容器若未定義 `grid-template`，auto track 會被原始尺寸大的子元素（如未壓縮的 img）撐開、把手足推出視窗，視覺容器要改 `flex column` + `min-width: 0`。

## 檔案位置

```
apps/<app>/src/assets/styles/base.css   # 設計 token、全域元件 class（三份需同步維護）
apps/<app>/src/assets/styles/market.css | list.css | workspace.css  # 各 app 專屬版面（沿用同一套 token）
apps/<app>/src/App.vue                  # Naive UI theme-overrides
apps/<app>/src/components/app-icon/icon-paths.ts  # icon 線稿資料
apps/portal-web/src/views/LandingView.vue          # storytelling landing 動效與專屬 CSS
```

## 新增視覺元素時的檢查清單

1. 顏色只用語意 token（`--text` / `--oj-primary` 等），不要硬寫 hex（品牌六色除外，且僅用於漸層/強調色來源）。
2. Hover 態要有位移 + 陰影生長（hard shadow 元件）或柔和陰影升級（一般卡片），不要只變色。
3. 展示文字用 `--oj-display`，標籤/mono 感文字用 `--oj-mono` + uppercase + letter-spacing，內文才用 `--oj-font`。
4. 若元素會出現在三個 app 中的一個以上，記得同步修改對應的 `base.css` 與 `App.vue` theme-overrides（目前無共用套件，三處各自維護一份）。
5. 深色模式目前只有 workspace-web 定義了 token 但未接 UI 開關；新增元件時仍應寫 `.oj-root.dark` 對應樣式，避免日後開放切換時漏掉。
