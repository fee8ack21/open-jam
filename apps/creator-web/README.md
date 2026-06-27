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

## 多語系（i18n）

以 **vue-i18n** 提供繁體中文（`zh-TW`，預設）與英文（`en`）。語系切換在頂部導覽列的
地球圖示，選擇後寫入 `localStorage`（key `openjam.creator.locale`）並更新 `<html lang>`；
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

- **鍵命名**：點號階層（如 `detail.specFiles`）。陣列以數字段表示（如 `foo.bar.0`），
  腳本會還原成 JSON 陣列。
- **插值**：以 `{name}` 命名參數（如 `checkout.subtotal` 的 `{count}`、`detail.previewLabel`
  的 `{current}`／`{total}`）。需要在文字中嵌入 HTML（粗體、彩色 span）的，模板用
  `<i18n-t keypath="…" scope="global">` 搭配具名 slot。
- **非元件取用**：store／工具模組以 `i18n.global.t(...)` 取字串（如 `stores/shop.ts` 的結帳錯誤）。
- **資料層**：商品標籤 `TAGS` 與示範商品 `PRODUCTS` 屬內容資料（需與後端實際標籤比對），
  維持原樣不進 i18n；分類顯示名稱則改由 `category.<id>` 提供。
- **新增語系**：在 xlsx 增加一欄（標題為 locale 代碼），跑 `pnpm gen:i18n` 產生對應 JSON，
  再於 `src/i18n/index.ts` 的 `SUPPORTED_LOCALES` 與 `messages` 註冊。

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
    │   ├── app-icon/        # 線條 icon 元件
    │   │   ├── AppIcon.vue
    │   │   ├── icon-paths.ts  # SVG path 資料
    │   │   └── index.ts       # barrel
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
