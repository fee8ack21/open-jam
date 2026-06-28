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

## 多語系（i18n）

以 **vue-i18n** 提供繁體中文（`zh-TW`，預設）與英文（`en`）。語系切換在頂部導覽列的
地球圖示，選擇後寫入 `localStorage`（key `openjam.workspace.locale`）並更新 `<html lang>`；
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

- **鍵命名**：點號階層（如 `orderModal.orderNumber`）。陣列以數字段表示（如 `foo.bar.0`），
  腳本會還原成 JSON 陣列。
- **插值**：以 `{name}` 命名參數（如 `orders.total` 的 `{count}`、`upload.revFilesValue`
  的 `{count}`／`{size}`）。
- **共用標籤**：訂單／商品／商店狀態等跨頁共用的標籤集中於 `orderStatus`、`catalogStatus`、
  `storeStatus`、`memberStatus`、`statusLabel` 等命名空間；`utils/order.ts`、`utils/format.ts`
  以 `labelKey` + `t(labelKey)` 取用，於 component 解析。
- **非元件取用**：store／工具模組以 `i18n.global.t(...)` 取字串（如各 store 的載入／操作錯誤，
  集中於 `storeError` 命名空間）。
- **資料層**：示範用的營收圖表、月份與分類標籤（`data/products.ts`、`stores/adminStats.ts`）
  屬 mock 內容資料，維持原樣不進 i18n；分類顯示名稱改由後端或 i18n 提供。
- **路由標題**：路由 `meta.titleKey` 指向 `route.*`，頂欄與側欄皆以 `t(titleKey)` 解析。
- **新增語系**：在 xlsx 增加一欄（標題為 locale 代碼），跑 `pnpm gen:i18n` 產生對應 JSON，
  再於 `src/i18n/index.ts` 的 `SUPPORTED_LOCALES` 與 `messages` 註冊。

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
