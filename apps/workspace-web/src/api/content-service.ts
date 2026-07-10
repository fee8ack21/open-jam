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

/** 法律文件類型。 */
export enum LegalDocumentType {
  TermsOfService = "TermsOfService",
  PrivacyPolicy = "PrivacyPolicy",
}

/** 法律文件狀態。文件不可刪除，停用後仍保留於資料庫供歷史比對。 */
export enum LegalDocumentStatus {
  Draft = "Draft",
  Active = "Active",
  Inactive = "Inactive",
}

/** 建立常見問題主題分類請求（平台維護）。 */
export interface CreateFaqCategoryRequest {
  /**
   * 分類名稱（1–100 字）。
   * @example "認識平台"
   */
  name?: string | null;
  /**
   * 分類代稱（全域唯一，3–100 字小寫英數字與連字號）。
   * @example "platform"
   */
  slug?: string | null;
  /**
   * 分類補充敘述（選填，最多 200 字）。
   * @example "關於 Open Jam 平台的基本介紹"
   */
  description?: string | null;
  /**
   * 分頁顯示排序。
   * @format int32
   * @example 0
   */
  sortOrder?: number;
}

/** 建立常見問題項目請求。 */
export interface CreateFaqItemRequest {
  /**
   * 所屬主題分類 ID。
   * @format uuid
   * @example "3fa85f64-5717-4562-b3fc-2c963f66afa6"
   */
  categoryId?: string;
  /**
   * 問題。
   * @example "Open Jam 是什麼？"
   */
  question?: string | null;
  /**
   * 解答。
   * @example "Open Jam 是台灣的數位商品平台。"
   */
  answer?: string | null;
  /**
   * 同分類內的顯示排序（升冪）。
   * @format int32
   * @example 0
   */
  sortOrder?: number;
  /**
   * 是否已發布（對外公開）；預設為 true。
   * @example true
   */
  isPublished?: boolean;
}

/** 建立法律文件草稿請求；版本序號由伺服器依同類型現有最大版本 +1 產生。 */
export interface CreateLegalDocumentRequest {
  /** 法律文件類型。 */
  type?: LegalDocumentType;
  /**
   * 文件標題。
   * @example "服務條款"
   */
  title?: string | null;
  /**
   * 文件內容（純文字；「## 」開頭為章節標題、「- 」開頭為列點）。
   * @example "## 歡迎加入 Open Jam"
   */
  content?: string | null;
}

/** 常見問題主題分類回應。 */
export interface FaqCategoryDto {
  /**
   * 分類唯一識別碼。
   * @format uuid
   * @example "3fa85f64-5717-4562-b3fc-2c963f66afa6"
   */
  id?: string;
  /**
   * 分類名稱。
   * @example "認識平台"
   */
  name?: string | null;
  /**
   * 分類代稱（全域唯一）。
   * @example "platform"
   */
  slug?: string | null;
  /**
   * 分類補充敘述；null 表示未設定。
   * @example "關於 Open Jam 平台的基本介紹"
   */
  description?: string | null;
  /**
   * 分頁顯示排序。
   * @format int32
   * @example 0
   */
  sortOrder?: number;
}

/** 常見問題項目。 */
export interface FaqItemDto {
  /**
   * 項目唯一識別碼。
   * @format uuid
   * @example "3fa85f64-5717-4562-b3fc-2c963f66afa6"
   */
  id?: string;
  /**
   * 所屬主題分類 ID。
   * @format uuid
   * @example "3fa85f64-5717-4562-b3fc-2c963f66afa6"
   */
  categoryId?: string;
  /**
   * 所屬主題分類名稱。
   * @example "認識平台"
   */
  categoryName?: string | null;
  /**
   * 所屬主題分類代稱。
   * @example "platform"
   */
  categorySlug?: string | null;
  /**
   * 問題。
   * @example "Open Jam 是什麼？"
   */
  question?: string | null;
  /**
   * 解答。
   * @example "Open Jam 是台灣的數位商品平台。"
   */
  answer?: string | null;
  /**
   * 同分類內的顯示排序（升冪）。
   * @format int32
   * @example 0
   */
  sortOrder?: number;
  /**
   * 是否已發布（對外公開）。
   * @example true
   */
  isPublished?: boolean;
  /**
   * 建立時間（UTC）。
   * @format date-time
   * @example "2026-06-30T08:00:00Z"
   */
  createdAt?: string;
  /**
   * 最後更新時間（UTC）；null 表示自建立後未變更。
   * @format date-time
   * @example "2026-07-01T00:00:00Z"
   */
  updatedAt?: string | null;
}

