/* eslint-disable */
/* tslint:disable */
// @ts-nocheck
/*
 * ---------------------------------------------------------------
 * ## THIS FILE WAS GENERATED VIA SWAGGER-TYPESCRIPT-API        ##
 * ##                                                           ##
 * ## AUTHOR: acacode                                           ##
 * ## SOURCE: https://github.com/acacode/swagger-typescript-api ##
 * ---------------------------------------------------------------
 */

/** 商品列表排序方式。 */
export enum CatalogSort {
  Newest = "Newest",
  PriceLowToHigh = "PriceLowToHigh",
  PriceHighToLow = "PriceHighToLow",
}

/** 商品狀態。 */
export enum CatalogStatus {
  Draft = "Draft",
  Published = "Published",
  Archived = "Archived",
  Suspended = "Suspended",
}

/** 商品展示型資產類型。 */
export enum CatalogAssetType {
  Thumbnail = "Thumbnail",
  Screenshot = "Screenshot",
  PreviewAudio = "PreviewAudio",
  PreviewVideo = "PreviewVideo",
}

/** 展示型資產回應。 */
export interface CatalogAssetDto {
  /**
   * 資產唯一識別碼。
   * @format uuid
   * @example "3fa85f64-5717-4562-b3fc-2c963f66afa6"
   */
  id?: string;
  /** 商品展示型資產類型。 */
  type?: CatalogAssetType;
  /**
   * 原始檔名。
   * @example "screenshot-1.png"
   */
  fileName?: string | null;
  /**
   * MIME 類型。
   * @example "image/png"
   */
  contentType?: string | null;
  /**
   * 檔案大小（bytes）。
   * @format int64
   * @example 204800
   */
  fileSize?: number;
  /**
   * 同類型內顯示排序。
   * @format int32
   * @example 0
   */
  sortOrder?: number;
  /**
   * 公開讀取 URL。
   * @example "http://localhost:5171/v1/files/blob/public/.../screenshot-1.png"
   */
  url?: string | null;
  /**
   * 建立時間。
   * @format date-time
   */
  createdAt?: string;
}

/** 展示型資產上傳簽章 URL 回應。 */
export interface CatalogAssetUploadUrlResponse {
  /**
   * 已建立的 Asset ID。
   * @format uuid
   * @example "3fa85f64-5717-4562-b3fc-2c963f66afa6"
   */
  assetId?: string;
  /**
   * 前端應使用此 URL 以 HTTP PUT 直傳檔案。
   * @example "http://localhost:5171/v1/files/blob/public/.../screenshot-1.png?expires=1735689600&sig=..."
   */
  uploadUrl?: string | null;
  /**
   * 上傳完成後的公開讀取網址。
   * @example "http://localhost:5171/v1/files/blob/public/.../screenshot-1.png"
   */
  publicUrl?: string | null;
  /**
   * 簽章 URL 過期時間（UTC）。
   * @format date-time
   */
  expiresAt?: string;
}

/** 商品分類回應。 */
export interface CatalogCategoryDto {
  /**
   * 分類唯一識別碼。
   * @format uuid
   * @example "3fa85f64-5717-4562-b3fc-2c963f66afa6"
   */
  id?: string;
  /**
   * 上層分類 ID；null 表示頂層分類。
   * @format uuid
   * @example "3fa85f64-5717-4562-b3fc-2c963f66afa6"
   */
  parentId?: string | null;
  /**
   * 分類名稱。
   * @example "音樂與音效"
   */
  name?: string | null;
  /**
   * 分類代稱（全域唯一）。
   * @example "audio"
   */
  slug?: string | null;
  /**
   * 分類補充敘述；null 表示未設定。
   * @example "樂譜、配樂、分軌音檔"
   */
  description?: string | null;
  /**
   * 同層顯示排序。
   * @format int32
   * @example 0
   */
  sortOrder?: number;
}

/** 商品完整資訊回應（商品詳情頁）。 */
export interface CatalogDto {
  /**
   * 商品唯一識別碼。
   * @format uuid
   * @example "3fa85f64-5717-4562-b3fc-2c963f66afa6"
   */
  id?: string;
  /**
   * 所屬商店 ID。
   * @format uuid
   * @example "3fa85f64-5717-4562-b3fc-2c963f66afa6"
   */
  storeId?: string;
  /**
   * 所屬分類 ID；null 表示未分類。
   * @format uuid
   * @example "3fa85f64-5717-4562-b3fc-2c963f66afa6"
   */
  categoryId?: string | null;
  /**
   * 商品名稱。
   * @example "像素風格音效包"
   */
  name?: string | null;
  /**
   * 商品代稱（同商店內唯一）。
   * @example "pixel-sfx-pack"
   */
  slug?: string | null;
  /**
   * 商品描述。
   * @example "200 個復古像素遊戲音效，WAV 格式。"
   */
  description?: string | null;
  /**
   * 一句話簡介（市集卡片短標語）；null 表示未設定。
   * @example "復古像素遊戲必備的 8-bit 音效合輯。"
   */
  summary?: string | null;
  /**
   * 封面色相（0–359），無縮圖時生成漸層佔位封面。
   * @format int32
   * @example 256
   */
  coverHue?: number;
  /** 商品狀態。 */
  status?: CatalogStatus;
  /**
   * 售價。
   * @format double
   * @example 150
   */
  price?: number;
  /**
   * 幣別（ISO 4217）。
   * @example "TWD"
   */
  currency?: string | null;
  /**
   * 累計銷量。
   * @format int64
   * @example 0
   */
  salesCount?: number;
  /**
   * 商品詳情頁累計瀏覽次數。
   * @format int64
   * @example 0
   */
  viewCount?: number;
  /**
   * 是否為編輯精選（平台策展）。
   * @example false
   */
  isFeatured?: boolean;
  /**
   * 平均評分（0–5）；無評論時為 0。
   * @format double
   * @example 4.6
   */
  ratingAverage?: number;
  /**
   * 評論數。
   * @format int32
   * @example 128
   */
  ratingCount?: number;
  /**
   * 縮圖公開 URL；null 表示尚未設定。
   * @example "http://localhost:5171/v1/files/blob/public/.../thumb.png"
   */
  thumbnailUrl?: string | null;
  /** 商品版本回應。 */
  currentVersion?: CatalogVersionDto;
  /** 展示型資產（縮圖 / 截圖 / 預覽影音）清單。 */
  assets?: CatalogAssetDto[] | null;
  /**
   * 標籤名稱清單。
   * @example ["audio","retro","8bit"]
   */
  tags?: string[] | null;
  /**
   * 首次上架時間；null 表示尚未上架。
   * @format date-time
   */
  publishedAt?: string | null;
  /**
   * 建立時間。
   * @format date-time
   */
  createdAt?: string;
  /**
   * 最後更新時間。
   * @format date-time
   */
  updatedAt?: string | null;
}

