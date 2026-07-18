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

export enum PaymentStatus {
  Pending = "Pending",
  Processing = "Processing",
  Succeeded = "Succeeded",
  Failed = "Failed",
  Cancelled = "Cancelled",
  Expired = "Expired",
}

/** Stripe Connect onboarding Account Link 回應。 */
export interface AccountLinkResponse {
  /**
   * Stripe 託管 onboarding 頁 URL（短效，前端直接導向）。
   * @example "https://connect.stripe.com/setup/e/acct_xxx/yyy"
   */
  url?: string | null;
}

/** Checkout Session 建立回應。 */
export interface CheckoutSessionResponse {
  /**
   * 付款 ID。
   * @format uuid
   */
  paymentId?: string;
  /** Stripe Checkout Session ID。 */
  sessionId?: string | null;
  /** Stripe Checkout URL（前端導向此 URL 完成付款）。 */
  url?: string | null;
}

/** 商店 Stripe Connect 帳戶狀態。 */
export interface ConnectAccountStatusResponse {
  /**
   * 商店是否已建立 Stripe 連接帳戶。
   * @example true
   */
  hasAccount?: boolean;
  /**
   * 創作者是否已提交 onboarding 資料。
   * @example true
   */
  detailsSubmitted?: boolean;
  /**
   * 帳戶是否可承接款項（付費商品上架與分帳的閘門依據）。
   * @example true
   */
  chargesEnabled?: boolean;
  /**
   * 帳戶是否可提領出金。
   * @example true
   */
  payoutsEnabled?: boolean;
}

/** 建立 Stripe Checkout Session 請求。 */
export interface CreateCheckoutSessionRequest {
  /**
   * 商品訂單 ID。
   * @format uuid
   */
  orderId?: string;
  /**
   * 賣方商店 ID，用於查找分帳目的地 Stripe 帳戶。
   * @format uuid
   * @example "3fa85f64-5717-4562-b3fc-2c963f66afa6"
   */
  storeId?: string;
  /**
   * 購買者使用者 ID；null 表示匿名購買。由 OrderService 帶入（呼叫者為內部服務，非買家本人）。
   * @format uuid
   * @example "3fa85f64-5717-4562-b3fc-2c963f66afa6"
   */
  userId?: string | null;
  /** 購買者電子信箱。 */
  email?: string | null;
  /**
   * 付款金額（最低貨幣單位，如 cents）。
   * @format int64
   */
  amount?: number;
  /** 貨幣代碼（小寫，如 "usd"、"twd"）。 */
  currency?: string | null;
  /** 商品名稱（顯示於 Stripe Checkout 頁面）。 */
  productName?: string | null;
}

/** 付款列表分頁回應。 */
export interface ListPaymentsResponse {
  /**
   * 符合條件的總筆數（未分頁）。
   * @format int32
   * @example 42
   */
  totalCount?: number;
  /** 本頁付款清單。 */
  items?: PaymentResponse[] | null;
}

/** 付款紀錄回應。 */
export interface PaymentResponse {
  /** @format uuid */
  id?: string;
  /** @format uuid */
  orderId?: string;
  /** @format uuid */
  storeId?: string;
  /** @format uuid */
  userId?: string | null;
  email?: string | null;
  provider?: string | null;
  /** @format int64 */
  amount?: number;
  /** @format int64 */
  applicationFeeAmount?: number;
  destinationAccountId?: string | null;
  currency?: string | null;
  status?: PaymentStatus;
  providerPaymentId?: string | null;
  providerCheckoutId?: string | null;
  /** @format date-time */
  createdAt?: string;
  /** @format date-time */
  paidAt?: string | null;
  /** @format date-time */
  failedAt?: string | null;
  /** @format date-time */
  expiresAt?: string | null;
  /** @format date-time */
  expiredAt?: string | null;
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
 * @title PaymentService
 * @version v1
 */
export class Api<SecurityDataType extends unknown> {
  http: HttpClient<SecurityDataType>;

  constructor(http: HttpClient<SecurityDataType>) {
    this.http = http;
  }

