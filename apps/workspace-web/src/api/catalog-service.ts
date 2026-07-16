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

export enum CatalogStatus {
  Draft = "Draft",
  Published = "Published",
  Archived = "Archived",
  Suspended = "Suspended",
}

export enum CatalogSort {
  Newest = "Newest",
  PriceLowToHigh = "PriceLowToHigh",
  PriceHighToLow = "PriceHighToLow",
  StoreFeatured = "StoreFeatured",
}

export enum CatalogAssetType {
  Thumbnail = "Thumbnail",
  Screenshot = "Screenshot",
  PreviewAudio = "PreviewAudio",
  PreviewVideo = "PreviewVideo",
}

export interface CatalogAssetDto {
  /** @format uuid */
  id?: string;
  type?: CatalogAssetType;
  fileName?: string | null;
  contentType?: string | null;
  /** @format int64 */
  fileSize?: number;
  /** @format int32 */
  sortOrder?: number;
  url?: string | null;
  /** @format date-time */
  createdAt?: string;
}

export interface CatalogAssetUploadUrlResponse {
  /** @format uuid */
  assetId?: string;
  uploadUrl?: string | null;
  publicUrl?: string | null;
  /** @format date-time */
  expiresAt?: string;
}

export interface CatalogCategoryDto {
  /** @format uuid */
  id?: string;
  /** @format uuid */
  parentId?: string | null;
  name?: string | null;
  slug?: string | null;
  description?: string | null;
  /** @format int32 */
  sortOrder?: number;
  isSystem?: boolean;
}

export interface CatalogDto {
  /** @format uuid */
  id?: string;
  /** @format uuid */
  storeId?: string;
  /** @format uuid */
  categoryId?: string | null;
  name?: string | null;
  slug?: string | null;
  description?: string | null;
  summary?: string | null;
  /** @format int32 */
  coverHue?: number;
  status?: CatalogStatus;
  /** @format double */
  price?: number;
  currency?: string | null;
  /** @format int64 */
  salesCount?: number;
  /** @format int64 */
  viewCount?: number;
  isFeatured?: boolean;
  isStoreFeatured?: boolean;
  /** @format int32 */
  storeFeaturedSortOrder?: number;
  /** @format double */
  ratingAverage?: number;
  /** @format int32 */
  ratingCount?: number;
  thumbnailUrl?: string | null;
  currentVersion?: CatalogVersionDto;
  assets?: CatalogAssetDto[] | null;
  tags?: string[] | null;
  /** @format date-time */
  publishedAt?: string | null;
  /** @format date-time */
  createdAt?: string;
  /** @format date-time */
  updatedAt?: string | null;
}

export interface CatalogFavoritesResponse {
  catalogIds?: string[] | null;
}

export interface CatalogReviewDto {
  /** @format uuid */
  id?: string;
  /** @format uuid */
  catalogId?: string;
  /** @format uuid */
  reviewerUserId?: string;
  /** @format int32 */
  rating?: number;
  comment?: string | null;
  /** @format date-time */
  createdAt?: string;
  /** @format date-time */
  updatedAt?: string | null;
}

export interface CatalogSummaryDto {
  /** @format uuid */
  id?: string;
  /** @format uuid */
  storeId?: string;
  /** @format uuid */
  categoryId?: string | null;
  name?: string | null;
  slug?: string | null;
  summary?: string | null;
  /** @format int32 */
  coverHue?: number;
  /** @format double */
  price?: number;
  currency?: string | null;
  status?: CatalogStatus;
  /** @format int64 */
  salesCount?: number;
  /** @format int64 */
  viewCount?: number;
  isFeatured?: boolean;
  isStoreFeatured?: boolean;
  /** @format int32 */
  storeFeaturedSortOrder?: number;
  /** @format double */
  ratingAverage?: number;
  /** @format int32 */
  ratingCount?: number;
  thumbnailUrl?: string | null;
  tags?: string[] | null;
  /** @format date-time */
  publishedAt?: string | null;
}

