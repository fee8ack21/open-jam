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
 * 媒體類型分類。
 * @format int32
 */
export enum FileType {
  Value0 = 0,
  Value1 = 1,
  Value2 = 2,
}

/**
 * 檔案處理狀態。
 * @format int32
 */
export enum FileStatus {
  Value0 = 0,
  Value1 = 1,
  Value2 = 2,
  Value3 = 3,
}

/** 單一創作者用量明細。 */
export interface CreatorUsageDto {
  /**
   * 創作者 ID。
   * @format uuid
   * @example "3fa85f64-5717-4562-b3fc-2c963f66afa6"
   */
  creatorId?: string;
  /**
   * 已 Ready 檔案數。
   * @format int32
   * @example 312
   */
  fileCount?: number;
  /**
   * 已 Ready 檔案位元組總和。
   * @format int64
   * @example 19783458816
   */
  bytes?: number;
}

/** 檔案紀錄回應 DTO。 */
export interface FileDto {
  /**
   * 檔案唯一識別碼。
   * @format uuid
   * @example "3fa85f64-5717-4562-b3fc-2c963f66afa6"
   */
  id?: string;
  /**
   * 擁有者（創作者）ID。
   * @format uuid
   * @example "3fa85f64-5717-4562-b3fc-2c963f66afa6"
   */
  creatorId?: string;
  /**
   * 所屬商品 ID。
   * @format uuid
   */
  productId?: string | null;
  /**
   * 在儲存後端的物件鍵值。
   * @example "creators/3fa85f64-5717-4562-b3fc-2c963f66afa6/.../intro-video.mp4"
   */
  storageKey?: string | null;
  /**
   * 使用者上傳時的原始檔名。
   * @example "intro-video.mp4"
   */
  originalName?: string | null;
  /**
   * MIME 類型。
   * @example "video/mp4"
   */
  contentType?: string | null;
  /**
   * 檔案大小（bytes）。
   * @format int64
   * @example 104857600
   */
  sizeBytes?: number | null;
  /** 媒體類型分類。 */
  fileType?: FileType;
  /** 檔案處理狀態。 */
  status?: FileStatus;
  /**
   * 是否為公開預覽衍生檔。
   * @example false
   */
  isPreview?: boolean;
  /**
   * 功能 API 確認此檔已被實際使用的時間；null 表示尚未被使用。
   * @format date-time
   */
  referencedAt?: string | null;
  /**
   * 建立時間（UTC）。
   * @format date-time
   */
  createdAt?: string;
  /**
   * 最後更新時間（UTC）。
   * @format date-time
   */
  updatedAt?: string | null;
}

/** 下載簽章 URL 回應。 */
export interface GetDownloadUrlResponse {
  /**
   * 檔案 ID。
   * @format uuid
   * @example "3fa85f64-5717-4562-b3fc-2c963f66afa6"
   */
  fileId?: string;
  /**
   * 有效的短效下載 URL；調用方應立即轉交給終端使用者，不應快取。
   * @example "http://localhost:5171/v1/files/blob/creators/.../intro-video.mp4?expires=1735689600&sig=..."
   */
  downloadUrl?: string | null;
  /**
   * 簽章 URL 過期時間（UTC）。
   * @format date-time
   */
  expiresAt?: string;
}

/**
 * 平台儲存用量彙總（Admin）。僅涵蓋 StorageService 可實際統計的指標：
 * 物件數量 / 大小（依公開展示 vs 私有可下載拆分）與孤兒檔。
 * 下載流量與歷史趨勢需另建指標管線，不在此端點。
 */
export interface PlatformUsageResponse {
  /**
   * 已 Ready 檔案位元組總和（公開 + 私有）。
   * @format int64
   * @example 71403606016
   */
  usedBytes?: number;
  /**
   * 已 Ready 檔案總數。
   * @format int32
   * @example 1180
   */
  fileCount?: number;
  /**
   * 公開展示型資產（預覽 / 縮圖）位元組總和。
   * @format int64
   * @example 21260124160
   */
  publicBytes?: number;
  /**
   * 私有可下載資產位元組總和。
   * @format int64
   * @example 50143481856
   */
  privateBytes?: number;
  /**
   * 孤兒檔數量（上傳未完成 / 處理失敗，保留期過後將清理）。
   * @format int32
   * @example 23
   */
  orphanFileCount?: number;
  /**
   * 孤兒檔位元組總和（含未確認大小者以 0 計）。
   * @format int64
   * @example 104857600
   */
  orphanBytes?: number;
  /** 用量前幾名的創作者明細（依位元組數遞減）。 */
  byCreator?: CreatorUsageDto[] | null;
}