/** 商品列表項目（精簡欄位）。 */
export interface CatalogSummaryDto {
  /**
   * 商品唯一識別碼。
   * @format uuid
   * @example "3fa85f64-5717-4562-b3fc-2c963f66afa6"
   */
  id?: string;
  /**
   * 所屬商店 ID。
   * @format uuid
   * @example "3fa85f64-5717-4562-b3fc-2c963f66afa6"
   */
  storeId?: string;
  /**
   * 所屬分類 ID；null 表示未分類。
   * @format uuid
   * @example "3fa85f64-5717-4562-b3fc-2c963f66afa6"
   */
  categoryId?: string | null;
  /**
   * 商品名稱。
   * @example "像素風格音效包"
   */
  name?: string | null;
  /**
   * 商品代稱。
   * @example "pixel-sfx-pack"
   */
  slug?: string | null;
  /**
   * 一句話簡介（市集卡片短標語）；null 表示未設定。
   * @example "復古像素遊戲必備的 8-bit 音效合輯。"
   */
  summary?: string | null;
  /**
   * 封面色相（0–359），無縮圖時生成漸層佔位封面。
   * @format int32
   * @example 256
   */
  coverHue?: number;
  /**
   * 售價。
   * @format double
   * @example 150
   */
  price?: number;
  /**
   * 幣別（ISO 4217）。
   * @example "TWD"
   */
  currency?: string | null;
  /** 商品狀態。 */
  status?: CatalogStatus;
  /**
   * 累計銷量。
   * @format int64
   * @example 0
   */
  salesCount?: number;
  /**
   * 商品詳情頁累計瀏覽次數。
   * @format int64
   * @example 0
   */
  viewCount?: number;
  /**
   * 是否為編輯精選（平台策展）。
   * @example false
   */
  isFeatured?: boolean;
  /**
   * 平均評分（0–5）；無評論時為 0。
   * @format double
   * @example 4.6
   */
  ratingAverage?: number;
  /**
   * 評論數。
   * @format int32
   * @example 128
   */
  ratingCount?: number;
  /**
   * 縮圖公開 URL；null 表示尚未設定。
   * @example "http://localhost:5171/v1/files/blob/public/.../thumb.png"
   */
  thumbnailUrl?: string | null;
  /**
   * 標籤名稱清單（市集卡片標籤、標籤搜尋用）。
   * @example ["audio","retro","8bit"]
   */
  tags?: string[] | null;
  /**
   * 首次上架時間；null 表示尚未上架。
   * @format date-time
   */
  publishedAt?: string | null;
}

/** 商品標籤回應。 */
export interface CatalogTagDto {
  /**
   * 標籤唯一識別碼。
   * @format uuid
   * @example "3fa85f64-5717-4562-b3fc-2c963f66afa6"
   */
  id?: string;
  /**
   * 標籤名稱（小寫）。
   * @example "retro"
   */
  name?: string | null;
  /**
   * 被商品引用的次數。
   * @format int32
   * @example 42
   */
  usageCount?: number;
}

/** 商品版本可下載檔案回應。 */
export interface CatalogVersionAssetDto {
  /**
   * 資產唯一識別碼。
   * @format uuid
   * @example "3fa85f64-5717-4562-b3fc-2c963f66afa6"
   */
  id?: string;
  /**
   * 原始檔名。
   * @example "pixel-sfx-pack-v1.zip"
   */
  fileName?: string | null;
  /**
   * MIME 類型。
   * @example "application/pdf"
   */
  contentType?: string | null;
  /**
   * 檔案大小（bytes）。
   * @format int64
   * @example 10485760
   */
  fileSize?: number;
  /**
   * 同版本內顯示排序。
   * @format int32
   * @example 0
   */
  sortOrder?: number;
  /**
   * 建立時間。
   * @format date-time
   */
  createdAt?: string;
}

/** 商品版本回應。 */
export interface CatalogVersionDto {
  /**
   * 版本唯一識別碼。
   * @format uuid
   * @example "3fa85f64-5717-4562-b3fc-2c963f66afa6"
   */
  id?: string;
  /**
   * 所屬商品 ID。
   * @format uuid
   * @example "3fa85f64-5717-4562-b3fc-2c963f66afa6"
   */
  catalogId?: string;
  /**
   * 版本字串。
   * @example "1.0.0"
   */
  version?: string | null;
  /**
   * 版本說明 / 更新紀錄。
   * @example "首次發行。"
   */
  releaseNote?: string | null;
  /** 本版本的可下載檔案清單。 */
  assets?: CatalogVersionAssetDto[] | null;
  /**
   * 建立時間。
   * @format date-time
   */
  createdAt?: string;
}

