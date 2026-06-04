# 開發慣例 Develop

本頁是 Open Jam 的開發規範：架構慣例、專案結構，以及前後端開發注意事項與 coding style。

## 架構慣例

- **CQRS / Mediator**。
- **軟體三層 + Service DI**。

## 專案結構

```
src/
  Auth/             # 認證授權（OIDC / Hydra）
  EmailService/     # 寄信服務
  StorageService/   # 檔案儲存 / 轉碼 / 預覽
  LogService/       # Audit Log / 日誌
  ProductService/   # 商店與商品上下架
  OrderService/     # 訂單與金流
  QuotaService/     # 資源配額計量
  Bootstrap/        # 平台初始化 seed
  Shared/           # 共用程式庫
apps/
  workspace-web/    # 用戶後台
  creator-web/      # 創作者商品空間
  market-web/       # 平台首頁，探索各產品與創作者
scripts/
  publish/
infra/
  docker/
  helm/
docs/
```

## 後端注意事項

- **列表 API 分頁採 `offset` / `limit`**（非 `page` / `rowsPerPage`）。
- **微服務資源參照（Ref Table）**：本地保留其他服務資源的參照表，避免每次跨服務查詢。
- **資源更新發 Event**：資源變更時發事件，讓持有 Ref Table 的服務同步。
- **常用資源查詢快取**（Redis）。
- **LogService 單純 index**：以索引支撐查詢，資料量大再評估分表分庫（見 [[Log]]）。
- **decimal 數值計算**：金額計算避免溢位。
- **後端多國語系**：採 .resx 資源檔。

## 前端注意事項

- **API promise singleton**：同一請求進行中時共用同一個 promise，避免重複發送；搭配 with-pending 狀態機制。
- **lodash debounce**：搜尋 / 輸入等場景去抖。
- **前端多國語系**：i18n。

## API Model 慣例（CRUD Model）

標準 request / response DTO 範例（分頁採 offset / limit）：

```csharp
public class GetUsersRequest
{
    public int Offset { get; set; } = 0;
    public int Limit { get; set; } = 20;
    public string? Keyword { get; set; }
    public string? Status { get; set; }
}

public class CreateUserRequest
{
    public string Name { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }
}

public class UpdateUserRequest
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string? Status { get; set; }
}

public class GetUsersResponse
{
    public int TotalCount { get; set; }
    public List<UserListItemDto> Items { get; set; }
}

public class GetUserResponse
{
    public UserDetailDto User { get; set; }
}

public class BulkUpdateUserRequest
{
    public List<UpdateUserItem> Items { get; set; }
}

public class UpdateUserItem
{
    public Guid Id { get; set; }
    public string? Status { get; set; }
}
```
