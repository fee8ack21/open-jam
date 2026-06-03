# Open Jam · 創作者後台 (SPA)

由 HTML 原型遷移而來的單頁應用骨架。

**技術棧**：pnpm + Vite + Vue 3 + Pinia + Vue Router + Naive UI

## 開始開發

```bash
pnpm install
pnpm dev        # 啟動開發伺服器 (http://localhost:5173)
pnpm build      # 打包到 dist/
pnpm preview    # 預覽打包結果
```

## 專案結構

```
src/
  main.js              # 進入點：掛載 Pinia / Router / Naive UI、註冊全域元件
  App.vue              # 應用外殼：側欄、頂欄、抽屜、使用者選單、Tweaks
  router/
    index.js           # 路由表（每個頁面一條 named route）
  stores/
    dashboard.js       # Pinia store：state / getters / actions（含 localStorage 持久化）
  data/
    index.js           # 範例資料（之後可換成 API）
  utils/
    format.js          # 金額 / 時間格式化、狀態標籤
  components/
    JIcon.vue          # 線性 icon（全域註冊為 <j-icon>）
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
  styles/
    june.css           # 基礎設計系統（含 Google Fonts @import）
    dashboard.css      # 後台版面與元件樣式
```

## 遷移備忘

- **路由即真相**：目前由 Vue Router 決定顯示哪個頁面；側欄高亮、頁面標題都來自當前 route 名稱。
- **賣家 / 買家模式**：存在 Pinia (`mode`)，並隨路由所屬群組自動同步。
- **資料層**：`src/data/index.js` 為靜態範例，正式環境改成在 store action 內呼叫 API 即可。
- **Naive UI**：在 `main.js` 以 `app.use(naive)` 全域安裝；如需縮小打包體積可改成按需引入。