/** 建立商品分類請求（平台維護）。 */
export interface CreateCatalogCategoryRequest {
  /**
   * 上層分類 ID；null 表示頂層分類。
   * @format uuid
   * @example "3fa85f64-5717-4562-b3fc-2c963f66afa6"
   */
  parentId?: string | null;
  /**
   * 分類名稱（1–100 字）。
   * @example "音樂與音效"
   */
  name?: string | null;
  /**
   * 分類代稱（全域唯一，3–100 字小寫英數字與連字號）。
   * @example "audio"
   */
  slug?: string | null;
  /**
   * 分類補充敘述（選填，最多 200 字）。
   * @example "樂譜、配樂、分軌音檔"
   */
  description?: string | null;
  /**
   * 同層顯示排序。
   * @format int32
   * @example 0
   */
  sortOrder?: number;
}

/** 建立商品請求。 */
export interface CreateCatalogRequest {
  /**
   * 所屬商店 ID。
   * @format uuid
   * @example "3fa85f64-5717-4562-b3fc-2c963f66afa6"
   */
  storeId?: string;
  /**
   * 商品名稱（1–200 字）。
   * @example "像素風格音效包"
   */
  name?: string | null;
  /**
   * 商品代稱（同商店內唯一，3–100 字小寫英數字與連字號）。
   * @example "pixel-sfx-pack"
   */
  slug?: string | null;
  /**
   * 商品描述。
   * @example "200 個復古像素遊戲音效，WAV 格式。"
   */
  description?: string | null;
  /**
   * 一句話簡介（市集卡片短標語，至多 200 字）；null 表示未設定。
   * @example "復古像素遊戲必備的 8-bit 音效合輯。"
   */
  summary?: string | null;
  /**
   * 封面色相（0–359）；省略時預設 256。
   * @format int32
   * @example 256
   */
  coverHue?: number | null;
  /**
   * 所屬分類 ID；null 表示未分類。
   * @format uuid
   * @example "3fa85f64-5717-4562-b3fc-2c963f66afa6"
   */
  categoryId?: string | null;
  /**
   * 售價（>= 0）。
   * @format double
   * @example 150
   */
  price?: number;
  /**
   * 幣別（ISO 4217）；省略時預設 TWD。
   * @example "TWD"
   */
  currency?: string | null;
  /**
   * 初始標籤名稱清單（強制小寫）。
   * @example ["audio","retro"]
   */
  tags?: string[] | null;
}

/** 建立商品版本請求。 */
export interface CreateCatalogVersionRequest {
  /**
   * 版本字串（同商品內唯一，1–50 字）。
   * @example "1.0.0"
   */
  version?: string | null;
  /**
   * 版本說明 / 更新紀錄。
   * @example "首次發行。"
   */
  releaseNote?: string | null;
}

/** 商品標籤分頁回應。 */
export interface ListCatalogTagsResponse {
  /**
   * 符合條件的總筆數（未分頁）。
   * @format int32
   * @example 128
   */
  totalCount?: number;
  /** 本頁標籤清單。 */
  items?: CatalogTagDto[] | null;
}

/** 商品列表分頁回應。 */
export interface ListCatalogsResponse {
  /**
   * 符合條件的總筆數（未分頁）。
   * @format int32
   * @example 42
   */
  totalCount?: number;
  /** 本頁商品清單。 */
  items?: CatalogSummaryDto[] | null;
}

/** 申請展示型資產上傳簽章 URL 請求。 */
export interface RequestCatalogAssetUploadUrlRequest {
  /** 商品展示型資產類型。 */
  type?: CatalogAssetType;
  /**
   * 原始檔名（含副檔名）。
   * @example "screenshot-1.png"
   */
  fileName?: string | null;
  /**
   * MIME 類型。
   * @example "image/png"
   */
  contentType?: string | null;
  /**
   * 檔案大小（bytes）。
   * @format int64
   * @example 204800
   */
  sizeBytes?: number;
}

/** 申請版本可下載檔案上傳簽章 URL 請求。 */
export interface RequestVersionAssetUploadUrlRequest {
  /**
   * 原始檔名（含副檔名）。
   * @example "pixel-sfx-pack-v1.pdf"
   */
  fileName?: string | null;
  /**
   * MIME 類型。
   * @example "application/pdf"
   */
  contentType?: string | null;
  /**
   * 檔案大小（bytes）。
   * @format int64
   * @example 10485760
   */
  sizeBytes?: number;
}

/** 設定商品分類請求。 */
export interface SetCatalogCategoryRequest {
  /**
   * 分類 ID；null 表示移除分類。
   * @format uuid
   * @example "3fa85f64-5717-4562-b3fc-2c963f66afa6"
   */
  categoryId?: string | null;
}

/** 設定商品標籤請求（全量覆蓋）。 */
export interface SetCatalogTagsRequest {
  /**
   * 標籤名稱清單（強制小寫，去重）。空陣列表示清除所有標籤。
   * @example ["audio","retro","8bit"]
   */
  tags?: string[] | null;
}

/** StorageService `GET /v1/files/{id}/download-url` 回應。 */
export interface StorageDownloadUrlResult {
  /**
   * 檔案 ID。
   * @format uuid
   */
  fileId?: string;
  /** 短效下載 URL。 */
  downloadUrl?: string | null;
  /**
   * 簽章 URL 過期時間（UTC）。
   * @format date-time
   */
  expiresAt?: string;
}

/** 更新商品分類請求（部分欄位，null 表示不變更）。 */
export interface UpdateCatalogCategoryRequest {
  /**
   * 上層分類 ID；未提供（欄位缺省）表示不變更。
   * @format uuid
   * @example "3fa85f64-5717-4562-b3fc-2c963f66afa6"
   */
  parentId?: string | null;
  /**
   * 分類名稱；null 表示不變更。
   * @example "音樂與音效"
   */
  name?: string | null;
  /**
   * 分類代稱；null 表示不變更。
   * @example "audio"
   */
  slug?: string | null;
  /**
   * 分類補充敘述；null 表示不變更。
   * @example "樂譜、配樂、分軌音檔"
   */
  description?: string | null;
  /**
   * 同層顯示排序；null 表示不變更。
   * @format int32
   * @example 1
   */
  sortOrder?: number | null;
}

