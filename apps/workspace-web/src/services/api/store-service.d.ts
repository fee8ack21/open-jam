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

/**
 * 商店狀態。
 * @format int32
 */
export declare enum StoreStatus {
  Value0 = 0,
  Value1 = 1,
  Value2 = 2,
}
/**
 * 商店成員角色。
 * @format int32
 */
export declare enum StoreMemberRole {
  Value0 = 0,
}
/**
 * 開店申請審核狀態。
 * @format int32
 */
export declare enum StoreApplicationStatus {
  Value0 = 0,
  Value1 = 1,
  Value2 = 2,
  Value3 = 3,
}
/** Avatar/Banner 上傳簽章 URL 回應。 */
export interface AssetUploadUrlResponse {
  /**
   * 已建立的 Asset ID。
   * @format uuid
   * @example "3fa85f64-5717-4562-b3fc-2c963f66afa6"
   */
  assetId?: string;
  /**
   * 前端應使用此 URL 以 HTTP PUT 直傳檔案。
   * @example "http://localhost:9000/open-jam/public/.../avatar.png?X-Amz-Signature=..."
   */
  uploadUrl?: string | null;
  /**
   * 上傳完成後的公開讀取網址。
   * @example "http://localhost:9000/open-jam/public/.../avatar.png"
   */
  publicUrl?: string | null;
  /**
   * 簽章 URL 過期時間（UTC）。
   * @format date-time
   */
  expiresAt?: string;
}
/** 追蹤／取消追蹤商店請求。 */
export interface FollowStoreRequest {
  /**
   * 追蹤者電子信箱。
   * @example "follower@example.com"
   */
  email?: string | null;
}
/** 開店申請分頁查詢回應。 */
export interface GetStoreApplicationsResponse {
  /**
   * 符合條件的總筆數（未分頁）。
   * @format int32
   * @example 3
   */
  totalCount?: number;
  /** 本頁開店申請清單。 */
  items?: StoreApplicationDto[] | null;
}
/** 商店追蹤者分頁查詢回應。 */
export interface GetStoreFollowersResponse {
  /**
   * 符合條件的總筆數（未分頁）。
   * @format int32
   * @example 42
   */
  totalCount?: number;
  /** 本頁追蹤者清單。 */
  items?: StoreFollowerDto[] | null;
}
/** 登入使用者所屬商店資訊。 */
export interface MyStoreDto {
  /** 商店基本資訊回應。 */
  store?: StoreDto;
  /** 商店成員角色。 */
  role?: StoreMemberRole;
}
/** 駁回開店申請請求。 */
export interface RejectStoreApplicationRequest {
  /**
   * 駁回原因，將附於通知信中。
   * @example "商店名稱與既有品牌過於相似，請調整後重新申請。"
   */
  reviewComment?: string | null;
}
/** 申請 Avatar/Banner 上傳簽章 URL 請求。 */
export interface RequestAssetUploadUrlRequest {
  /**
   * 原始檔名（含副檔名）。
   * @example "avatar.png"
   */
  fileName?: string | null;
  /**
   * MIME 類型，僅允許 jpeg/png/gif/webp。
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
/** 單筆開店申請回應。 */
export interface StoreApplicationDto {
  /**
   * 申請唯一識別碼。
   * @format uuid
   * @example "3fa85f64-5717-4562-b3fc-2c963f66afa6"
   */
  id?: string;
  /**
   * 申請人使用者 ID。
   * @format uuid
   * @example "3fa85f64-5717-4562-b3fc-2c963f66afa6"
   */
  userId?: string;
  /**
   * 申請人電子信箱。
   * @example "creator@example.com"
   */
  email?: string | null;
  /**
   * 欲申請的商店顯示名稱。
   * @example "小明的數位商店"
   */
  storeName?: string | null;
  /**
   * 欲申請的商店子網域代稱。
   * @example "xiaoming-shop"
   */
  storeSlug?: string | null;
  /** 開店申請審核狀態。 */
  status?: StoreApplicationStatus;
  /**
   * 提交時間。
   * @format date-time
   */
  createdAt?: string;
  /**
   * 審核時間；null 表示尚未審核。
   * @format date-time
   */
  reviewedAt?: string | null;
  /**
   * 審核管理員使用者 ID。
   * @format uuid
   */
  reviewedBy?: string | null;
  /**
   * 審核意見，主要用於 Rejected。
   * @example "商店名稱與既有品牌過於相似，請調整後重新申請。"
   */
  reviewComment?: string | null;
}
/** 商店基本資訊回應。 */
export interface StoreDto {
  /**
   * 商店唯一識別碼。
   * @format uuid
   * @example "3fa85f64-5717-4562-b3fc-2c963f66afa6"
   */
  id?: string;
  /**
   * 商店顯示名稱。
   * @example "小明的數位商店"
   */
  storeName?: string | null;
  /**
   * 商店子網域代稱。
   * @example "xiaoming-shop"
   */
  storeSlug?: string | null;
  /**
   * 商店描述。
   * @example "專注於數位插畫與素材販售。"
   */
  description?: string | null;
  /**
   * 商店頭像公開 URL；null 表示尚未設定。
   * @example "http://localhost:9000/open-jam/public/3fa85f64-5717-4562-b3fc-2c963f66afa6/avatar.png"
   */
  avatarUrl?: string | null;
  /**
   * 商店橫幅公開 URL；null 表示尚未設定。
   * @example "http://localhost:9000/open-jam/public/3fa85f64-5717-4562-b3fc-2c963f66afa6/banner.png"
   */
  bannerUrl?: string | null;
  /** 商店狀態。 */
  status?: StoreStatus;
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
/** 單筆商店追蹤者回應。 */
export interface StoreFollowerDto {
  /**
   * 追蹤者電子信箱。
   * @example "follower@example.com"
   */
  email?: string | null;
  /**
   * 追蹤者使用者 ID；null 表示尚未關聯帳號（訪客憑信箱追蹤）。
   * @format uuid
   */
  userId?: string | null;
  /**
   * 追蹤建立時間。
   * @format date-time
   */
  createdAt?: string;
}
/** 提交開店申請請求。 */
export interface SubmitStoreApplicationRequest {
  /**
   * 欲申請的商店顯示名稱。
   * @example "小明的數位商店"
   */
  storeName?: string | null;
  /**
   * 欲申請的商店子網域代稱。小寫英數字 + 連字號，3–30 字，不可開頭/結尾為連字號。
   * @example "xiaoming-shop"
   */
  storeSlug?: string | null;
}
/** 更新商店資料請求（部分欄位，未提供者不變更）。 */
export interface UpdateStoreRequest {
  /**
   * 商店顯示名稱；null 表示不變更。
   * @example "小明的數位商店"
   */
  storeName?: string | null;
  /**
   * 商店描述；null 表示不變更，空字串表示清空。
   * @example "專注於數位插畫與素材販售。"
   */
  description?: string | null;
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
export declare enum ContentType {
  Json = "application/json",
  JsonApi = "application/vnd.api+json",
  FormData = "multipart/form-data",
  UrlEncoded = "application/x-www-form-urlencoded",
  Text = "text/plain",
}
export declare class HttpClient<SecurityDataType = unknown> {
  baseUrl: string;
  private securityData;
  private securityWorker?;
  private abortControllers;
  private customFetch;
  private baseApiParams;
  constructor(apiConfig?: ApiConfig<SecurityDataType>);
  setSecurityData: (data: SecurityDataType | null) => void;
  protected encodeQueryParam(key: string, value: any): string;
  protected addQueryParam(query: QueryParamsType, key: string): string;
  protected addArrayQueryParam(query: QueryParamsType, key: string): any;
  protected toQueryString(rawQuery?: QueryParamsType): string;
  protected addQueryParams(rawQuery?: QueryParamsType): string;
  private contentFormatters;
  protected mergeRequestParams(
    params1: RequestParams,
    params2?: RequestParams,
  ): RequestParams;
  protected createAbortSignal: (
    cancelToken: CancelToken,
  ) => AbortSignal | undefined;
  abortRequest: (cancelToken: CancelToken) => void;
  request: <T = any, E = any>({
    body,
    secure,
    path,
    type,
    query,
    format,
    baseUrl,
    cancelToken,
    ...params
  }: FullRequestParams) => Promise<HttpResponse<T, E>>;
}
/**
 * @title StoreService
 * @version v1
 */
export declare class Api<SecurityDataType extends unknown> {
  http: HttpClient<SecurityDataType>;
  constructor(http: HttpClient<SecurityDataType>);
  storeApplications: {
    /**
     * No description
     *
     * @tags StoreApplications
     * @name StoreApplicationsCreate
     * @summary 提交開店申請。同一使用者僅能有一筆 Pending 申請。
     * @request POST:/store-applications
     */
    storeApplicationsCreate: (
      data: SubmitStoreApplicationRequest,
      params?: RequestParams,
    ) => Promise<HttpResponse<StoreApplicationDto, any>>;
    /**
     * No description
     *
     * @tags StoreApplications
     * @name StoreApplicationsList
     * @summary 查詢全平台開店申請列表，可依審核狀態篩選（分頁）。
     * @request GET:/store-applications
     */
    storeApplicationsList: (
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
        /** 過濾審核狀態。 */
        Status?: StoreApplicationStatus;
      },
      params?: RequestParams,
    ) => Promise<HttpResponse<GetStoreApplicationsResponse, any>>;
    /**
     * No description
     *
     * @tags StoreApplications
     * @name GetStoreApplications
     * @summary 查詢自己的開店申請紀錄（分頁）。
     * @request GET:/store-applications/me
     */
    getStoreApplications: (
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
        /** 過濾審核狀態。 */
        Status?: StoreApplicationStatus;
      },
      params?: RequestParams,
    ) => Promise<HttpResponse<GetStoreApplicationsResponse, any>>;
    /**
     * No description
     *
     * @tags StoreApplications
     * @name WithdrawCreate
     * @summary 撤回自己的待審核申請（Pending → Withdrawn），可重新提交。
     * @request POST:/store-applications/{id}/withdraw
     */
    withdrawCreate: (
      id: string,
      params?: RequestParams,
    ) => Promise<HttpResponse<void, any>>;
    /**
     * No description
     *
     * @tags StoreApplications
     * @name ApproveCreate
     * @summary 核准開店申請（Pending → Approved），建立 Store 與 StoreMember(Owner)。
     * @request POST:/store-applications/{id}/approve
     */
    approveCreate: (
      id: string,
      params?: RequestParams,
    ) => Promise<HttpResponse<StoreApplicationDto, any>>;
    /**
     * No description
     *
     * @tags StoreApplications
     * @name RejectCreate
     * @summary 駁回開店申請（Pending → Rejected），可重新提交。
     * @request POST:/store-applications/{id}/reject
     */
    rejectCreate: (
      id: string,
      data: RejectStoreApplicationRequest,
      params?: RequestParams,
    ) => Promise<HttpResponse<StoreApplicationDto, any>>;
  };
  stores: {
    /**
     * No description
     *
     * @tags StoreFollowers
     * @name FollowCreate
     * @summary 追蹤商店。已追蹤則 no-op。
     * @request POST:/stores/{id}/follow
     */
    followCreate: (
      id: string,
      data: FollowStoreRequest,
      params?: RequestParams,
    ) => Promise<HttpResponse<void, any>>;
    /**
     * No description
     *
     * @tags StoreFollowers
     * @name FollowDelete
     * @summary 取消追蹤商店。依 (StoreId, Email) 移除，未追蹤則 no-op。
     * @request DELETE:/stores/{id}/follow
     */
    followDelete: (
      id: string,
      data: FollowStoreRequest,
      params?: RequestParams,
    ) => Promise<HttpResponse<void, any>>;
    /**
     * No description
     *
     * @tags StoreFollowers
     * @name FollowersList
     * @summary 查詢商店追蹤者列表（分頁）。僅 Owner 可操作。
     * @request GET:/stores/{id}/followers
     */
    followersList: (
      id: string,
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
      params?: RequestParams,
    ) => Promise<HttpResponse<GetStoreFollowersResponse, any>>;
    /**
     * No description
     *
     * @tags Stores
     * @name StoresDetail
     * @summary 查詢商店基本資訊（公開）。
     * @request GET:/stores/{idOrSlug}
     */
    storesDetail: (
      idOrSlug: string,
      params?: RequestParams,
    ) => Promise<HttpResponse<StoreDto, any>>;
    /**
     * No description
     *
     * @tags Stores
     * @name GetStores
     * @summary 查詢登入使用者所屬的商店列表。
     * @request GET:/stores/me
     */
    getStores: (
      params?: RequestParams,
    ) => Promise<HttpResponse<MyStoreDto[], any>>;
    /**
     * No description
     *
     * @tags Stores
     * @name StoresPartialUpdate
     * @summary 更新商店基本資料（StoreName / Description）。僅 Owner 可操作。
     * @request PATCH:/stores/{id}
     */
    storesPartialUpdate: (
      id: string,
      data: UpdateStoreRequest,
      params?: RequestParams,
    ) => Promise<HttpResponse<StoreDto, any>>;
    /**
     * No description
     *
     * @tags Stores
     * @name SuspendCreate
     * @summary 平台停權商店（Active → Suspended）。僅 Admin 可操作。
     * @request POST:/stores/{id}/suspend
     */
    suspendCreate: (
      id: string,
      params?: RequestParams,
    ) => Promise<HttpResponse<void, any>>;
    /**
     * No description
     *
     * @tags Stores
     * @name UnsuspendCreate
     * @summary 解除商店停權（Suspended → Active）。僅 Admin 可操作。
     * @request POST:/stores/{id}/unsuspend
     */
    unsuspendCreate: (
      id: string,
      params?: RequestParams,
    ) => Promise<HttpResponse<void, any>>;
    /**
     * No description
     *
     * @tags Stores
     * @name CloseCreate
     * @summary 關閉商店（Active/Suspended → Closed，終態不可逆）。Owner 或 Admin 可操作。
     * @request POST:/stores/{id}/close
     */
    closeCreate: (
      id: string,
      params?: RequestParams,
    ) => Promise<HttpResponse<void, any>>;
    /**
     * No description
     *
     * @tags Stores
     * @name AvatarUploadUrlCreate
     * @summary 申請商店頭像（Avatar）上傳簽章 URL。僅 Owner 可操作。
     * @request POST:/stores/{id}/avatar/upload-url
     */
    avatarUploadUrlCreate: (
      id: string,
      data: RequestAssetUploadUrlRequest,
      params?: RequestParams,
    ) => Promise<HttpResponse<AssetUploadUrlResponse, any>>;
    /**
     * No description
     *
     * @tags Stores
     * @name BannerUploadUrlCreate
     * @summary 申請商店橫幅（Banner）上傳簽章 URL。僅 Owner 可操作。
     * @request POST:/stores/{id}/banner/upload-url
     */
    bannerUploadUrlCreate: (
      id: string,
      data: RequestAssetUploadUrlRequest,
      params?: RequestParams,
    ) => Promise<HttpResponse<AssetUploadUrlResponse, any>>;
  };
  healthz: {
    /**
     * No description
     *
     * @tags StoreService
     * @name HealthzList
     * @request GET:/healthz
     */
    healthzList: (params?: RequestParams) => Promise<HttpResponse<void, any>>;
  };
}
export {};