export interface ProblemDetails {
  type?: string | null;
  title?: string | null;
  /** @format int32 */
  status?: number | null;
  detail?: string | null;
  instance?: string | null;
  [key: string]: any;
}

/** 向 StorageService 申請上傳簽章 URL 的請求體。 */
export interface RequestUploadUrlRequest {
  /**
   * 擁有者（創作者）ID。
   * @format uuid
   * @example "3fa85f64-5717-4562-b3fc-2c963f66afa6"
   */
  creatorId?: string;
  /**
   * 所屬商品 ID；null 表示尚未關聯商品（暫存後再關聯）。
   * @format uuid
   * @example "3fa85f64-5717-4562-b3fc-2c963f66afa6"
   */
  productId?: string | null;
  /**
   * 使用者上傳時的原始檔名（含副檔名）。
   * @example "intro-video.mp4"
   */
  originalName?: string | null;
  /**
   * MIME 類型；StorageService 驗證是否為允許格式。
   * @example "video/mp4"
   */
  contentType?: string | null;
  /**
   * 檔案大小（bytes）；用於配額檢查與 presigned URL 條件。
   * @format int64
   * @example 104857600
   */
  sizeBytes?: number;
  /** 媒體類型分類。 */
  fileType?: FileType;
  /**
   * 是否為公開預覽衍生檔。
   * @example false
   */
  isPreview?: boolean;
  /**
   * 是否為公開讀取物件（例如商店 Avatar/Banner）；true 時物件鍵值前綴為 "public/"。
   * @example false
   */
  isPublic?: boolean;
}

/** 上傳簽章 URL 回應。 */
export interface RequestUploadUrlResponse {
  /**
   * 已建立的檔案紀錄 ID；上傳完成後以此 ID 通知 storage-event。
   * @format uuid
   * @example "3fa85f64-5717-4562-b3fc-2c963f66afa6"
   */
  fileId?: string;
  /**
   * 前端應使用此 URL 以 HTTP PUT 直傳檔案，不得經過 API Server 轉傳。
   * @example "http://localhost:5171/v1/files/blob/creators/.../intro-video.mp4?expires=1735689600&sig=..."
   */
  uploadUrl?: string | null;
  /**
   * 在儲存後端的物件鍵值。
   * @example "public/3fa85f64-5717-4562-b3fc-2c963f66afa6/.../avatar.png"
   */
  storageKey?: string | null;
  /**
   * 公開讀取網址；僅 `IsPublic=true` 時提供。
   * @example "http://localhost:5171/v1/files/blob/public/3fa85f64-5717-4562-b3fc-2c963f66afa6/.../avatar.png"
   */
  publicUrl?: string | null;
  /**
   * 簽章 URL 過期時間（UTC）。
   * @format date-time
   */
  expiresAt?: string;
}

/** 租戶實際用量回應（每日對帳用，加總已 Ready 且已被使用（referenced）的檔案大小）。 */
export interface TenantUsageResponse {
  /**
   * 租戶（創作者）ID。
   * @format uuid
   * @example "3fa85f64-5717-4562-b3fc-2c963f66afa6"
   */
  creatorId?: string;
  /**
   * 該租戶已 Ready 且已被使用檔案的位元組總和。
   * @format int64
   * @example 1048576
   */
  totalBytes?: number;
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
 * @title StorageService
 * @version v1
 */
export class Api<SecurityDataType extends unknown> {
  http: HttpClient<SecurityDataType>;

  constructor(http: HttpClient<SecurityDataType>) {
    this.http = http;
  }

