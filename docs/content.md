# 平台內容 Content

ContentService 負責**平台自身內容**的管理：法律文件（服務條款 / 隱私權政策）的版本生命週期，與常見問題（FAQ）的主題分類及問答項目。法律文件原屬 [[Auth]]，已抽離至本服務；**使用者的同意紀錄（`UserLegalConsent`）仍留在 Auth**，本服務不涉及。

本頁聚焦內容管理；註冊 / 登入時的條款同意流程見 [[Auth]]、Admin 角色授權見 [[Develop]]，不在本頁範圍。

## 範圍與前提（MVP）

- **內容型態為純文字慣例**：法律文件內容以「`## `」開頭為章節標題、「`- `」開頭為列點（Auth 註冊 dialog 與 portal-web 條款頁共用此渲染慣例）；FAQ 解答為純文字可含換行。不引入富文本編輯器。
- **無事件訂閱**：純 REST API 服務，僅經 Outbox 發 `AuditLogRequestedEvent`（[[Log]]）。
- 初始資料由 [[Bootstrap]] seed：`LegalDocumentSeeder`（兩類文件各一筆啟用版本，該類型已有紀錄即略過）、`FaqSeeder`（分類以 slug 冪等 upsert、問答於已有資料時略過）。

## 資料模型

- **法律文件（`legal_document`）**：`Id`、`Type`（`TermsOfService` / `PrivacyPolicy`）、`Version`（同類型內遞增且唯一）、`Title`、`Content`、`Highlights`（**重點速覽**：純文字每行一則「標題|描述」，portal-web 條款頁頂部摘要卡優先取用、留空退回 i18n 靜態文案；Auth 同意流程僅渲染正文不用速覽）、`Status`（`Draft` / `Active` / `Inactive`）、`ActivatedAt`（最近一次啟用；null 表從未啟用）。
  - **每次修訂為一筆新紀錄**，版本序號由伺服器依同類型現有最大版本 +1 產生；**僅 `Draft` 可刪除**（軟刪除，`IDeletedAt`），**啟用過的版本永不刪除**，供 Auth 同意紀錄與歷史比對。版本號以 `IgnoreQueryFilters()` 含已刪草稿計算，避免撞 `(Type, Version)` 唯一索引。
  - **partial unique index 保證同類型僅一筆 `Active`**。
- **FAQ 主題分類（`faq_category`）**：`Id`、`Name`、`Slug`（全域唯一，小寫英數字與連字號）、`Description`、`SortOrder`（分頁顯示排序）。單層分類（比照 [[Catalog]] 商品分類但無 `ParentId`），取代原本寫死的 `FaqCategory` enum。
- **FAQ 項目（`faq_item`）**：`Id`、`CategoryId`（FK → `faq_category`，`OnDelete Restrict`）、`Question`、`Answer`、`SortOrder`（同分類內排序）、`IsPublished`（發布旗標，僅 true 對外公開）。DTO 另帶 `CategoryName` / `CategorySlug` 供前端顯示。

## 法律文件生命週期

```
建草稿（POST）──▶ Draft ──activate──▶ Active ──deactivate──▶ Inactive
                  │  ▲                    │
                  └──┘僅 Draft 可編輯（PUT）└─ 同類型啟用新版本時自動轉 Inactive
                     與刪除（DELETE，軟刪除）
```

- **僅 `Draft` 可編輯與刪除**：對 `Active` / `Inactive` 版本呼叫 PUT / DELETE 回 409——已啟用過的版本為歷史紀錄，內容凍結、永不刪除。
- **啟用（activate）**：同一交易內將該類型既有 `Active` 文件轉為 `Inactive`，再啟用目標文件並填 `ActivatedAt`。
- **停用（deactivate）**：僅 `Active` 可停用（否則 409）。停用後該類型暫無啟用版本，Auth 註冊流程屬 fail-closed（取不到啟用文件則中止註冊），操作前需知悉影響。
- **上架新啟用版本的下游效果**：使用者對新版本無同意紀錄，下次登入被 [[Auth]] 導向 re-consent 頁重新同意。

## 常見問題（FAQ）

- **公開讀取**：分類列表與已發布問答皆為匿名端點，portal-web FAQ 頁以分類 `SortOrder` 建主題分頁、項目依分類 `SortOrder` + 項目 `SortOrder` 排序呈現；取代 portal-web 原前端寫死的 FAQ 內容。
- **Admin 管理**：分類與問答皆可 CRUD（workspace-web「常見問題分類」/「常見問題」後台）。
  - 分類 `Slug` 重複回 422；刪除分類時若仍被問答引用回 409（FK `Restrict`）。
  - 問答建立 / 更新時驗證 `CategoryId` 存在（不存在回 422）；未發布（`IsPublished = false`）項目僅 Admin 列表可見。