export interface CatalogTagDto {
  /** @format uuid */
  id?: string;
  name?: string | null;
  /** @format int32 */
  usageCount?: number;
}

export interface CatalogVersionAssetDto {
  /** @format uuid */
  id?: string;
  fileName?: string | null;
  contentType?: string | null;
  /** @format int64 */
  fileSize?: number;
  /** @format int32 */
  sortOrder?: number;
  /** @format date-time */
  createdAt?: string;
}

export interface CatalogVersionDto {
  /** @format uuid */
  id?: string;
  /** @format uuid */
  catalogId?: string;
  version?: string | null;
  releaseNote?: string | null;
  assets?: CatalogVersionAssetDto[] | null;
  /** @format date-time */
  createdAt?: string;
}

export interface ConfirmCatalogAssetRequest {
  type?: CatalogAssetType;
}

export interface CreateCatalogCategoryRequest {
  /** @format uuid */
  parentId?: string | null;
  name?: string | null;
  slug?: string | null;
  description?: string | null;
  /** @format int32 */
  sortOrder?: number;
}

export interface CreateCatalogRequest {
  /** @format uuid */
  storeId?: string;
  name?: string | null;
  slug?: string | null;
  description?: string | null;
  summary?: string | null;
  /** @format int32 */
  coverHue?: number | null;
  /** @format uuid */
  categoryId?: string | null;
  /** @format double */
  price?: number;
  currency?: string | null;
  tags?: string[] | null;
}

export interface CreateCatalogVersionRequest {
  version?: string | null;
  releaseNote?: string | null;
}

export interface ListCatalogTagsResponse {
  /** @format int32 */
  totalCount?: number;
  items?: CatalogTagDto[] | null;
}

export interface ListCatalogsResponse {
  /** @format int32 */
  totalCount?: number;
  items?: CatalogSummaryDto[] | null;
}

