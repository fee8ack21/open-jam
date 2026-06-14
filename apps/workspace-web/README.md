# workspace-web · 創作者後台 (SPA)

由 HTML 原型遷移而來的單頁應用骨架。

**技術棧**：pnpm + Vite + Vue 3 + TypeScript + Pinia + Vue Router + Naive UI

## 開始開發

```bash
pnpm install
pnpm dev          # 啟動開發伺服器 (http://localhost:5173)
pnpm type-check   # 以 vue-tsc 做型別檢查
pnpm build        # 型別檢查 + 打包到 dist/
pnpm preview      # 預覽打包結果
```

## 專案結構

```
.
index.html             # Vite 進入點（掛載 #app）
vite.config.ts         # 別名 @ → /src
tsconfig.json          # TypeScript 設定（extends @vue/tsconfig）
env.d.ts               # Vite client 與 import.meta.env 型別宣告
package.json           # pnpm 相依與指令
src/
  main.ts              # 進入點：掛載 Pinia / Router / Naive UI、註冊全域元件
  App.vue              # 應用外殼：側欄、頂欄、抽屜、使用者選單、Tweaks
  pinia.d.ts           # Pinia 型別擴充（store.router 注入）
  router/
    index.ts           # 路由表（每個頁面一條 named route）
  stores/
    dashboard.ts       # Pinia store：state / getters / actions（含 localStorage 持久化）
  data/
    index.ts           # 範例資料與領域型別（Product / Order / ... 之後可換成 API）
  utils/
    format.ts          # 金額 / 時間格式化、狀態標籤
  components/
    app-icon/            # 線性 icon（全域註冊為 <app-icon>）
      AppIcon.vue
      icon-paths.ts      # SVG path 資料
      index.ts           # barrel
    ProductThumb.vue   # 作品縮圖（<product-thumb>）
    Stars.vue          # 星等（<stars>）
    TrendChart.vue     # SVG 收入趨勢圖（<trend-chart>）
  views/
    OverviewView.vue   # 儀表板
    ProductsView.vue   # 商品管理
    UploadView.vue     # 上架精靈
    OrdersView.vue     # 訂單管理
    PurchasesView.vue  # 購買紀錄
    WishlistView.vue   # Wishlist
    SettingsView.vue   # 帳號設定
  assets/
    styles/
      base.css         # 基礎設計系統（含 Google Fonts @import）
      workspace.css    # 後台版面與元件樣式
```

## 遷移備忘

- **路由即真相**：目前由 Vue Router 決定顯示哪個頁面；側欄高亮、頁面標題都來自當前 route 名稱。
- **賣家 / 買家模式**：存在 Pinia (`mode`)，並隨路由所屬群組自動同步。
- **資料層**：`src/data/index.ts` 為靜態範例（含領域型別），正式環境改成在 store action 內呼叫 API 即可。
- **Naive UI**：在 `main.ts` 以 `app.use(naive)` 全域安裝；如需縮小打包體積可改成按需引入。
- **API client**：`src/api/store-service.ts` 由 `pnpm gen:api` 從後端 OpenAPI 產生（TS），勿手改。後端 enum 已以 `JsonStringEnumConverter` 序列化為字串，codegen 產出具名字串 enum。
