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

/** 通知任務狀態。 */
export enum NotificationRequestStatus {
  Pending = "Pending",
  Dispatched = "Dispatched",
  Cancelled = "Cancelled",
  Failed = "Failed",
}

/** 建立商店公告通知任務（可預定發送時間）。 */
export interface CreateNotificationRequestRequest {
  /**
   * 通知對象商店 ID（fan-out 至該商店的追蹤者）。僅商店 Owner 可建立。
   * @format uuid
   * @example "3fa85f64-5717-4562-b3fc-2c963f66afa6"
   */
  storeId?: string;
  /**
   * 公告標題。
   * @example "夏季特賣開跑"
   */
  title?: string | null;
  /**
   * 公告內文。
   * @example "全店素材 8 折，只到月底！"
   */
  message?: string | null;
  /**
   * 預定發送時間；null 表示立即發送。
   * @format date-time
   */
  scheduledAt?: string | null;
}

/** 商店通知任務列表回應。 */
export interface ListNotificationRequestsResponse {
  /**
   * 符合條件的總筆數。
   * @format int32
   * @example 7
   */
  totalCount?: number;
  /** 本頁任務。 */
  items?: NotificationRequestDto[] | null;
}

/** 本人通知列表回應。 */
export interface ListNotificationsResponse {
  /**
   * 符合條件的總筆數。
   * @format int32
   * @example 42
   */
  totalCount?: number;
  /** 本頁通知。 */
  items?: NotificationDto[] | null;
}

/** In-app 通知單筆資料。 */
export interface NotificationDto {
  /**
   * 通知 ID。
   * @format uuid
   * @example "3fa85f64-5717-4562-b3fc-2c963f66afa6"
   */
  id?: string;
  /**
   * 通知類型，前端據此搭配 Payload 渲染。
   * @example "catalog.published"
   */
  type?: string | null;
  /**
   * 通知內容參數（JSON 字串，camelCase，依 Type 決定結構）。
   * @example "{"catalogId":"3fa85f64-5717-4562-b3fc-2c963f66afa6","catalogName":"插畫素材包","storeName":"小明的數位商店"}"
   */
  payload?: string | null;
  /**
   * 已讀時間；null 表示未讀。
   * @format date-time
   */
  readAt?: string | null;
  /**
   * 通知建立時間。
   * @format date-time
   */
  createdAt?: string;
}

/** 通知任務單筆資料。 */
export interface NotificationRequestDto {
  /**
   * 通知任務 ID。
   * @format uuid
   * @example "3fa85f64-5717-4562-b3fc-2c963f66afa6"
   */
  id?: string;
  /**
   * 通知類型。
   * @example "store.announcement"
   */
  type?: string | null;
  /**
   * 通知對象商店 ID。
   * @format uuid
   * @example "3fa85f64-5717-4562-b3fc-2c963f66afa6"
   */
  storeId?: string;
  /**
   * 通知內容參數（JSON 字串，camelCase，依 Type 決定結構）。
   * @example "{"title":"夏季特賣開跑","message":"全店素材 8 折，只到月底！"}"
   */
  payload?: string | null;
  /**
   * 預定發送時間。
   * @format date-time
   */
  scheduledAt?: string;
  /** 通知任務狀態。 */
  status?: NotificationRequestStatus;
  /**
   * 完成 fan-out 的時間；null 表示尚未發送。
   * @format date-time
   */
  dispatchedAt?: string | null;
  /**
   * 建立時間。
   * @format date-time
   */
  createdAt?: string;
}

/** 未讀通知數回應。 */
export interface UnreadCountResponse {
  /**
   * 未讀通知數。
   * @format int32
   * @example 3
   */
  count?: number;
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
 * @title NotificationService
 * @version v1
 */
export class Api<SecurityDataType extends unknown> {
  http: HttpClient<SecurityDataType>;

  constructor(http: HttpClient<SecurityDataType>) {
    this.http = http;
  }

