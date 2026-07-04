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

/** 訂單狀態。數位商品付款成功後即視為履約完成（Completed）。 */
export enum OrderStatus {
  Pending = "Pending",
  Paid = "Paid",
  Completed = "Completed",
  Cancelled = "Cancelled",
  Refunded = "Refunded",
}

/** 取消訂單請求。 */
export interface CancelOrderRequest {
  /**
   * 取消原因（選填）。
   * @example "改變心意"
   */
  reason?: string | null;
}

/** 建立訂單時的單一項目。數位商品數量固定為 1。 */
export interface CreateOrderItemRequest {
  /**
   * 商品 ID。
   * @format uuid
   * @example "3fa85f64-5717-4562-b3fc-2c963f66afa6"
   */
  catalogId?: string;
}

/** 建立訂單（結帳）請求。 */
export interface CreateOrderRequest {
  /**
   * 訂單所屬商店 ID（賣方）。
   * @format uuid
   * @example "3fa85f64-5717-4562-b3fc-2c963f66afa6"
   */
  storeId?: string;
  /**
   * 購買者電子信箱。
   * @example "buyer@example.com"
   */
  buyerEmail?: string | null;
  /** 訂單項目（至少一筆，商品不得重複）。名稱 / 單價 / 版本 / 幣別由伺服器端向 CatalogService 取得快照，不接受外部指定。 */
  items?: CreateOrderItemRequest[] | null;
}

/** 訂單列表分頁回應。 */
export interface ListOrdersResponse {
  /**
   * 符合條件的總筆數（未分頁）。
   * @format int32
   * @example 42
   */
  totalCount?: number;
  /** 本頁訂單清單。 */
  items?: OrderSummaryDto[] | null;
}

/** 訂單項目回應。 */
export interface OrderItemResponse {
  /**
   * 項目 ID。
   * @format uuid
   */
  id?: string;
  /**
   * 商品 ID。
   * @format uuid
   */
  catalogId?: string;
  /**
   * 商品版本 ID。
   * @format uuid
   */
  catalogVersionId?: string;
  /**
   * 商品名稱（下單快照）。
   * @example "復古 8-bit 音效包"
   */
  catalogName?: string | null;
  /**
   * 單價（最低貨幣單位）。
   * @format int64
   * @example 1990
   */
  unitPrice?: number;
}

/** 訂單完整資訊回應（含項目與狀態歷程）。 */
export interface OrderResponse {
  /**
   * 訂單 ID。
   * @format uuid
   */
  id?: string;
  /**
   * 人類可讀訂單編號。
   * @example "OJ-20260624-1A2B3C4D"
   */
  orderNumber?: string | null;
  /**
   * 訂單所屬商店 ID（賣方）。
   * @format uuid
   * @example "3fa85f64-5717-4562-b3fc-2c963f66afa6"
   */
  storeId?: string;
  /**
   * 購買者使用者 ID；null 表示匿名購買。
   * @format uuid
   */
  buyerUserId?: string | null;
  /**
   * 購買者電子信箱。
   * @example "buyer@example.com"
   */
  buyerEmail?: string | null;
  /** 訂單狀態。數位商品付款成功後即視為履約完成（Completed）。 */
  status?: OrderStatus;
  /**
   * 貨幣代碼。
   * @example "usd"
   */
  currency?: string | null;
  /**
   * 訂單總金額（最低貨幣單位）。
   * @format int64
   * @example 1990
   */
  totalAmount?: number;
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
  /**
   * 履約完成時間（UTC）；null 表示尚未完成。
   * @format date-time
   */
  completedAt?: string | null;
  /** 訂單項目。 */
  items?: OrderItemResponse[] | null;
  /** 狀態變更歷程（依時間排序）。 */
  statusHistory?: OrderStatusHistoryResponse[] | null;
  /**
   * Stripe Checkout 付款頁 URL；僅建立訂單（結帳）回應帶值，前端導向此 URL 完成付款。
   * @example "https://checkout.stripe.com/c/pay/cs_test_123"
   */
  checkoutUrl?: string | null;
}

/** 訂單狀態歷程回應。 */
export interface OrderStatusHistoryResponse {
  /** 訂單狀態。數位商品付款成功後即視為履約完成（Completed）。 */
  oldStatus?: OrderStatus;
  /** 訂單狀態。數位商品付款成功後即視為履約完成（Completed）。 */
  newStatus?: OrderStatus;
  /**
   * 變更原因。
   * @example "Payment succeeded"
   */
  reason?: string | null;
  /**
   * 變更時間（UTC）。
   * @format date-time
   */
  createdAt?: string;
}

/** 訂單列表項目（精簡）。 */
export interface OrderSummaryDto {
  /**
   * 訂單 ID。
   * @format uuid
   */
  id?: string;
  /**
   * 人類可讀訂單編號。
   * @example "OJ-20260624-1A2B3C4D"
   */
  orderNumber?: string | null;
  /**
   * 訂單所屬商店 ID（賣方）。
   * @format uuid
   * @example "3fa85f64-5717-4562-b3fc-2c963f66afa6"
   */
  storeId?: string;
  /** 訂單狀態。數位商品付款成功後即視為履約完成（Completed）。 */
  status?: OrderStatus;
  /**
   * 貨幣代碼。
   * @example "usd"
   */
  currency?: string | null;
  /**
   * 訂單總金額（最低貨幣單位）。
   * @format int64
   * @example 1990
   */
  totalAmount?: number;
  /**
   * 建立時間（UTC）。
   * @format date-time
   */
  createdAt?: string;
  /**
   * 履約完成時間（UTC）；null 表示尚未完成。
   * @format date-time
   */
  completedAt?: string | null;
}