## REST API 契約

遵循 [[Develop]] 三層 + Service DI、`offset`/`limit` 分頁、FluentValidation（422，欄位 `errors`）、AutoMapper、`ExceptionMiddleware` 轉 RFC 9457 Problem Details、`AddOpenJamJwtAuth` 驗 Hydra JWT。開發 URL 5181，Swagger 於 `/swagger`。

### 法律文件 `/v1/legal-documents`

| Method | Path | 用途 | 授權 | 失敗碼 |
|--------|------|------|------|--------|
| `GET`  | `/v1/legal-documents` | 分頁列表（可過濾 `type` / `status`；摘要不含內容） | Admin | — |
| `GET`  | `/v1/legal-documents/active?type=` | 撈啟用版本（含內容；`type` 省略回兩類） | 匿名 | — |
| `GET`  | `/v1/legal-documents/{id}` | 單筆（含內容） | Admin | 404 |
| `POST` | `/v1/legal-documents` | 建草稿（版本序號伺服器產生） | Admin | 422 |
| `PUT`  | `/v1/legal-documents/{id}` | 改草稿（標題 / 內容） | Admin | 404 / 409 非草稿 |
| `POST` | `/v1/legal-documents/{id}/activate` | 啟用（自動停用既有同類型 Active） | Admin | 404 |
| `POST` | `/v1/legal-documents/{id}/deactivate` | 停用 | Admin | 404 / 409 非啟用中 |
| `DELETE` | `/v1/legal-documents/{id}` | 刪草稿（軟刪除） | Admin | 404 / 409 非草稿 |

### 常見問題 `/v1/faq-categories`、`/v1/faqs`

| Method | Path | 用途 | 授權 | 失敗碼 |
|--------|------|------|------|--------|
| `GET`    | `/v1/faq-categories` | 分類列表（依 `SortOrder`） | 匿名 | — |
| `GET`    | `/v1/faq-categories/{id}` | 單筆分類 | Admin | 404 |
| `POST`   | `/v1/faq-categories` | 建分類 | Admin | 422 slug 重複 |
| `PATCH`  | `/v1/faq-categories/{id}` | 改分類 | Admin | 404 / 422 slug 重複 |
| `DELETE` | `/v1/faq-categories/{id}` | 刪分類 | Admin | 404 / 409 仍被引用 |
| `GET`    | `/v1/faqs` | 分頁列表（可過濾 `categoryId` / `isPublished`） | Admin | — |
| `GET`    | `/v1/faqs/published?categoryId=` | 已發布問答（portal-web FAQ 頁） | 匿名 | — |
| `GET`    | `/v1/faqs/{id}` | 單筆問答 | Admin | 404 |
| `POST`   | `/v1/faqs` | 建問答 | Admin | 422 分類不存在 |
| `PUT`    | `/v1/faqs/{id}` | 改問答 | Admin | 404 / 422 分類不存在 |
| `DELETE` | `/v1/faqs/{id}` | 刪問答 | Admin | 404 |

### 稽核事件

所有寫入操作經 Outbox 發 `AuditLogRequestedEvent`：`legal.create` / `legal.update` / `legal.activate` / `legal.deactivate` / `legal.delete`、`faq.category.create` / `.update` / `.delete`、`faq.create` / `faq.update` / `faq.delete`。

## 對其他服務的相依

1. **Auth**（呼叫方）：`ContentServiceClient`（named `"content"`）呼叫 `GET /v1/legal-documents/active` 取啟用版本，供註冊同意 dialog 渲染與登入 re-consent 比對；同意紀錄以 `LegalDocumentId` 跨服務參照（無外鍵）。ContentService 不可用時註冊 fail-closed 中止。
2. **portal-web**：條款頁（`/v1/legal-documents/active`）與 FAQ 頁（`/v1/faq-categories`、`/v1/faqs/published`）皆走匿名端點。
3. **workspace-web**：管理員「條款管理」「常見問題分類」「常見問題」後台走 Admin 端點。
4. **Bootstrap**：`LegalDocumentSeeder` / `FaqSeeder` 直接寫入本服務 DB（`ContentConnection` 具名連線字串）。

## 技術與架構

- .NET 微服務（`src/ContentService/`），整體結構慣例見 [[Develop]]，可參考 QuotaService（同為純 REST API、無事件訂閱）。
- 資料庫 `open_jam_content`（snake_case、`BaseDbContext` audit 慣例；法律文件草稿刪除為軟刪除（`IDeletedAt`）、啟用過版本永不刪除，FAQ 為硬刪除）。
- 跨服務關聯：同意流程 [[Auth]]、稽核 [[Log]]、初始資料 [[Bootstrap]]。
