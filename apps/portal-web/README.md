# Open Jam · 創作者數位市集

Vue 3 + Vite + Pinia + Vue Router 單頁應用（SPA）。
由原本的靜態 HTML 原型重構而成，結構對齊真實 SPA 專案，可直接接續開發或遷移。

## 技術棧

| 範疇 | 選用 |
| --- | --- |
| 套件管理 | **pnpm** (`packageManager: pnpm@9`) |
| 建置工具 | **Vite 5** + `@vitejs/plugin-vue` |
| 框架 | **Vue 3**（SFC，`<script>` Options/Composition 混用） |
| 狀態管理 | **Pinia**（`src/stores/shop.js`） |
| 路由 | **Vue Router 4**（`createWebHistory`） |
| UI 元件庫 | **Naive UI**（slider / select / form / input / button…） |

## 開始開發

```bash
pnpm install
pnpm dev        # 啟動 Vite 開發伺服器 (http://localhost:5174)
pnpm build      # 產出 dist/
pnpm preview    # 預覽 build 結果
```

## 多語系（i18n）

以 **vue-i18n** 提供繁體中文（`zh-TW`，預設）與英文（`en`）。語系切換在頂部導覽列的
地球圖示，選擇後寫入 `localStorage`（key `openjam.market.locale`）並更新 `<html lang>`；
首次造訪則依瀏覽器語言推斷。

**翻譯流程：開發者改 Excel，腳本轉成 vue-i18n JSON。** 不手動編輯產生出來的 JSON。

| 檔案 | 角色 |
| --- | --- |
| `i18n.xlsx` | **可編輯來源**（譯者維護）。欄位：`key \| zh-TW \| en \| …`，一列一個鍵 |
| `src/i18n/locales/<locale>.json` | **產生檔**（app 載入用），由腳本從 xlsx 轉出，請勿手改 |
| `src/i18n/index.ts` | vue-i18n 啟動：偵測語系、`setLocale()` |

```bash
pnpm gen:i18n        # ⭐ 日常：i18n.xlsx → src/i18n/locales/*.json
pnpm gen:i18n:xlsx   # 反向：在程式碼直接新增鍵（改 JSON）後，回寫更新 xlsx 供譯者編輯
```

- **鍵命名**：點號階層（如 `market.browse.title`）。陣列以數字段表示（如
  `market.hero.keywords.0`、`legal.terms.sections.2.list.0`），腳本會還原成 JSON 陣列。
- **插值**：以 `{name}` 命名參數（如 `market.browse.count` 的 `{cat}`／`{count}`）。
  需要在文字中嵌入 HTML（粗體、彩色 span）的，模板用 `<i18n-t keypath="…" scope="global">`
  搭配具名 slot。
- **新增鍵**：建議直接在 `src/i18n/locales/*.json` 補齊各語系後，跑 `pnpm gen:i18n:xlsx`
  把鍵同步進 xlsx；之後譯者的修訂再以 `pnpm gen:i18n` 轉回 JSON。兩個方向皆可無損 round-trip。
- **新增語系**：在 xlsx 增加一欄（標題為 locale 代碼），跑 `pnpm gen:i18n` 產生對應 JSON，
  再於 `src/i18n/index.ts` 的 `SUPPORTED_LOCALES` 與 `messages` 註冊。

## 目錄結構

```
.
├── index.html              # Vite 進入點（掛載 #app）
├── vite.config.js          # 別名 @ → /src
├── package.json            # pnpm 相依與指令
├── src/
│   ├── main.js             # 啟動：Pinia · Router · Naive UI · 全域元件
│   ├── App.vue             # 根元件（Naive UI providers + <router-view>）
│   ├── router/index.js     # 路由表
│   ├── stores/shop.js      # Pinia store：篩選 · 購物車 · 收藏 · 訂單 · 主題
│   ├── data/catalogue.js   # 範例商品資料（之後可換成 API）
│   ├── layouts/
│   │   └── ShopLayout.vue  # 商店外殼：導覽列 · 搜尋 · 購物車 · Tweaks 面板
│   ├── views/
│   │   ├── MarketView.vue    # 「/」市場集首頁（探索 hub）
│   │   ├── ListView.vue      # 「/shop」商品列表（篩選 · 排序）
│   │   ├── DetailView.vue    # 「/shop/product/:id」商品詳情
│   │   └── CheckoutView.vue  # 「/shop/checkout」結帳流程
│   ├── components/
│   │   ├── app-icon/         # 線性圖示集（全域註冊 <app-icon>）
│   │   │   ├── AppIcon.vue
│   │   │   ├── icon-paths.ts # SVG path 資料
│   │   │   └── index.ts      # barrel
│   │   ├── Stars.vue         # 星級評分（<stars>）
│   │   ├── ProductThumb.vue  # 漸層佔位縮圖（<product-thumb>）
│   │   ├── ProductCard.vue   # 商店網格卡片（<product-card>）
│   │   └── MktCard.vue       # 市場集卡片（<mkt-card>）
│   └── assets/css/
│       ├── base.css          # 「Creative Studio Pop」設計系統
│       └── market.css        # 市場集 hub 樣式
└── legacy/                  # 重構前的原始靜態原型（保留備查，可刪）
```

## 路由表

| 路徑 | 名稱 | 元件 | 說明 |
| --- | --- | --- | --- |
| `/` | `market` | `MarketView` | 市場集首頁 / 探索 |
| `/shop` | `shop-list` | `ListView` | 商品列表（`ShopLayout` 之下） |
| `/shop/product/:id` | `product` | `DetailView` | 商品詳情 |
| `/shop/checkout` | `checkout` | `CheckoutView` | 結帳 |

深層連結支援查詢參數：`/shop?category=music&tag=爵士&sort=rating&free=1`，
由 `ShopLayout` 於 `onMounted` 寫入 store。

## 遷移到正式 SPA 的注意事項

- **資料層**：`src/data/catalogue.js` 為靜態範例。接 API 時，建議在 store 內新增
  `async fetchProducts()` action，將 `PRODUCTS` 改為由後端載入。
- **狀態持久化**：store 目前以 `localStorage`（key 前綴 `june.`）保存購物車 /
  收藏 / 主題。改用後端帳號時可替換成 API 同步。
- **路由模式**：使用 `createWebHistory`，部署時伺服器需將未知路徑 fallback 至
  `index.html`（SPA history 模式）。
- **金流**：`CheckoutView` 的付款為前端模擬（`setTimeout`），需串接真實金流。
- `legacy/` 僅供參考，正式專案可移除。
```