/** 法律文件完整內容（單筆查詢 / 公開撈取用）。 */
export interface LegalDocumentDto {
  /**
   * 文件唯一識別碼。
   * @format uuid
   * @example "3fa85f64-5717-4562-b3fc-2c963f66afa6"
   */
  id?: string;
  /** 法律文件類型。 */
  type?: LegalDocumentType;
  /**
   * 版本序號（同類型內遞增）。
   * @format int32
   * @example 2
   */
  version?: number;
  /**
   * 文件標題。
   * @example "服務條款"
   */
  title?: string | null;
  /** 法律文件狀態。文件不可刪除，停用後仍保留於資料庫供歷史比對。 */
  status?: LegalDocumentStatus;
  /**
   * 最近一次啟用時間（UTC）；null 表示從未啟用。
   * @format date-time
   * @example "2026-07-01T00:00:00Z"
   */
  activatedAt?: string | null;
  /**
   * 建立時間（UTC）。
   * @format date-time
   * @example "2026-06-30T08:00:00Z"
   */
  createdAt?: string;
  /**
   * 最後更新時間（UTC）；null 表示自建立後未變更。
   * @format date-time
   * @example "2026-07-01T00:00:00Z"
   */
  updatedAt?: string | null;
  /**
   * 文件內容（純文字；「## 」開頭為章節標題、「- 」開頭為列點）。
   * @example "## 歡迎加入 Open Jam"
   */
  content?: string | null;
}

/** 法律文件摘要（列表用，不含完整內容）。 */
export interface LegalDocumentSummaryDto {
  /**
   * 文件唯一識別碼。
   * @format uuid
   * @example "3fa85f64-5717-4562-b3fc-2c963f66afa6"
   */
  id?: string;
  /** 法律文件類型。 */
  type?: LegalDocumentType;
  /**
   * 版本序號（同類型內遞增）。
   * @format int32
   * @example 2
   */
  version?: number;
  /**
   * 文件標題。
   * @example "服務條款"
   */
  title?: string | null;
  /** 法律文件狀態。文件不可刪除，停用後仍保留於資料庫供歷史比對。 */
  status?: LegalDocumentStatus;
  /**
   * 最近一次啟用時間（UTC）；null 表示從未啟用。
   * @format date-time
   * @example "2026-07-01T00:00:00Z"
   */
  activatedAt?: string | null;
  /**
   * 建立時間（UTC）。
   * @format date-time
   * @example "2026-06-30T08:00:00Z"
   */
  createdAt?: string;
  /**
   * 最後更新時間（UTC）；null 表示自建立後未變更。
   * @format date-time
   * @example "2026-07-01T00:00:00Z"
   */
  updatedAt?: string | null;
}

/** 常見問題分頁查詢回應。 */
export interface ListFaqItemsResponse {
  /**
   * 符合條件的總筆數（未分頁）。
   * @format int32
   * @example 12
   */
  totalCount?: number;
  /** 本頁項目清單。 */
  items?: FaqItemDto[] | null;
}

/** 法律文件分頁查詢回應。 */
export interface ListLegalDocumentsResponse {
  /**
   * 符合條件的總筆數（未分頁）。
   * @format int32
   * @example 6
   */
  totalCount?: number;
  /** 本頁文件清單。 */
  items?: LegalDocumentSummaryDto[] | null;
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

/** 更新常見問題主題分類請求（部分欄位，null 表示不變更）。 */
export interface UpdateFaqCategoryRequest {
  /**
   * 分類名稱；null 表示不變更。
   * @example "認識平台"
   */
  name?: string | null;
  /**
   * 分類代稱；null 表示不變更。
   * @example "platform"
   */
  slug?: string | null;
  /**
   * 分類補充敘述；null 表示不變更。
   * @example "關於 Open Jam 平台的基本介紹"
   */
  description?: string | null;
  /**
   * 分頁顯示排序；null 表示不變更。
   * @format int32
   * @example 1
   */
  sortOrder?: number | null;
}

/** 更新常見問題項目請求。 */
export interface UpdateFaqItemRequest {
  /**
   * 所屬主題分類 ID。
   * @format uuid
   * @example "3fa85f64-5717-4562-b3fc-2c963f66afa6"
   */
  categoryId?: string;
  /**
   * 問題。
   * @example "Open Jam 是什麼？"
   */
  question?: string | null;
  /**
   * 解答。
   * @example "Open Jam 是台灣的數位商品平台。"
   */
  answer?: string | null;
  /**
   * 同分類內的顯示排序（升冪）。
   * @format int32
   * @example 0
   */
  sortOrder?: number;
  /**
   * 是否已發布（對外公開）。
   * @example true
   */
  isPublished?: boolean;
}

/** 更新法律文件草稿請求；僅 Draft 狀態可更新。 */
export interface UpdateLegalDocumentRequest {
  /**
   * 文件標題。
   * @example "服務條款"
   */
  title?: string | null;
  /**
   * 文件內容（純文字；「## 」開頭為章節標題、「- 」開頭為列點）。
   * @example "## 歡迎加入 Open Jam"
   */
  content?: string | null;
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
 * @title ContentService
 * @version v1
 */
export class Api<SecurityDataType extends unknown> {
  http: HttpClient<SecurityDataType>;