export interface ListReviewsResponse {
  /** @format double */
  ratingAverage?: number;
  /** @format int32 */
  ratingCount?: number;
  ratingDistribution?: number[] | null;
  items?: CatalogReviewDto[] | null;
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

export interface PurchasedVersionAssetDto {
  /** @format uuid */
  id?: string;
  fileName?: string | null;
  contentType?: string | null;
  /** @format int64 */
  fileSize?: number;
  /** @format int32 */
  sortOrder?: number;
  downloadUrl?: string | null;
  /** @format date-time */
  expiresAt?: string;
}

export interface ReorderStoreFeaturedRequest {
  /** @format uuid */
  storeId?: string;
  catalogIds?: string[] | null;
}

export interface RequestCatalogAssetUploadUrlRequest {
  type?: CatalogAssetType;
  fileName?: string | null;
  contentType?: string | null;
  /** @format int64 */
  sizeBytes?: number;
}

export interface RequestVersionAssetUploadUrlRequest {
  fileName?: string | null;
  contentType?: string | null;
  /** @format int64 */
  sizeBytes?: number;
}

export interface SetCatalogCategoryRequest {
  /** @format uuid */
  categoryId?: string | null;
}

export interface SetCatalogTagsRequest {
  tags?: string[] | null;
}

export interface StorageDownloadUrlResult {
  /** @format uuid */
  fileId?: string;
  downloadUrl?: string | null;
  /** @format date-time */
  expiresAt?: string;
}

export interface UpdateCatalogCategoryRequest {
  /** @format uuid */
  parentId?: string | null;
  name?: string | null;
  slug?: string | null;
  description?: string | null;
  /** @format int32 */
  sortOrder?: number | null;
}

export interface UpdateCatalogRequest {
  name?: string | null;
  slug?: string | null;
  description?: string | null;
  summary?: string | null;
  /** @format int32 */
  coverHue?: number | null;
  /** @format double */
  price?: number | null;
  currency?: string | null;
}

export interface UpsertReviewRequest {
  /** @format int32 */
  rating?: number;
  comment?: string | null;
}

export interface VersionAssetUploadUrlResponse {
  /** @format uuid */
  assetId?: string;
  uploadUrl?: string | null;
  /** @format date-time */
  expiresAt?: string;
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
     * @request DELETE:/v1/catalog-categories/{id}
     */
    delete: (id: string, params: RequestParams = {}) =>
      this.http.request<void, any>({
        path: `/v1/catalog-categories/${id}`,
        method: "DELETE",
        ...params,
      }),
  };
  catalogFavorites = {
    /**
     * No description
     *
     * @tags CatalogFavorites
     * @name ListMine
     * @request GET:/v1/catalogs/favorites
     */
    listMine: (params: RequestParams = {}) =>
      this.http.request<CatalogFavoritesResponse, any>({
        path: `/v1/catalogs/favorites`,
        method: "GET",
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags CatalogFavorites
     * @name Add
     * @request POST:/v1/catalogs/{id}/favorite
     */
    add: (id: string, params: RequestParams = {}) =>
      this.http.request<void, any>({
        path: `/v1/catalogs/${id}/favorite`,
        method: "POST",
        ...params,
      }),

    /**
     * No description
     *
     * @tags CatalogFavorites
     * @name Remove
     * @request DELETE:/v1/catalogs/{id}/favorite
     */
    remove: (id: string, params: RequestParams = {}) =>
      this.http.request<void, any>({
        path: `/v1/catalogs/${id}/favorite`,
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
     * @request GET:/v1/catalogs/{catalogId}/reviews
     */
    list: (
      catalogId: string,
      query?: {
        /** @format int32 */
        Offset?: number;
        /** @format int32 */
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
     * @request DELETE:/v1/catalogs/{catalogId}/reviews/mine
     */
    deleteMine: (catalogId: string, params: RequestParams = {}) =>
      this.http.request<void, any>({
        path: `/v1/catalogs/${catalogId}/reviews/mine`,
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
     * @request GET:/v1/catalogs
     */
    list: (
      query?: {
        /** @format uuid */
        StoreId?: string;
        /** @format uuid */
        CategoryId?: string;
        Status?: CatalogStatus;
        Tag?: string;
        Search?: string;
        Featured?: boolean;
        StoreFeatured?: boolean;
        /** @format double */
        MinPrice?: number;
        /** @format double */
        MaxPrice?: number;
        Sort?: CatalogSort;
        /** @format int32 */
        Offset?: number;
        /** @format int32 */
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
     * @request GET:/v1/catalogs/mine
     */
    listMine: (
      query?: {
        /** @format uuid */
        StoreId?: string;
        /** @format uuid */
        CategoryId?: string;
        Status?: CatalogStatus;
        Tag?: string;
        Search?: string;
        Featured?: boolean;
        StoreFeatured?: boolean;
        /** @format double */
        MinPrice?: number;
        /** @format double */
        MaxPrice?: number;
        Sort?: CatalogSort;
        /** @format int32 */
        Offset?: number;
        /** @format int32 */
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
     * @request GET:/v1/catalogs/by-store/{storeId}
     */
    listByStore: (
      storeId: string,
      query?: {
        /** @format uuid */
        StoreId?: string;
        /** @format uuid */
        CategoryId?: string;
        Status?: CatalogStatus;
        Tag?: string;
        Search?: string;
        Featured?: boolean;
        StoreFeatured?: boolean;
        /** @format double */
        MinPrice?: number;
        /** @format double */
        MaxPrice?: number;
        Sort?: CatalogSort;
        /** @format int32 */
        Offset?: number;
        /** @format int32 */
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
     * @name Delete
     * @request DELETE:/v1/catalogs/{id}
     */
    delete: (id: string, params: RequestParams = {}) =>
      this.http.request<void, ProblemDetails>({
        path: `/v1/catalogs/${id}`,
        method: "DELETE",
        ...params,
      }),

    /**
     * No description
     *
     * @tags Catalogs
     * @name SetCategory
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
     * @name IncrementView
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
     * @name Feature
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
     * @name StoreFeature
     * @request POST:/v1/catalogs/{id}/store-feature
     */
    storeFeature: (id: string, params: RequestParams = {}) =>
      this.http.request<void, any>({
        path: `/v1/catalogs/${id}/store-feature`,
        method: "POST",
        ...params,
      }),

    /**
     * No description
     *
     * @tags Catalogs
     * @name StoreUnfeature
     * @request POST:/v1/catalogs/{id}/store-unfeature
     */
    storeUnfeature: (id: string, params: RequestParams = {}) =>
      this.http.request<void, any>({
        path: `/v1/catalogs/${id}/store-unfeature`,
        method: "POST",
        ...params,
      }),

    /**
     * No description
     *
     * @tags Catalogs
     * @name ReorderStoreFeatured
     * @request PUT:/v1/catalogs/store-featured/order
     */
    reorderStoreFeatured: (
      data: ReorderStoreFeaturedRequest,
      params: RequestParams = {},
    ) =>
      this.http.request<void, any>({
        path: `/v1/catalogs/store-featured/order`,
        method: "PUT",
        body: data,
        type: ContentType.Json,
        ...params,
      }),

    /**
     * No description
     *
     * @tags Catalogs
     * @name RequestAssetUploadUrl
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
     * @name ConfirmAsset
     * @request POST:/v1/catalogs/{id}/assets/{assetId}/confirm
     */
    confirmAsset: (
      id: string,
      assetId: string,
      data: ConfirmCatalogAssetRequest,
      params: RequestParams = {},
    ) =>
      this.http.request<CatalogAssetDto, ProblemDetails>({
        path: `/v1/catalogs/${id}/assets/${assetId}/confirm`,
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
     * @request DELETE:/v1/catalogs/{id}/assets/{assetId}
     */
    deleteAsset: (id: string, assetId: string, params: RequestParams = {}) =>
      this.http.request<void, any>({
        path: `/v1/catalogs/${id}/assets/${assetId}`,
        method: "DELETE",
        ...params,
      }),
  };
  catalogServiceVersion0080CultureNeutralPublicKeyTokenNull = {
    /**
     * No description
     *
     * @tags CatalogService, Version=0.0.8.0, Culture=neutral, PublicKeyToken=null
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
     * @request GET:/v1/catalog-tags
     */
    list: (
      query?: {
        Search?: string;
        /** @format int32 */
        Offset?: number;
        /** @format int32 */
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
     * @name ConfirmAsset
     * @request POST:/v1/catalogs/{catalogId}/versions/{versionId}/assets/{assetId}/confirm
     */
    confirmAsset: (
      catalogId: string,
      versionId: string,
      assetId: string,
      params: RequestParams = {},
    ) =>
      this.http.request<CatalogVersionAssetDto, ProblemDetails>({
        path: `/v1/catalogs/${catalogId}/versions/${versionId}/assets/${assetId}/confirm`,
        method: "POST",
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags CatalogVersions
     * @name GetAssetDownloadUrl
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
     * @name ListPurchasedDownloads
     * @request GET:/v1/catalogs/{catalogId}/versions/{versionId}/downloads
     */
    listPurchasedDownloads: (
      catalogId: string,
      versionId: string,
      query?: {
        /** @format uuid */
        orderId?: string;
      },
      params: RequestParams = {},
    ) =>
      this.http.request<PurchasedVersionAssetDto[], any>({
        path: `/v1/catalogs/${catalogId}/versions/${versionId}/downloads`,
        method: "GET",
        query: query,
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags CatalogVersions
     * @name DeleteAsset
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
}
