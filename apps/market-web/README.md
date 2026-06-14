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
pnpm dev        # 啟動 Vite 開發伺服器 (http://localhost:5173)
pnpm build      # 產出 dist/
pnpm preview    # 預覽 build 結果
```

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
