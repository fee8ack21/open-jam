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
export var StoreStatus;
(function (StoreStatus) {
  StoreStatus[(StoreStatus["Value0"] = 0)] = "Value0";
  StoreStatus[(StoreStatus["Value1"] = 1)] = "Value1";
  StoreStatus[(StoreStatus["Value2"] = 2)] = "Value2";
})(StoreStatus || (StoreStatus = {}));
/**
 * 商店成員角色。
 * @format int32
 */
export var StoreMemberRole;
(function (StoreMemberRole) {
  StoreMemberRole[(StoreMemberRole["Value0"] = 0)] = "Value0";
})(StoreMemberRole || (StoreMemberRole = {}));
/**
 * 開店申請審核狀態。
 * @format int32
 */
export var StoreApplicationStatus;
(function (StoreApplicationStatus) {
  StoreApplicationStatus[(StoreApplicationStatus["Value0"] = 0)] = "Value0";
  StoreApplicationStatus[(StoreApplicationStatus["Value1"] = 1)] = "Value1";
  StoreApplicationStatus[(StoreApplicationStatus["Value2"] = 2)] = "Value2";
  StoreApplicationStatus[(StoreApplicationStatus["Value3"] = 3)] = "Value3";
})(StoreApplicationStatus || (StoreApplicationStatus = {}));
export var ContentType;
(function (ContentType) {
  ContentType["Json"] = "application/json";
  ContentType["JsonApi"] = "application/vnd.api+json";
  ContentType["FormData"] = "multipart/form-data";
  ContentType["UrlEncoded"] = "application/x-www-form-urlencoded";
  ContentType["Text"] = "text/plain";
})(ContentType || (ContentType = {}));
export class HttpClient {
  baseUrl = "";
  securityData = null;
  securityWorker;
  abortControllers = new Map();
  customFetch = (...fetchParams) => fetch(...fetchParams);
  baseApiParams = {
    credentials: "same-origin",
    headers: {},
    redirect: "follow",
    referrerPolicy: "no-referrer",
  };
  constructor(apiConfig = {}) {
    Object.assign(this, apiConfig);
  }
  setSecurityData = (data) => {
    this.securityData = data;
  };
  encodeQueryParam(key, value) {
    const encodedKey = encodeURIComponent(key);
    return `${encodedKey}=${encodeURIComponent(typeof value === "number" ? value : `${value}`)}`;
  }
  addQueryParam(query, key) {
    return this.encodeQueryParam(key, query[key]);
  }
  addArrayQueryParam(query, key) {
    const value = query[key];
    return value.map((v) => this.encodeQueryParam(key, v)).join("&");
  }
  toQueryString(rawQuery) {
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
  addQueryParams(rawQuery) {
    const queryString = this.toQueryString(rawQuery);
    return queryString ? `?${queryString}` : "";
  }
  contentFormatters = {
    [ContentType.Json]: (input) =>
      input !== null && (typeof input === "object" || typeof input === "string")
        ? JSON.stringify(input)
        : input,
    [ContentType.JsonApi]: (input) =>
      input !== null && (typeof input === "object" || typeof input === "string")
        ? JSON.stringify(input)
        : input,
    [ContentType.Text]: (input) =>
      input !== null && typeof input !== "string"
        ? JSON.stringify(input)
        : input,
    [ContentType.FormData]: (input) => {
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
    [ContentType.UrlEncoded]: (input) => this.toQueryString(input),
  };
  mergeRequestParams(params1, params2) {
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
  createAbortSignal = (cancelToken) => {
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
  abortRequest = (cancelToken) => {
    const abortController = this.abortControllers.get(cancelToken);
    if (abortController) {
      abortController.abort();
      this.abortControllers.delete(cancelToken);
    }
  };
  request = async ({
    body,
    secure,
    path,
    type,
    query,
    format,
    baseUrl,
    cancelToken,
    ...params
  }) => {
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
      const r = response;
      r.data = null;
      r.error = null;
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
 * @title StoreService
 * @version v1
 */
export class Api {
  http;
  constructor(http) {
    this.http = http;
  }
  storeApplications = {
    /**
     * No description
     *
     * @tags StoreApplications
     * @name StoreApplicationsCreate
     * @summary 提交開店申請。同一使用者僅能有一筆 Pending 申請。
     * @request POST:/store-applications
     */
    storeApplicationsCreate: (data, params = {}) =>
      this.http.request({
        path: `/store-applications`,
        method: "POST",
        body: data,
        type: ContentType.Json,
        format: "json",
        ...params,
      }),
    /**
     * No description
     *
     * @tags StoreApplications
     * @name StoreApplicationsList
     * @summary 查詢全平台開店申請列表，可依審核狀態篩選（分頁）。
     * @request GET:/store-applications
     */
    storeApplicationsList: (query, params = {}) =>
      this.http.request({
        path: `/store-applications`,
        method: "GET",
        query: query,
        format: "json",
        ...params,
      }),
    /**
     * No description
     *
     * @tags StoreApplications
     * @name GetStoreApplications
     * @summary 查詢自己的開店申請紀錄（分頁）。
     * @request GET:/store-applications/me
     */
    getStoreApplications: (query, params = {}) =>
      this.http.request({
        path: `/store-applications/me`,
        method: "GET",
        query: query,
        format: "json",
        ...params,
      }),
    /**
     * No description
     *
     * @tags StoreApplications
     * @name WithdrawCreate
     * @summary 撤回自己的待審核申請（Pending → Withdrawn），可重新提交。
     * @request POST:/store-applications/{id}/withdraw
     */
    withdrawCreate: (id, params = {}) =>
      this.http.request({
        path: `/store-applications/${id}/withdraw`,
        method: "POST",
        ...params,
      }),
    /**
     * No description
     *
     * @tags StoreApplications
     * @name ApproveCreate
     * @summary 核准開店申請（Pending → Approved），建立 Store 與 StoreMember(Owner)。
     * @request POST:/store-applications/{id}/approve
     */
    approveCreate: (id, params = {}) =>
      this.http.request({
        path: `/store-applications/${id}/approve`,
        method: "POST",
        format: "json",
        ...params,
      }),
    /**
     * No description
     *
     * @tags StoreApplications
     * @name RejectCreate
     * @summary 駁回開店申請（Pending → Rejected），可重新提交。
     * @request POST:/store-applications/{id}/reject
     */
    rejectCreate: (id, data, params = {}) =>
      this.http.request({
        path: `/store-applications/${id}/reject`,
        method: "POST",
        body: data,
        type: ContentType.Json,
        format: "json",
        ...params,
      }),
  };
  stores = {
    /**
     * No description
     *
     * @tags StoreFollowers
     * @name FollowCreate
     * @summary 追蹤商店。已追蹤則 no-op。
     * @request POST:/stores/{id}/follow
     */
    followCreate: (id, data, params = {}) =>
      this.http.request({
        path: `/stores/${id}/follow`,
        method: "POST",
        body: data,
        type: ContentType.Json,
        ...params,
      }),
    /**
     * No description
     *
     * @tags StoreFollowers
     * @name FollowDelete
     * @summary 取消追蹤商店。依 (StoreId, Email) 移除，未追蹤則 no-op。
     * @request DELETE:/stores/{id}/follow
     */
    followDelete: (id, data, params = {}) =>
      this.http.request({
        path: `/stores/${id}/follow`,
        method: "DELETE",
        body: data,
        type: ContentType.Json,
        ...params,
      }),
    /**
     * No description
     *
     * @tags StoreFollowers
     * @name FollowersList
     * @summary 查詢商店追蹤者列表（分頁）。僅 Owner 可操作。
     * @request GET:/stores/{id}/followers
     */
    followersList: (id, query, params = {}) =>
      this.http.request({
        path: `/stores/${id}/followers`,
        method: "GET",
        query: query,
        format: "json",
        ...params,
      }),
    /**
     * No description
     *
     * @tags Stores
     * @name StoresDetail
     * @summary 查詢商店基本資訊（公開）。
     * @request GET:/stores/{idOrSlug}
     */
    storesDetail: (idOrSlug, params = {}) =>
      this.http.request({
        path: `/stores/${idOrSlug}`,
        method: "GET",
        format: "json",
        ...params,
      }),
    /**
     * No description
     *
     * @tags Stores
     * @name GetStores
     * @summary 查詢登入使用者所屬的商店列表。
     * @request GET:/stores/me
     */
    getStores: (params = {}) =>
      this.http.request({
        path: `/stores/me`,
        method: "GET",
        format: "json",
        ...params,
      }),
    /**
     * No description
     *
     * @tags Stores
     * @name StoresPartialUpdate
     * @summary 更新商店基本資料（StoreName / Description）。僅 Owner 可操作。
     * @request PATCH:/stores/{id}
     */
    storesPartialUpdate: (id, data, params = {}) =>
      this.http.request({
        path: `/stores/${id}`,
        method: "PATCH",
        body: data,
        type: ContentType.Json,
        format: "json",
        ...params,
      }),
    /**
     * No description
     *
     * @tags Stores
     * @name SuspendCreate
     * @summary 平台停權商店（Active → Suspended）。僅 Admin 可操作。
     * @request POST:/stores/{id}/suspend
     */
    suspendCreate: (id, params = {}) =>
      this.http.request({
        path: `/stores/${id}/suspend`,
        method: "POST",
        ...params,
      }),
    /**
     * No description
     *
     * @tags Stores
     * @name UnsuspendCreate
     * @summary 解除商店停權（Suspended → Active）。僅 Admin 可操作。
     * @request POST:/stores/{id}/unsuspend
     */
    unsuspendCreate: (id, params = {}) =>
      this.http.request({
        path: `/stores/${id}/unsuspend`,
        method: "POST",
        ...params,
      }),
    /**
     * No description
     *
     * @tags Stores
     * @name CloseCreate
     * @summary 關閉商店（Active/Suspended → Closed，終態不可逆）。Owner 或 Admin 可操作。
     * @request POST:/stores/{id}/close
     */
    closeCreate: (id, params = {}) =>
      this.http.request({
        path: `/stores/${id}/close`,
        method: "POST",
        ...params,
      }),
    /**
     * No description
     *
     * @tags Stores
     * @name AvatarUploadUrlCreate
     * @summary 申請商店頭像（Avatar）上傳簽章 URL。僅 Owner 可操作。
     * @request POST:/stores/{id}/avatar/upload-url
     */
    avatarUploadUrlCreate: (id, data, params = {}) =>
      this.http.request({
        path: `/stores/${id}/avatar/upload-url`,
        method: "POST",
        body: data,
        type: ContentType.Json,
        format: "json",
        ...params,
      }),
    /**
     * No description
     *
     * @tags Stores
     * @name BannerUploadUrlCreate
     * @summary 申請商店橫幅（Banner）上傳簽章 URL。僅 Owner 可操作。
     * @request POST:/stores/{id}/banner/upload-url
     */
    bannerUploadUrlCreate: (id, data, params = {}) =>
      this.http.request({
        path: `/stores/${id}/banner/upload-url`,
        method: "POST",
        body: data,
        type: ContentType.Json,
        format: "json",
        ...params,
      }),
  };
  healthz = {
    /**
     * No description
     *
     * @tags StoreService
     * @name HealthzList
     * @request GET:/healthz
     */
    healthzList: (params = {}) =>
      this.http.request({
        path: `/healthz`,
        method: "GET",
        ...params,
      }),
  };
}