  connectAccounts = {
    /**
     * No description
     *
     * @tags ConnectAccounts
     * @name CreateOnboardingLink
     * @summary 為商店建立（或重用）Stripe Express 帳戶並簽發 onboarding Account Link。僅商店 Owner 可操作。
     * @request POST:/v1/connect/accounts/{storeId}/onboarding-link
     */
    createOnboardingLink: (storeId: string, params: RequestParams = {}) =>
      this.http.request<AccountLinkResponse, any>({
        path: `/v1/connect/accounts/${storeId}/onboarding-link`,
        method: "POST",
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags ConnectAccounts
     * @name CreateLoginLink
     * @summary 簽發商店 Stripe Express Dashboard 登入連結（短效，前端直接開啟）。僅商店 Owner 可操作； 創作者於 Stripe 託管頁查看餘額、撥款排程與交易明細。
     * @request POST:/v1/connect/accounts/{storeId}/login-link
     */
    createLoginLink: (storeId: string, params: RequestParams = {}) =>
      this.http.request<AccountLinkResponse, any>({
        path: `/v1/connect/accounts/${storeId}/login-link`,
        method: "POST",
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags ConnectAccounts
     * @name Get
     * @summary 查詢商店連接帳戶狀態。僅商店 Owner 可操作；refresh 時向 Stripe 取即時狀態。
     * @request GET:/v1/connect/accounts/{storeId}
     */
    get: (
      storeId: string,
      query?: {
        /** 是否向 Stripe 取得即時狀態並回寫（onboarding 導回頁使用）。 */
        refresh?: boolean;
      },
      params: RequestParams = {},
    ) =>
      this.http.request<ConnectAccountStatusResponse, any>({
        path: `/v1/connect/accounts/${storeId}`,
        method: "GET",
        query: query,
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags ConnectAccounts
     * @name GetStatus
     * @summary 查詢商店收款狀態（僅布林旗標）。匿名公開，供 CatalogService 上架閘門與前端顯示。
     * @request GET:/v1/connect/accounts/{storeId}/status
     */
    getStatus: (storeId: string, params: RequestParams = {}) =>
      this.http.request<ConnectAccountStatusResponse, any>({
        path: `/v1/connect/accounts/${storeId}/status`,
        method: "GET",
        format: "json",
        ...params,
      }),
  };
  payments = {
    /**
     * No description
     *
     * @tags Payments
     * @name CreateCheckoutSession
     * @summary 建立（或重用）Stripe Checkout Session。僅限內部服務（OrderService）以 service token 呼叫。
     * @request POST:/v1/payments/checkout-session
     */
    createCheckoutSession: (
      data: CreateCheckoutSessionRequest,
      params: RequestParams = {},
    ) =>
      this.http.request<CheckoutSessionResponse, any>({
        path: `/v1/payments/checkout-session`,
        method: "POST",
        body: data,
        type: ContentType.Json,
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags Payments
     * @name ExpireByOrder
     * @summary 作廢訂單既有的 Stripe Checkout Session（取消訂單前呼叫，付款頁立即失效）。 訂單已完成付款回 409（呼叫端應拒絕取消）；無 Pending 付款時冪等視為成功。 僅限內部服務（OrderService）以 service token 呼叫。
     * @request POST:/v1/payments/expire-by-order/{orderId}
     */
    expireByOrder: (orderId: string, params: RequestParams = {}) =>
      this.http.request<void, ProblemDetails>({
        path: `/v1/payments/expire-by-order/${orderId}`,
        method: "POST",
        ...params,
      }),

    /**
     * No description
     *
     * @tags Payments
     * @name List
     * @summary 付款紀錄分頁列表（可依狀態 / 商店 / 訂單 / 買家信箱 / 時間區間過濾）。僅 Admin 可操作， 供管理員後台對帳與客訴查案。
     * @request GET:/v1/payments
     */
    list: (
      query?: {
        /**
         * 限定付款狀態；null 表示不限。
         * @example "Succeeded"
         */
        Status?: PaymentStatus;
        /**
         * 限定賣方商店 ID；null 表示不限。
         * @format uuid
         * @example "3fa85f64-5717-4562-b3fc-2c963f66afa6"
         */
        StoreId?: string;
        /**
         * 限定商品訂單 ID；null 表示不限。
         * @format uuid
         * @example "3fa85f64-5717-4562-b3fc-2c963f66afa6"
         */
        OrderId?: string;
        /**
         * 限定購買者 Email（完全比對，不分大小寫）；null 表示不限。
         * @example "buyer@example.com"
         */
        Email?: string;
        /**
         * 建立時間下限（UTC，含）；null 表示不限。
         * @format date-time
         */
        From?: string;
        /**
         * 建立時間上限（UTC，含）；null 表示不限。
         * @format date-time
         */
        To?: string;
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
      this.http.request<ListPaymentsResponse, any>({
        path: `/v1/payments`,
        method: "GET",
        query: query,
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags Payments
     * @name Get
     * @summary 查詢付款紀錄。僅 Admin 可操作。
     * @request GET:/v1/payments/{id}
     */
    get: (id: string, params: RequestParams = {}) =>
      this.http.request<PaymentResponse, any>({
        path: `/v1/payments/${id}`,
        method: "GET",
        format: "json",
        ...params,
      }),
  };
  paymentService = {
    /**
     * No description
     *
     * @tags PaymentService
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