/** 更新商品請求（部分欄位，null 表示不變更）。 */
export interface UpdateCatalogRequest {
  /**
   * 商品名稱；null 表示不變更。
   * @example "像素風格音效包 Vol.2"
   */
  name?: string | null;
  /**
   * 商品代稱；null 表示不變更。
   * @example "pixel-sfx-pack-2"
   */
  slug?: string | null;
  /**
   * 商品描述；null 表示不變更，空字串表示清空。
   * @example "新增 50 個音效。"
   */
  description?: string | null;
  /**
   * 一句話簡介；null 表示不變更，空字串表示清空。
   * @example "全新擴充版，收錄 250 個音效。"
   */
  summary?: string | null;
  /**
   * 封面色相（0–359）；null 表示不變更。
   * @format int32
   * @example 320
   */
  coverHue?: number | null;
  /**
   * 售價；null 表示不變更。
   * @format double
   * @example 180
   */
  price?: number | null;
  /**
   * 幣別（ISO 4217）；null 表示不變更。
   * @example "TWD"
   */
  currency?: string | null;
}

/** 版本可下載檔案上傳簽章 URL 回應（私有物件，無公開讀取網址）。 */
export interface VersionAssetUploadUrlResponse {
  /**
   * 已建立的 Asset ID。
   * @format uuid
   * @example "3fa85f64-5717-4562-b3fc-2c963f66afa6"
   */
  assetId?: string;
  /**
   * 前端應使用此 URL 以 HTTP PUT 直傳檔案。
   * @example "http://localhost:5171/v1/files/blob/creators/.../pixel-sfx-pack-v1.pdf?expires=1735689600&sig=..."
   */
  uploadUrl?: string | null;
  /**
   * 簽章 URL 過期時間（UTC）。
   * @format date-time
   */
  expiresAt?: string;
}

/** 商品評論回應。 */
export interface CatalogReviewDto {
  /**
   * 評論唯一識別碼。
   * @format uuid
   * @example "3fa85f64-5717-4562-b3fc-2c963f66afa6"
   */
  id?: string;
  /**
   * 所屬商品 ID。
   * @format uuid
   * @example "3fa85f64-5717-4562-b3fc-2c963f66afa6"
   */
  catalogId?: string;
  /**
   * 評論者使用者 ID。
   * @format uuid
   * @example "3fa85f64-5717-4562-b3fc-2c963f66afa6"
   */
  reviewerUserId?: string;
  /**
   * 評分（1–5）。
   * @format int32
   * @example 5
   */
  rating?: number;
  /**
   * 留言內容；null 表示僅評分未留言。
   * @example "非常實用，物超所值！"
   */
  comment?: string | null;
  /**
   * 建立時間。
   * @format date-time
   */
  createdAt?: string;
  /**
   * 最後更新時間；null 表示未曾更新。
   * @format date-time
   */
  updatedAt?: string | null;
}

/** 新增 / 更新評論請求（同一使用者對同一商品為 upsert）。 */
export interface UpsertReviewRequest {
  /**
   * 評分（1–5）。
   * @format int32
   * @example 5
   */
  rating?: number;
  /**
   * 留言內容（至多 2000 字）；null 或空字串表示僅評分。
   * @example "非常實用，物超所值！"
   */
  comment?: string | null;
}

/** 商品評論列表分頁回應（含彙總）。 */
export interface ListReviewsResponse {
  /**
   * 平均評分（0–5）；無評論時為 0。
   * @format double
   * @example 4.6
   */
  ratingAverage?: number;
  /**
   * 評論總數。
   * @format int32
   * @example 128
   */
  ratingCount?: number;
  /** 本頁評論清單（依時間新到舊）。 */
  items?: CatalogReviewDto[] | null;
}

export type QueryParamsType = Record<string | number, any>;
export type ResponseFormat = keyof Omit<Body, "body" | "bodyUsed">;

export interface FullRequestParams extends Omit<RequestInit, "body"> {
  /** set parameter to `true` for call `securityWorker` for this request */
  secure?: boolean;
  /** request path */
  path: string;
  /** content type of request body */
  type?: ContentType;
  /** query params */
  query?: QueryParamsType;
  /** format of response (i.e. response.json() -> format: "json") */
  format?: ResponseFormat;
  /** request body */
  body?: unknown;
  /** base url */
  baseUrl?: string;
  /** request cancellation token */
  cancelToken?: CancelToken;
}

export type RequestParams = Omit<
  FullRequestParams,
  "body" | "method" | "query" | "path"
>;

export interface ApiConfig<SecurityDataType = unknown> {
  baseUrl?: string;
  baseApiParams?: Omit<RequestParams, "baseUrl" | "cancelToken" | "signal">;
  securityWorker?: (
    securityData: SecurityDataType | null,
  ) => Promise<RequestParams | void> | RequestParams | void;
  customFetch?: typeof fetch;
}

export interface HttpResponse<D extends unknown, E extends unknown = unknown>
  extends Response {
  data: D;
  error: E;
}

type CancelToken = Symbol | string | number;

export enum ContentType {
  Json = "application/json",
  JsonApi = "application/vnd.api+json",
  FormData = "multipart/form-data",
  UrlEncoded = "application/x-www-form-urlencoded",
  Text = "text/plain",
}

export class HttpClient<SecurityDataType = unknown> {
  public baseUrl: string = "";
  private securityData: SecurityDataType | null = null;
  private securityWorker?: ApiConfig<SecurityDataType>["securityWorker"];
  private abortControllers = new Map<CancelToken, AbortController>();
  private customFetch = (...fetchParams: Parameters<typeof fetch>) =>
    fetch(...fetchParams);

  private baseApiParams: RequestParams = {
    credentials: "same-origin",
    headers: {},
    redirect: "follow",
    referrerPolicy: "no-referrer",
  };

  constructor(apiConfig: ApiConfig<SecurityDataType> = {}) {
    Object.assign(this, apiConfig);
  }

  public setSecurityData = (data: SecurityDataType | null) => {
    this.securityData = data;
  };