  constructor(http: HttpClient<SecurityDataType>) {
    this.http = http;
  }

  contentService = {
    /**
     * No description
     *
     * @tags ContentService
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
  faqCategories = {
    /**
     * No description
     *
     * @tags FaqCategories
     * @name List
     * @summary 列出所有分類（匿名公開，依排序）。
     * @request GET:/v1/faq-categories
     */
    list: (params: RequestParams = {}) =>
      this.http.request<FaqCategoryDto[], any>({
        path: `/v1/faq-categories`,
        method: "GET",
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags FaqCategories
     * @name Create
     * @summary 建立分類。僅 Admin 可操作。
     * @request POST:/v1/faq-categories
     */
    create: (data: CreateFaqCategoryRequest, params: RequestParams = {}) =>
      this.http.request<FaqCategoryDto, any>({
        path: `/v1/faq-categories`,
        method: "POST",
        body: data,
        type: ContentType.Json,
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags FaqCategories
     * @name Get
     * @summary 查詢單一分類。僅 Admin 可存取。
     * @request GET:/v1/faq-categories/{id}
     */
    get: (id: string, params: RequestParams = {}) =>
      this.http.request<FaqCategoryDto, ProblemDetails>({
        path: `/v1/faq-categories/${id}`,
        method: "GET",
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags FaqCategories
     * @name Update
     * @summary 更新分類。僅 Admin 可操作。
     * @request PATCH:/v1/faq-categories/{id}
     */
    update: (
      id: string,
      data: UpdateFaqCategoryRequest,
      params: RequestParams = {},
    ) =>
      this.http.request<FaqCategoryDto, any>({
        path: `/v1/faq-categories/${id}`,
        method: "PATCH",
        body: data,
        type: ContentType.Json,
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags FaqCategories
     * @name Delete
     * @summary 刪除分類（不可仍被常見問題項目引用）。僅 Admin 可操作。
     * @request DELETE:/v1/faq-categories/{id}
     */
    delete: (id: string, params: RequestParams = {}) =>
      this.http.request<void, any>({
        path: `/v1/faq-categories/${id}`,
        method: "DELETE",
        ...params,
      }),
  };
  faqs = {
    /**
     * No description
     *
     * @tags Faqs
     * @name List
     * @summary 查詢常見問題（分頁，支援分類 / 發布狀態篩選）。
     * @request GET:/v1/faqs
     */
    list: (
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
        /**
         * 過濾主題分類 ID；null 表示不限。
         * @format uuid
         * @example "3fa85f64-5717-4562-b3fc-2c963f66afa6"
         */
        CategoryId?: string;
        /**
         * 過濾發布狀態；null 表示不限。
         * @example true
         */
        IsPublished?: boolean;
      },
      params: RequestParams = {},
    ) =>
      this.http.request<ListFaqItemsResponse, any>({
        path: `/v1/faqs`,
        method: "GET",
        query: query,
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags Faqs
     * @name Create
     * @summary 建立常見問題項目。
     * @request POST:/v1/faqs
     */
    create: (data: CreateFaqItemRequest, params: RequestParams = {}) =>
      this.http.request<FaqItemDto, any>({
        path: `/v1/faqs`,
        method: "POST",
        body: data,
        type: ContentType.Json,
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags Faqs
     * @name GetPublished
     * @summary 取得已發布的常見問題（匿名公開，依分類與排序）。
     * @request GET:/v1/faqs/published
     */
    getPublished: (
      query?: {
        /**
         * 主題分類 ID；不帶時回傳所有分類。
         * @format uuid
         */
        categoryId?: string;
      },
      params: RequestParams = {},
    ) =>
      this.http.request<FaqItemDto[], any>({
        path: `/v1/faqs/published`,
        method: "GET",
        query: query,
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags Faqs
     * @name Get
     * @summary 取得單筆常見問題。
     * @request GET:/v1/faqs/{id}
     */
    get: (id: string, params: RequestParams = {}) =>
      this.http.request<FaqItemDto, ProblemDetails>({
        path: `/v1/faqs/${id}`,
        method: "GET",
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags Faqs
     * @name Update
     * @summary 更新常見問題項目。
     * @request PUT:/v1/faqs/{id}
     */
    update: (
      id: string,
      data: UpdateFaqItemRequest,
      params: RequestParams = {},
    ) =>
      this.http.request<FaqItemDto, ProblemDetails>({
        path: `/v1/faqs/${id}`,
        method: "PUT",
        body: data,
        type: ContentType.Json,
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags Faqs
     * @name Delete
     * @summary 刪除常見問題項目。
     * @request DELETE:/v1/faqs/{id}
     */
    delete: (id: string, params: RequestParams = {}) =>
      this.http.request<void, ProblemDetails>({
        path: `/v1/faqs/${id}`,
        method: "DELETE",
        ...params,
      }),
  };
  legalDocuments = {
    /**
     * No description
     *
     * @tags LegalDocuments
     * @name List
     * @summary 查詢法律文件（分頁，支援類型 / 狀態篩選）。
     * @request GET:/v1/legal-documents
     */
    list: (
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
        /**
         * 過濾文件類型；null 表示不限。
         * @example "TermsOfService"
         */
        Type?: LegalDocumentType;
        /**
         * 過濾文件狀態；null 表示不限。
         * @example "Active"
         */
        Status?: LegalDocumentStatus;
      },
      params: RequestParams = {},
    ) =>
      this.http.request<ListLegalDocumentsResponse, any>({
        path: `/v1/legal-documents`,
        method: "GET",
        query: query,
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags LegalDocuments
     * @name Create
     * @summary 建立法律文件草稿；版本序號由伺服器依同類型現有最大版本 +1 產生。
     * @request POST:/v1/legal-documents
     */
    create: (data: CreateLegalDocumentRequest, params: RequestParams = {}) =>
      this.http.request<LegalDocumentDto, any>({
        path: `/v1/legal-documents`,
        method: "POST",
        body: data,
        type: ContentType.Json,
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags LegalDocuments
     * @name GetActive
     * @summary 取得目前啟用中的法律文件內容（匿名公開）。
     * @request GET:/v1/legal-documents/active
     */
    getActive: (
      query?: {
        /** 文件類型；不帶時回傳所有類型的啟用版本。 */
        type?: LegalDocumentType;
      },
      params: RequestParams = {},
    ) =>
      this.http.request<LegalDocumentDto[], any>({
        path: `/v1/legal-documents/active`,
        method: "GET",
        query: query,
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags LegalDocuments
     * @name Get
     * @summary 取得單筆法律文件完整內容。
     * @request GET:/v1/legal-documents/{id}
     */
    get: (id: string, params: RequestParams = {}) =>
      this.http.request<LegalDocumentDto, ProblemDetails>({
        path: `/v1/legal-documents/${id}`,
        method: "GET",
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags LegalDocuments
     * @name Update
     * @summary 更新法律文件草稿的標題與內容；僅 Draft 狀態可更新。
     * @request PUT:/v1/legal-documents/{id}
     */
    update: (
      id: string,
      data: UpdateLegalDocumentRequest,
      params: RequestParams = {},
    ) =>
      this.http.request<LegalDocumentDto, ProblemDetails>({
        path: `/v1/legal-documents/${id}`,
        method: "PUT",
        body: data,
        type: ContentType.Json,
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags LegalDocuments
     * @name Activate
     * @summary 啟用文件（Draft / Inactive → Active）；同類型既有啟用版本自動轉為 Inactive。
     * @request POST:/v1/legal-documents/{id}/activate
     */
    activate: (id: string, params: RequestParams = {}) =>
      this.http.request<LegalDocumentDto, ProblemDetails>({
        path: `/v1/legal-documents/${id}/activate`,
        method: "POST",
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags LegalDocuments
     * @name Deactivate
     * @summary 停用啟用中的文件（Active → Inactive）。
     * @request POST:/v1/legal-documents/{id}/deactivate
     */
    deactivate: (id: string, params: RequestParams = {}) =>
      this.http.request<LegalDocumentDto, ProblemDetails>({
        path: `/v1/legal-documents/${id}/deactivate`,
        method: "POST",
        format: "json",
        ...params,
      }),
  };
}
