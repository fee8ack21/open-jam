# Open Jam — 創作者數位市集

樂譜、攝影集、電子書的數位作品市集前端。Vue 3 + Vite + Pinia + Vue Router，
搭配 [Naive UI](https://www.naiveui.com/) 元件庫。

## 技術棧

| 層 | 套件 |
|----|------|
| 建置 | Vite 5 + `@vitejs/plugin-vue` |
| 框架 | Vue 3 |
| 狀態 | Pinia |
| 路由 | Vue Router 4 |
| UI | Naive UI |
| 套件管理 | pnpm |

## 快速開始

```bash
pnpm install
pnpm dev        # http://localhost:5173
pnpm build      # 產出 dist/
pnpm preview    # 預覽 build 結果
```

## 專案結構

```
.
├── index.html              # Vite 進入點（正式用）
├── preview.html            # 免建置預覽進入點（見下方說明）
├── vite.config.js
├── package.json
└── src/
    ├── main.js             # createApp → pinia → router → naive → mount
    ├── App.js              # 根元件：主題 provider、導覽列、<router-view>、Tweaks 面板
    ├── router/
    │   └── index.js        # 路由表：/  /product/:id  /checkout
    ├── stores/
    │   └── shop.js         # Pinia store（篩選 / 購物車 / 收藏 / 訂單）
    ├── data/
    │   └── products.js      # 範例商品目錄（CATEGORIES / TAGS / PRODUCTS）
    ├── components/
    │   ├── JIcon.js         # 線條 icon
    │   ├── Stars.js         # 星等標籤
    │   ├── ProductThumb.js  # 漸層佔位縮圖
    │   ├── ProductCard.js   # 商品卡（連到詳細頁）
    │   └── AppNav.js        # 頂部導覽列
    ├── views/
    │   ├── ListView.js      # 列表頁（route /）
    │   ├── DetailView.js    # 詳細頁（route /product/:id）
    │   └── CheckoutView.js  # 結帳頁（route /checkout）
    └── styles/
        └── base.css         # 全域樣式（"Creative Studio Pop" 設計系統）
```

## 路由

| path | name | view |
|------|------|------|
| `/` | `list` | 商品列表 + 篩選 |
| `/product/:id` | `product` | 商品詳細頁 |
| `/checkout` | `checkout` | 購物車 + 結帳 + 完成 |

導覽都透過 `vue-router`（`this.$router.push(...)`、`this.$route.params`），
不再由 store 控制畫面切換。

## 兩種執行方式

### 1. 正式開發（推薦）

```bash
pnpm install && pnpm dev
```

走 `index.html` → `src/main.js`，所有 `import 'vue' / 'pinia' / ...`
由 Vite 從 `node_modules` 解析。

### 2. 免建置預覽（`preview.html`）

`preview.html` 讓專案不需安裝任何依賴、直接用靜態檔案就能在瀏覽器
（或這個工具的預覽窗）中執行。原理：

- 用 `<script>` 從 CDN 載入 Vue / Pinia / Vue Router / Naive UI 的 **全域版**。
- 透過 `<script type="importmap">` 把 bare specifier（`vue` 等）對應到
  `preview/shims/*.js`，這些 shim 只是把全域物件重新 `export`。
- 載入的 **是同一份 `src/` 原始碼**，因此預覽與正式 build 行為一致。

`preview/` 與 `preview.html` 只服務於免建置預覽，正式 build 用不到，
可在真正部署前刪除。

## 遷移到正式 SPA 的後續步驟

這份程式碼已是可直接使用的 Vite 專案。若要再進一步「標準化」：

1. **History 模式**：`src/router/index.js` 目前用 `createWebHashHistory()`
   （URL 帶 `#`），讓靜態檔案也能跑。部署到有 SPA fallback 的伺服器後，
   改成 `createWebHistory(import.meta.env.BASE_URL)`。
2. **元件改寫為 `.vue` 單檔元件（SFC）**：目前元件是「options 物件 +
   `template:` 字串」的 `.js` 模組（這樣才能免建置預覽）。要轉成 SFC，
   把 `template` 字串搬進 `<template>`、其餘搬進 `<script setup>` 或
   `<script>`，並把 `import './X.js'` 改成 `import './X.vue'`。全部轉完後，
   即可移除 `vite.config.js` 裡的 `vue` alias，改用 runtime-only 版本。
3. **CSS import**：可改在 `src/main.js` 內 `import './styles/base.css'`，
   並移除 `index.html` 的 `<link>`。
4. 刪除 `preview.html` 與 `preview/`。