  protected encodeQueryParam(key: string, value: any) {
    const encodedKey = encodeURIComponent(key);
    return `${encodedKey}=${encodeURIComponent(typeof value === "number" ? value : `${value}`)}`;
  }

  protected addQueryParam(query: QueryParamsType, key: string) {
    return this.encodeQueryParam(key, query[key]);
  }

  protected addArrayQueryParam(query: QueryParamsType, key: string) {
    const value = query[key];
    return value.map((v: any) => this.encodeQueryParam(key, v)).join("&");
  }

  protected toQueryString(rawQuery?: QueryParamsType): string {
    const query = rawQuery || {};
    const keys = Object.keys(query).filter(
      (key) => "undefined" !== typeof query[key],
    );
    return keys
      .map((key) =>
        Array.isArray(query[key])
          ? this.addArrayQueryParam(query, key)
          : this.addQueryParam(query, key),
      )
      .join("&");
  }

  protected addQueryParams(rawQuery?: QueryParamsType): string {
    const queryString = this.toQueryString(rawQuery);
    return queryString ? `?${queryString}` : "";
  }

  private contentFormatters: Record<ContentType, (input: any) => any> = {
    [ContentType.Json]: (input: any) =>
      input !== null && (typeof input === "object" || typeof input === "string")
        ? JSON.stringify(input)
        : input,
    [ContentType.JsonApi]: (input: any) =>
      input !== null && (typeof input === "object" || typeof input === "string")
        ? JSON.stringify(input)
        : input,
    [ContentType.Text]: (input: any) =>
      input !== null && typeof input !== "string"
        ? JSON.stringify(input)
        : input,
    [ContentType.FormData]: (input: any) => {
      if (input instanceof FormData) {
        return input;
      }

      return Object.keys(input || {}).reduce((formData, key) => {
        const property = input[key];
        formData.append(
          key,
          property instanceof Blob
            ? property
            : typeof property === "object" && property !== null
              ? JSON.stringify(property)
              : `${property}`,
        );
        return formData;
      }, new FormData());
    },
    [ContentType.UrlEncoded]: (input: any) => this.toQueryString(input),
  };

  protected mergeRequestParams(
    params1: RequestParams,
    params2?: RequestParams,
  ): RequestParams {
    return {
      ...this.baseApiParams,
      ...params1,
      ...(params2 || {}),
      headers: {
        ...(this.baseApiParams.headers || {}),
        ...(params1.headers || {}),
        ...((params2 && params2.headers) || {}),
      },
    };
  }

  protected createAbortSignal = (
    cancelToken: CancelToken,
  ): AbortSignal | undefined => {
    if (this.abortControllers.has(cancelToken)) {
      const abortController = this.abortControllers.get(cancelToken);
      if (abortController) {
        return abortController.signal;
      }
      return void 0;
    }

    const abortController = new AbortController();
    this.abortControllers.set(cancelToken, abortController);
    return abortController.signal;
  };

  public abortRequest = (cancelToken: CancelToken) => {
    const abortController = this.abortControllers.get(cancelToken);

    if (abortController) {
      abortController.abort();
      this.abortControllers.delete(cancelToken);
    }
  };

  public request = async <T = any, E = any>({
    body,
    secure,
    path,
    type,
    query,
    format,
    baseUrl,
    cancelToken,
    ...params
  }: FullRequestParams): Promise<HttpResponse<T, E>> => {
    const secureParams =
      ((typeof secure === "boolean" ? secure : this.baseApiParams.secure) &&
        this.securityWorker &&
        (await this.securityWorker(this.securityData))) ||
      {};
    const requestParams = this.mergeRequestParams(params, secureParams);
    const queryString = query && this.toQueryString(query);
    const payloadFormatter = this.contentFormatters[type || ContentType.Json];
    const responseFormat = format || requestParams.format;

    return this.customFetch(
      `${baseUrl || this.baseUrl || ""}${path}${queryString ? `?${queryString}` : ""}`,
      {
        ...requestParams,
        headers: {
          ...(requestParams.headers || {}),
          ...(type && type !== ContentType.FormData
            ? { "Content-Type": type }
            : {}),
        },
        signal:
          (cancelToken
            ? this.createAbortSignal(cancelToken)
            : requestParams.signal) || null,
        body:
          typeof body === "undefined" || body === null
            ? null
            : payloadFormatter(body),
      },
    ).then(async (response) => {
      const r = response as HttpResponse<T, E>;
      r.data = null as unknown as T;
      r.error = null as unknown as E;

      const responseToParse = responseFormat ? response.clone() : response;
      const data = !responseFormat
        ? r
        : await responseToParse[responseFormat]()
            .then((data) => {
              if (r.ok) {
                r.data = data;
              } else {
                r.error = data;
              }
              return r;
            })
            .catch((e) => {
              r.error = e;
              return r;
            });

      if (cancelToken) {
        this.abortControllers.delete(cancelToken);
      }

      if (!response.ok) throw data;
      return data;
    });
  };
}

/**
 * @title CatalogService
 * @version v1
 */
export class Api<SecurityDataType extends unknown> {
  http: HttpClient<SecurityDataType>;

  constructor(http: HttpClient<SecurityDataType>) {
    this.http = http;
  }