/** 商品購買驗證回應。 */
export interface PurchaseCheckResponse {
  /**
   * 登入使用者是否曾以已完成訂單購買此商品。
   * @example true
   */
  purchased?: boolean;
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
 * @title OrderService
 * @version v1
 */
export class Api<SecurityDataType extends unknown> {
  http: HttpClient<SecurityDataType>;

  constructor(http: HttpClient<SecurityDataType>) {
    this.http = http;
  }

  orders = {
    /**
     * No description
     *
     * @tags Orders
     * @name Create
     * @summary 建立訂單（結帳）。消費者免註冊，憑 Email 即可下單；回應含 Stripe Checkout 付款頁 URL。
     * @request POST:/v1/orders
     */
    create: (data: CreateOrderRequest, params: RequestParams = {}) =>
      this.http.request<OrderResponse, any>({
        path: `/v1/orders`,
        method: "POST",
        body: data,
        type: ContentType.Json,
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags Orders
     * @name List
     * @summary 查詢全部訂單列表（含各狀態），可依商店 / 買家 / 狀態過濾。僅 Admin 可操作。
     * @request GET:/v1/orders
     */
    list: (
      query?: {
        /**
         * 限定所屬商店 ID（賣方）；null 表示不限。
         * @format uuid
         * @example "3fa85f64-5717-4562-b3fc-2c963f66afa6"
         */
        StoreId?: string;
        /**
         * 限定購買者使用者 ID；null 表示不限。
         * @format uuid
         * @example "3fa85f64-5717-4562-b3fc-2c963f66afa6"
         */
        BuyerUserId?: string;
        /**
         * 限定購買者 Email；null 表示不限。
         * @example "buyer@example.com"
         */
        BuyerEmail?: string;
        /**
         * 限定訂單狀態；null 表示不限。
         * @example "Completed"
         */
        Status?: OrderStatus;
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
      this.http.request<ListOrdersResponse, any>({
        path: `/v1/orders`,
        method: "GET",
        query: query,
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags Orders
     * @name Get
     * @summary 查詢訂單完整資訊（含項目與狀態歷程）。
     * @request GET:/v1/orders/{id}
     */
    get: (id: string, params: RequestParams = {}) =>
      this.http.request<OrderResponse, any>({
        path: `/v1/orders/${id}`,
        method: "GET",
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags Orders
     * @name GetByNumber
     * @summary 以訂單編號查詢訂單完整資訊。
     * @request GET:/v1/orders/by-number/{orderNumber}
     */
    getByNumber: (orderNumber: string, params: RequestParams = {}) =>
      this.http.request<OrderResponse, any>({
        path: `/v1/orders/by-number/${orderNumber}`,
        method: "GET",
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags Orders
     * @name ListMine
     * @summary 查詢登入使用者本人的訂單列表（分頁）。
     * @request GET:/v1/orders/mine
     */
    listMine: (
      query?: {
        /**
         * 限定所屬商店 ID（賣方）；null 表示不限。
         * @format uuid
         * @example "3fa85f64-5717-4562-b3fc-2c963f66afa6"
         */
        StoreId?: string;
        /**
         * 限定購買者使用者 ID；null 表示不限。
         * @format uuid
         * @example "3fa85f64-5717-4562-b3fc-2c963f66afa6"
         */
        BuyerUserId?: string;
        /**
         * 限定購買者 Email；null 表示不限。
         * @example "buyer@example.com"
         */
        BuyerEmail?: string;
        /**
         * 限定訂單狀態；null 表示不限。
         * @example "Completed"
         */
        Status?: OrderStatus;
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
      this.http.request<ListOrdersResponse, any>({
        path: `/v1/orders/mine`,
        method: "GET",
        query: query,
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags Orders
     * @name ListByStore
     * @summary 查詢指定商店收到的訂單列表（賣家視角，分頁）。僅該商店 Owner 可操作。
     * @request GET:/v1/orders/store/{storeId}
     */
    listByStore: (
      storeId: string,
      query?: {
        /**
         * 限定所屬商店 ID（賣方）；null 表示不限。
         * @format uuid
         * @example "3fa85f64-5717-4562-b3fc-2c963f66afa6"
         */
        StoreId?: string;
        /**
         * 限定購買者使用者 ID；null 表示不限。
         * @format uuid
         * @example "3fa85f64-5717-4562-b3fc-2c963f66afa6"
         */
        BuyerUserId?: string;
        /**
         * 限定購買者 Email；null 表示不限。
         * @example "buyer@example.com"
         */
        BuyerEmail?: string;
        /**
         * 限定訂單狀態；null 表示不限。
         * @example "Completed"
         */
        Status?: OrderStatus;
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
      this.http.request<ListOrdersResponse, any>({
        path: `/v1/orders/store/${storeId}`,
        method: "GET",
        query: query,
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags Orders
     * @name HasPurchased
     * @summary 查詢登入使用者是否已購買（已完成訂單）某商品。供評論前的購買驗證。
     * @request GET:/v1/orders/purchased/{catalogId}
     */
    hasPurchased: (catalogId: string, params: RequestParams = {}) =>
      this.http.request<PurchaseCheckResponse, any>({
        path: `/v1/orders/purchased/${catalogId}`,
        method: "GET",
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags Orders
     * @name Cancel
     * @summary 取消未付款的訂單。具名買家僅本人可取消；匿名訂單憑訂單 ID 取消。
     * @request POST:/v1/orders/{id}/cancel
     */
    cancel: (
      id: string,
      data: CancelOrderRequest,
      params: RequestParams = {},
    ) =>
      this.http.request<OrderResponse, any>({
        path: `/v1/orders/${id}/cancel`,
        method: "POST",
        body: data,
        type: ContentType.Json,
        format: "json",
        ...params,
      }),
  };
  orderService = {
    /**
     * No description
     *
     * @tags OrderService
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
