# Open Jam — 創作者數位市集

樂譜、攝影集、電子書的數位作品市集前端。Vue 3 + Vite + Pinia + Vue Router + TypeScript，
搭配 [Naive UI](https://www.naiveui.com/) 元件庫。

## 技術棧

| 層 | 套件 |
|----|------|
| 建置 | Vite 5 + `@vitejs/plugin-vue` |
| 語言 | TypeScript（`vue-tsc` 型別檢查） |
| 框架 | Vue 3（Options API SFC） |
| 狀態 | Pinia |
| 路由 | Vue Router 4 |
| UI | Naive UI |
| 套件管理 | pnpm |

## 快速開始

```bash
pnpm install
pnpm dev          # http://localhost:5173
pnpm build        # vue-tsc 型別檢查 + 產出 dist/
pnpm preview      # 預覽 build 結果
pnpm type-check   # 只跑型別檢查
```

## 專案結構

```
.
├── index.html              # Vite 進入點
├── env.d.ts                # vite/client 型別參照
├── tsconfig.json
├── vite.config.ts
├── package.json
└── src/
    ├── main.ts             # createApp → pinia → router → naive → mount
    ├── App.vue             # 根元件：主題 provider、導覽列、<router-view>、Tweaks 面板
    ├── environment.ts      # 由 <meta> 覆蓋的執行期設定
    ├── router/
    │   └── index.ts        # 路由表：/  /product/:id  /checkout
    ├── stores/
    │   └── shop.ts         # Pinia store（篩選 / 購物車 / 收藏 / 訂單），含領域型別
    ├── data/
    │   └── products.ts     # 範例商品目錄與型別（Category / Product / ...）
    ├── components/
    │   ├── icon-paths.ts    # JIcon 的 SVG path 資料
    │   ├── JIcon.vue        # 線條 icon
    │   ├── Stars.vue        # 星等標籤
    │   ├── ProductThumb.vue # 漸層佔位縮圖
    │   ├── ProductCard.vue  # 商品卡（連到詳細頁）
    │   └── AppNav.vue       # 頂部導覽列
    ├── views/
    │   ├── ListView.vue     # 列表頁（route /）
    │   ├── DetailView.vue   # 詳細頁（route /product/:id）
    │   └── CheckoutView.vue # 結帳頁（route /checkout）
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
不再由 store 控制畫面切換。History 模式為 `createWebHistory`。

## TypeScript 慣例

- 各 SFC 維持 Options API，`<script>` 標 `lang="ts"`；需要 prop 型別時以
  `defineComponent` + `PropType<T>` 標註（如 `ProductCard` / `ProductThumb`）。
- 領域型別（`Category` / `Product` / `ProductContent` 等）集中在 `src/data/products.ts`
  並 export；store 的 `CartItem` / `Order` 等型別定義於 `src/stores/shop.ts`。
- 型別檢查走單一 `tsconfig.json`（`extends @vue/tsconfig/tsconfig.dom.json`、
  `noEmit`），不使用 project references。