  blob = {
    /**
     * No description
     *
     * @tags Blob
     * @name Upload
     * @summary 接收客戶端直傳的檔案內容，寫入本地儲存後觸發處理 pipeline。
     * @request PUT:/v1/files/blob/{key}
     */
    upload: (
      key: string,
      query?: {
        /**
         * URL 到期時間（Unix 秒）。
         * @format int64
         */
        expires?: number;
        /** HMAC 簽章。 */
        sig?: string;
      },
      params: RequestParams = {},
    ) =>
      this.http.request<void, ProblemDetails>({
        path: `/v1/files/blob/${key}`,
        method: "PUT",
        query: query,
        ...params,
      }),

    /**
     * No description
     *
     * @tags Blob
     * @name Download
     * @summary 提供本地儲存檔案的下載；`public/` 前綴免簽章，其餘須帶有效簽章。
     * @request GET:/v1/files/blob/{key}
     */
    download: (
      key: string,
      query?: {
        /**
         * URL 到期時間（Unix 秒）；公開物件可省略。
         * @format int64
         */
        expires?: number;
        /** HMAC 簽章；公開物件可省略。 */
        sig?: string;
      },
      params: RequestParams = {},
    ) =>
      this.http.request<void, ProblemDetails>({
        path: `/v1/files/blob/${key}`,
        method: "GET",
        query: query,
        ...params,
      }),
  };
  files = {
    /**
     * @description 簽發階段不涉及配額：配額於使用者提交確認、功能 API 建立 reference 時才計量。 本地儲存於 blob 端點接收上傳後即觸發處理 pipeline；GCS 由 bucket notification 觸發， 皆無需客戶端另行回報。
     *
     * @tags Files
     * @name RequestUploadUrl
     * @summary 申請上傳簽章 URL（presigned PUT）。
     * @request POST:/v1/files/upload-url
     */
    requestUploadUrl: (
      data: RequestUploadUrlRequest,
      params: RequestParams = {},
    ) =>
      this.http.request<RequestUploadUrlResponse, ProblemDetails>({
        path: `/v1/files/upload-url`,
        method: "POST",
        body: data,
        type: ContentType.Json,
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags Files
     * @name GetTenantUsage
     * @summary 加總指定創作者已 Ready 且已被使用（referenced）檔案的位元組總和（QuotaService 每日對帳用）。
     * @request GET:/v1/files/usage
     */
    getTenantUsage: (
      query?: {
        /**
         * 創作者（租戶）ID。
         * @format uuid
         */
        creatorId?: string;
      },
      params: RequestParams = {},
    ) =>
      this.http.request<TenantUsageResponse, any>({
        path: `/v1/files/usage`,
        method: "GET",
        query: query,
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags Files
     * @name GetPlatformUsage
     * @summary 彙總全平台儲存用量（數量 / 大小 / 公開私有 / 孤兒檔 / 創作者明細）。僅 Admin 可操作。
     * @request GET:/v1/files/usage/summary
     */
    getPlatformUsage: (params: RequestParams = {}) =>
      this.http.request<PlatformUsageResponse, any>({
        path: `/v1/files/usage/summary`,
        method: "GET",
        format: "json",
        ...params,
      }),

    /**
     * @description 雲端（GCS）模式下，前端透過簽章 URL 直傳後由功能 API 呼叫此端點確認； StorageService 驗證物件確實存在後執行處理並發布 FileReadyEvent。 本地儲存由 blob 端點接收上傳時自動觸發，無需呼叫。冪等。
     *
     * @tags Files
     * @name ConfirmUpload
     * @summary 確認檔案已直傳完成，觸發處理 pipeline 並標記 Ready。
     * @request POST:/v1/files/{id}/confirm
     */
    confirmUpload: (id: string, params: RequestParams = {}) =>
      this.http.request<FileDto, ProblemDetails>({
        path: `/v1/files/${id}/confirm`,
        method: "POST",
        format: "json",
        ...params,
      }),

    /**
     * @description 功能 API 在使用者提交確認、完成配額計量並建立資產 reference 後呼叫。 僅 Ready 檔案可標記；未標記的檔案不計入配額，且逾期未被使用將由清理排程回收。冪等。
     *
     * @tags Files
     * @name MarkReferenced
     * @summary 標記檔案已被實際使用（建立 File reference）。
     * @request POST:/v1/files/{id}/reference
     */
    markReferenced: (id: string, params: RequestParams = {}) =>
      this.http.request<FileDto, ProblemDetails>({
        path: `/v1/files/${id}/reference`,
        method: "POST",
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags Files
     * @name Get
     * @summary 查詢檔案元資訊與處理狀態。
     * @request GET:/v1/files/{id}
     */
    get: (id: string, params: RequestParams = {}) =>
      this.http.request<FileDto, ProblemDetails>({
        path: `/v1/files/${id}`,
        method: "GET",
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags Files
     * @name Delete
     * @summary 軟刪除檔案；已購買的商品仍保留買家下載權。
     * @request DELETE:/v1/files/{id}
     */
    delete: (id: string, params: RequestParams = {}) =>
      this.http.request<void, ProblemDetails>({
        path: `/v1/files/${id}`,
        method: "DELETE",
        ...params,
      }),

    /**
     * @description 此端點不自行驗證買家是否擁有商品（entitlement check）， 應由功能 API 完成授權驗證後再呼叫此端點。 僅 Ready 狀態的檔案才能取得下載 URL。
     *
     * @tags Files
     * @name GetDownloadUrl
     * @summary 取得已授權的下載簽章 URL（presigned GET）。
     * @request GET:/v1/files/{id}/download-url
     */
    getDownloadUrl: (id: string, params: RequestParams = {}) =>
      this.http.request<GetDownloadUrlResponse, ProblemDetails>({
        path: `/v1/files/${id}/download-url`,
        method: "GET",
        format: "json",
        ...params,
      }),
  };
  storageService = {
    /**
     * No description
     *
     * @tags StorageService
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
}