  catalogCategories = {
    /**
     * No description
     *
     * @tags CatalogCategories
     * @name List
     * @summary 列出所有分類（公開）。
     * @request GET:/v1/catalog-categories
     */
    list: (params: RequestParams = {}) =>
      this.http.request<CatalogCategoryDto[], any>({
        path: `/v1/catalog-categories`,
        method: "GET",
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags CatalogCategories
     * @name Create
     * @summary 建立分類。僅 Admin 可操作。
     * @request POST:/v1/catalog-categories
     */
    create: (data: CreateCatalogCategoryRequest, params: RequestParams = {}) =>
      this.http.request<CatalogCategoryDto, any>({
        path: `/v1/catalog-categories`,
        method: "POST",
        body: data,
        type: ContentType.Json,
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags CatalogCategories
     * @name Get
     * @summary 查詢單一分類（公開）。
     * @request GET:/v1/catalog-categories/{id}
     */
    get: (id: string, params: RequestParams = {}) =>
      this.http.request<CatalogCategoryDto, any>({
        path: `/v1/catalog-categories/${id}`,
        method: "GET",
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags CatalogCategories
     * @name Update
     * @summary 更新分類。僅 Admin 可操作。
     * @request PATCH:/v1/catalog-categories/{id}
     */
    update: (
      id: string,
      data: UpdateCatalogCategoryRequest,
      params: RequestParams = {},
    ) =>
      this.http.request<CatalogCategoryDto, any>({
        path: `/v1/catalog-categories/${id}`,
        method: "PATCH",
        body: data,
        type: ContentType.Json,
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags CatalogCategories
     * @name Delete
     * @summary 刪除分類（不可有子分類或被商品引用）。僅 Admin 可操作。
     * @request DELETE:/v1/catalog-categories/{id}
     */
    delete: (id: string, params: RequestParams = {}) =>
      this.http.request<void, any>({
        path: `/v1/catalog-categories/${id}`,
        method: "DELETE",
        ...params,
      }),
  };
  catalogs = {
    /**
     * No description
     *
     * @tags Catalogs
     * @name List
     * @summary 瀏覽已上架商品列表（公開）。
     * @request GET:/v1/catalogs
     */
    list: (
      query?: {
        /**
         * 限定商店 ID；null 表示不限。
         * @format uuid
         * @example "3fa85f64-5717-4562-b3fc-2c963f66afa6"
         */
        StoreId?: string;
        /**
         * 限定分類 ID；null 表示不限。
         * @format uuid
         * @example "3fa85f64-5717-4562-b3fc-2c963f66afa6"
         */
        CategoryId?: string;
        /**
         * 限定標籤名稱；null 表示不限。
         * @example "retro"
         */
        Tag?: string;
        /**
         * 名稱關鍵字搜尋；null 表示不限。
         * @example "音效"
         */
        Search?: string;
        /**
         * 僅限編輯精選；true 只回精選、false 只回非精選、null 表示不限。
         * @example true
         */
        Featured?: boolean;
        /**
         * 售價下限（含）；null 表示不限。
         * @format double
         * @example 0
         */
        MinPrice?: number;
        /**
         * 售價上限（含）；null 表示不限。
         * @format double
         * @example 30
         */
        MaxPrice?: number;
        /** 排序方式；省略時預設最新上架。 */
        Sort?: CatalogSort;
        /**
         * 略過筆數。
         * @format int32
         * @example 0
         */
        Offset?: number;
        /**
         * 每頁筆數（最大 100）。
         * @format int32
         * @example 20
         */
        Limit?: number;
      },
      params: RequestParams = {},
    ) =>
      this.http.request<ListCatalogsResponse, any>({
        path: `/v1/catalogs`,
        method: "GET",
        query: query,
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags Catalogs
     * @name Create
     * @summary 建立商品（草稿）。僅商店 Owner 可操作。
     * @request POST:/v1/catalogs
     */
    create: (data: CreateCatalogRequest, params: RequestParams = {}) =>
      this.http.request<CatalogDto, any>({
        path: `/v1/catalogs`,
        method: "POST",
        body: data,
        type: ContentType.Json,
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags Catalogs
     * @name ListMine
     * @summary 查詢登入使用者（商店 Owner）的商品列表，含未上架商品。
     * @request GET:/v1/catalogs/mine
     */
    listMine: (
      query?: {
        /**
         * 限定商店 ID；null 表示不限。
         * @format uuid
         * @example "3fa85f64-5717-4562-b3fc-2c963f66afa6"
         */
        StoreId?: string;
        /**
         * 限定分類 ID；null 表示不限。
         * @format uuid
         * @example "3fa85f64-5717-4562-b3fc-2c963f66afa6"
         */
        CategoryId?: string;
        /**
         * 限定標籤名稱；null 表示不限。
         * @example "retro"
         */
        Tag?: string;
        /**
         * 名稱關鍵字搜尋；null 表示不限。
         * @example "音效"
         */
        Search?: string;
        /**
         * 略過筆數。
         * @format int32
         * @example 0
         */
        Offset?: number;
        /**
         * 每頁筆數（最大 100）。
         * @format int32
         * @example 20
         */
        Limit?: number;
      },
      params: RequestParams = {},
    ) =>
      this.http.request<ListCatalogsResponse, any>({
        path: `/v1/catalogs/mine`,
        method: "GET",
        query: query,
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags Catalogs
     * @name ListByStore
     * @summary 查詢指定商店的全部商品（含草稿 / 已下架 / 已停權）。僅 Admin 可操作。
     * @request GET:/v1/catalogs/by-store/{storeId}
     */
    listByStore: (
      storeId: string,
      query?: {
        /**
         * 限定商店 ID；null 表示不限。
         * @format uuid
         * @example "3fa85f64-5717-4562-b3fc-2c963f66afa6"
         */
        StoreId?: string;
        /**
         * 限定分類 ID；null 表示不限。
         * @format uuid
         * @example "3fa85f64-5717-4562-b3fc-2c963f66afa6"
         */
        CategoryId?: string;
        /**
         * 限定標籤名稱；null 表示不限。
         * @example "retro"
         */
        Tag?: string;
        /**
         * 名稱關鍵字搜尋；null 表示不限。
         * @example "音效"
         */
        Search?: string;
        /**
         * 略過筆數。
         * @format int32
         * @example 0
         */
        Offset?: number;
        /**
         * 每頁筆數（最大 100）。
         * @format int32
         * @example 20
         */
        Limit?: number;
      },
      params: RequestParams = {},
    ) =>
      this.http.request<ListCatalogsResponse, any>({
        path: `/v1/catalogs/by-store/${storeId}`,
        method: "GET",
        query: query,
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags Catalogs
     * @name Get
     * @summary 查詢商品完整資訊。未上架商品僅 Owner 可見。
     * @request GET:/v1/catalogs/{id}
     */
    get: (id: string, params: RequestParams = {}) =>
      this.http.request<CatalogDto, any>({
        path: `/v1/catalogs/${id}`,
        method: "GET",
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags Catalogs
     * @name Update
     * @summary 更新商品基本資料。僅 Owner 可操作。
     * @request PATCH:/v1/catalogs/{id}
     */
    update: (
      id: string,
      data: UpdateCatalogRequest,
      params: RequestParams = {},
    ) =>
      this.http.request<CatalogDto, any>({
        path: `/v1/catalogs/${id}`,
        method: "PATCH",
        body: data,
        type: ContentType.Json,
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags Catalogs
     * @name IncrementView
     * @summary 商品詳情頁瀏覽次數 +1（公開）。
     * @request POST:/v1/catalogs/{id}/view
     */
    incrementView: (id: string, params: RequestParams = {}) =>
      this.http.request<void, any>({
        path: `/v1/catalogs/${id}/view`,
        method: "POST",
        ...params,
      }),

    /**
     * No description
     *
     * @tags Catalogs
     * @name SetCategory
     * @summary 設定 / 移除商品分類。僅 Owner 可操作。
     * @request PUT:/v1/catalogs/{id}/category
     */
    setCategory: (
      id: string,
      data: SetCatalogCategoryRequest,
      params: RequestParams = {},
    ) =>
      this.http.request<CatalogDto, any>({
        path: `/v1/catalogs/${id}/category`,
        method: "PUT",
        body: data,
        type: ContentType.Json,
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags Catalogs
     * @name SetTags
     * @summary 全量覆蓋商品標籤。僅 Owner 可操作。
     * @request PUT:/v1/catalogs/{id}/tags
     */
    setTags: (
      id: string,
      data: SetCatalogTagsRequest,
      params: RequestParams = {},
    ) =>
      this.http.request<string[], any>({
        path: `/v1/catalogs/${id}/tags`,
        method: "PUT",
        body: data,
        type: ContentType.Json,
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags Catalogs
     * @name Publish
     * @summary 上架商品（Draft/Archived → Published）。需已有目前版本。僅 Owner 可操作。
     * @request POST:/v1/catalogs/{id}/publish
     */
    publish: (id: string, params: RequestParams = {}) =>
      this.http.request<void, any>({
        path: `/v1/catalogs/${id}/publish`,
        method: "POST",
        ...params,
      }),

    /**
     * No description
     *
     * @tags Catalogs
     * @name Archive
     * @summary 下架封存商品（Published → Archived）。僅 Owner 可操作。
     * @request POST:/v1/catalogs/{id}/archive
     */
    archive: (id: string, params: RequestParams = {}) =>
      this.http.request<void, any>({
        path: `/v1/catalogs/${id}/archive`,
        method: "POST",
        ...params,
      }),

    /**
     * No description
     *
     * @tags Catalogs
     * @name Suspend
     * @summary 平台停權商品（任意狀態 → Suspended）。僅 Admin 可操作。
     * @request POST:/v1/catalogs/{id}/suspend
     */
    suspend: (id: string, params: RequestParams = {}) =>
      this.http.request<void, any>({
        path: `/v1/catalogs/${id}/suspend`,
        method: "POST",
        ...params,
      }),

    /**
     * No description
     *
     * @tags Catalogs
     * @name Unsuspend
     * @summary 解除商品停權（Suspended → Archived）。僅 Admin 可操作。
     * @request POST:/v1/catalogs/{id}/unsuspend
     */
    unsuspend: (id: string, params: RequestParams = {}) =>
      this.http.request<void, any>({
        path: `/v1/catalogs/${id}/unsuspend`,
        method: "POST",
        ...params,
      }),

    /**
     * No description
     *
     * @tags Catalogs
     * @name Feature
     * @summary 設為編輯精選（市集首頁精選輪播）。僅 Admin 可操作。
     * @request POST:/v1/catalogs/{id}/feature
     */
    feature: (id: string, params: RequestParams = {}) =>
      this.http.request<void, any>({
        path: `/v1/catalogs/${id}/feature`,
        method: "POST",
        ...params,
      }),

    /**
     * No description
     *
     * @tags Catalogs
     * @name Unfeature
     * @summary 取消編輯精選。僅 Admin 可操作。
     * @request POST:/v1/catalogs/{id}/unfeature
     */
    unfeature: (id: string, params: RequestParams = {}) =>
      this.http.request<void, any>({
        path: `/v1/catalogs/${id}/unfeature`,
        method: "POST",
        ...params,
      }),

    /**
     * No description
     *
     * @tags Catalogs
     * @name RequestAssetUploadUrl
     * @summary 申請展示型資產（縮圖 / 截圖 / 預覽影音）上傳簽章 URL。僅 Owner 可操作。
     * @request POST:/v1/catalogs/{id}/assets/upload-url
     */
    requestAssetUploadUrl: (
      id: string,
      data: RequestCatalogAssetUploadUrlRequest,
      params: RequestParams = {},
    ) =>
      this.http.request<CatalogAssetUploadUrlResponse, any>({
        path: `/v1/catalogs/${id}/assets/upload-url`,
        method: "POST",
        body: data,
        type: ContentType.Json,
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags Catalogs
     * @name DeleteAsset
     * @summary 刪除展示型資產。僅 Owner 可操作。
     * @request DELETE:/v1/catalogs/{id}/assets/{assetId}
     */
    deleteAsset: (id: string, assetId: string, params: RequestParams = {}) =>
      this.http.request<void, any>({
        path: `/v1/catalogs/${id}/assets/${assetId}`,
        method: "DELETE",
        ...params,
      }),
  };
  catalogService = {
    /**
     * No description
     *
     * @tags CatalogService
     * @name HealthzList
     * @request GET:/healthz
     */
    healthzList: (params: RequestParams = {}) =>
      this.http.request<void, any>({
        path: `/healthz`,
        method: "GET",
        ...params,
      }),
  };
  catalogTags = {
    /**
     * No description
     *
     * @tags CatalogTags
     * @name List
     * @summary 分頁查詢標籤（依使用次數遞減，公開）。
     * @request GET:/v1/catalog-tags
     */
    list: (
      query?: {
        /**
         * 名稱前綴關鍵字（強制小寫）；null 表示不限。
         * @example "ret"
         */
        Search?: string;
        /**
         * 略過筆數。
         * @format int32
         * @example 0
         */
        Offset?: number;
        /**
         * 每頁筆數（最大 100）。
         * @format int32
         * @example 20
         */
        Limit?: number;
      },
      params: RequestParams = {},
    ) =>
      this.http.request<ListCatalogTagsResponse, any>({
        path: `/v1/catalog-tags`,
        method: "GET",
        query: query,
        format: "json",
        ...params,
      }),
  };
  catalogVersions = {
    /**
     * No description
     *
     * @tags CatalogVersions
     * @name List
     * @summary 列出商品的所有版本（新到舊）。
     * @request GET:/v1/catalogs/{catalogId}/versions
     */
    list: (catalogId: string, params: RequestParams = {}) =>
      this.http.request<CatalogVersionDto[], any>({
        path: `/v1/catalogs/${catalogId}/versions`,
        method: "GET",
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags CatalogVersions
     * @name Create
     * @summary 建立新版本，並設為商品的目前版本。
     * @request POST:/v1/catalogs/{catalogId}/versions
     */
    create: (
      catalogId: string,
      data: CreateCatalogVersionRequest,
      params: RequestParams = {},
    ) =>
      this.http.request<CatalogVersionDto, any>({
        path: `/v1/catalogs/${catalogId}/versions`,
        method: "POST",
        body: data,
        type: ContentType.Json,
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags CatalogVersions
     * @name RequestAssetUploadUrl
     * @summary 申請版本可下載檔案上傳簽章 URL（私有物件）。
     * @request POST:/v1/catalogs/{catalogId}/versions/{versionId}/assets/upload-url
     */
    requestAssetUploadUrl: (
      catalogId: string,
      versionId: string,
      data: RequestVersionAssetUploadUrlRequest,
      params: RequestParams = {},
    ) =>
      this.http.request<VersionAssetUploadUrlResponse, any>({
        path: `/v1/catalogs/${catalogId}/versions/${versionId}/assets/upload-url`,
        method: "POST",
        body: data,
        type: ContentType.Json,
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags CatalogVersions
     * @name GetAssetDownloadUrl
     * @summary 取得版本可下載檔案的下載簽章 URL（管理用途）。
     * @request GET:/v1/catalogs/{catalogId}/versions/{versionId}/assets/{assetId}/download-url
     */
    getAssetDownloadUrl: (
      catalogId: string,
      versionId: string,
      assetId: string,
      params: RequestParams = {},
    ) =>
      this.http.request<StorageDownloadUrlResult, any>({
        path: `/v1/catalogs/${catalogId}/versions/${versionId}/assets/${assetId}/download-url`,
        method: "GET",
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags CatalogVersions
     * @name DeleteAsset
     * @summary 刪除版本可下載檔案。
     * @request DELETE:/v1/catalogs/{catalogId}/versions/{versionId}/assets/{assetId}
     */
    deleteAsset: (
      catalogId: string,
      versionId: string,
      assetId: string,
      params: RequestParams = {},
    ) =>
      this.http.request<void, any>({
        path: `/v1/catalogs/${catalogId}/versions/${versionId}/assets/${assetId}`,
        method: "DELETE",
        ...params,
      }),
  };
  catalogReviews = {
    /**
     * No description
     *
     * @tags CatalogReviews
     * @name List
     * @summary 分頁列出商品評論（公開），含平均分與評論數。
     * @request GET:/v1/catalogs/{catalogId}/reviews
     */
    list: (
      catalogId: string,
      query?: {
        /**
         * 略過筆數。
         * @format int32
         * @example 0
         */
        Offset?: number;
        /**
         * 每頁筆數（最大 100）。
         * @format int32
         * @example 20
         */
        Limit?: number;
      },
      params: RequestParams = {},
    ) =>
      this.http.request<ListReviewsResponse, any>({
        path: `/v1/catalogs/${catalogId}/reviews`,
        method: "GET",
        query: query,
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags CatalogReviews
     * @name GetMine
     * @summary 取得目前使用者對此商品的評論；尚未評論回傳 204。
     * @request GET:/v1/catalogs/{catalogId}/reviews/mine
     */
    getMine: (catalogId: string, params: RequestParams = {}) =>
      this.http.request<CatalogReviewDto, any>({
        path: `/v1/catalogs/${catalogId}/reviews/mine`,
        method: "GET",
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags CatalogReviews
     * @name UpsertMine
     * @summary 新增 / 更新本人對此商品的評論（一人一則）。須為已購買者。
     * @request PUT:/v1/catalogs/{catalogId}/reviews/mine
     */
    upsertMine: (
      catalogId: string,
      data: UpsertReviewRequest,
      params: RequestParams = {},
    ) =>
      this.http.request<CatalogReviewDto, any>({
        path: `/v1/catalogs/${catalogId}/reviews/mine`,
        method: "PUT",
        body: data,
        type: ContentType.Json,
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags CatalogReviews
     * @name DeleteMine
     * @summary 刪除本人對此商品的評論。
     * @request DELETE:/v1/catalogs/{catalogId}/reviews/mine
     */
    deleteMine: (catalogId: string, params: RequestParams = {}) =>
      this.http.request<void, any>({
        path: `/v1/catalogs/${catalogId}/reviews/mine`,
        method: "DELETE",
        ...params,
      }),
  };
}