  notificationRequests = {
    /**
     * No description
     *
     * @tags NotificationRequests
     * @name Create
     * @summary 建立商店公告通知任務；ScheduledAt 為 null 表示立即發送。
     * @request POST:/v1/notification-requests
     */
    create: (
      data: CreateNotificationRequestRequest,
      params: RequestParams = {},
    ) =>
      this.http.request<NotificationRequestDto, any>({
        path: `/v1/notification-requests`,
        method: "POST",
        body: data,
        type: ContentType.Json,
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags NotificationRequests
     * @name List
     * @summary 查詢商店的通知任務列表（分頁，可依狀態過濾）。
     * @request GET:/v1/notification-requests
     */
    list: (
      query?: {
        /**
         * 商店 ID（必填，僅該商店 Owner 可查詢）。
         * @format uuid
         * @example "3fa85f64-5717-4562-b3fc-2c963f66afa6"
         */
        StoreId?: string;
        /**
         * 以任務狀態過濾；null 表示不過濾。
         * @example "Pending"
         */
        Status?: NotificationRequestStatus;
        /**
         * 略過筆數。
         * @format int32
         * @example 0
         */
        Offset?: number;
        /**
         * 取回筆數上限。
         * @format int32
         * @example 20
         */
        Limit?: number;
      },
      params: RequestParams = {},
    ) =>
      this.http.request<ListNotificationRequestsResponse, any>({
        path: `/v1/notification-requests`,
        method: "GET",
        query: query,
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags NotificationRequests
     * @name Cancel
     * @summary 取消尚未發送的通知任務（僅 Pending 可取消）。
     * @request DELETE:/v1/notification-requests/{id}
     */
    cancel: (id: string, params: RequestParams = {}) =>
      this.http.request<void, any>({
        path: `/v1/notification-requests/${id}`,
        method: "DELETE",
        ...params,
      }),
  };
  notifications = {
    /**
     * No description
     *
     * @tags Notifications
     * @name ListMine
     * @summary 查詢登入使用者本人的通知列表（分頁，可僅未讀）。
     * @request GET:/v1/notifications/mine
     */
    listMine: (
      query?: {
        /**
         * 略過筆數。
         * @format int32
         * @example 0
         */
        Offset?: number;
        /**
         * 取回筆數上限。
         * @format int32
         * @example 20
         */
        Limit?: number;
        /**
         * 是否僅列出未讀通知。
         * @example false
         */
        UnreadOnly?: boolean;
      },
      params: RequestParams = {},
    ) =>
      this.http.request<ListNotificationsResponse, any>({
        path: `/v1/notifications/mine`,
        method: "GET",
        query: query,
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags Notifications
     * @name GetUnreadCount
     * @summary 查詢登入使用者本人的未讀通知數（前端鈴鐺輪詢用）。
     * @request GET:/v1/notifications/mine/unread-count
     */
    getUnreadCount: (params: RequestParams = {}) =>
      this.http.request<UnreadCountResponse, any>({
        path: `/v1/notifications/mine/unread-count`,
        method: "GET",
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags Notifications
     * @name MarkRead
     * @summary 將本人的單筆通知標為已讀。已讀則 no-op。
     * @request POST:/v1/notifications/{id}/read
     */
    markRead: (id: string, params: RequestParams = {}) =>
      this.http.request<void, any>({
        path: `/v1/notifications/${id}/read`,
        method: "POST",
        ...params,
      }),

    /**
     * No description
     *
     * @tags Notifications
     * @name MarkAllRead
     * @summary 將本人的全部未讀通知標為已讀。
     * @request POST:/v1/notifications/read-all
     */
    markAllRead: (params: RequestParams = {}) =>
      this.http.request<void, any>({
        path: `/v1/notifications/read-all`,
        method: "POST",
        ...params,
      }),
  };
  notificationService = {
    /**
     * No description
     *
     * @tags NotificationService
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
