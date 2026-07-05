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

export enum UserStatus {
  Pending = "Pending",
  Active = "Active",
  Locked = "Locked",
  Suspended = "Suspended",
  Deactivated = "Deactivated",
  Deleted = "Deleted",
}

export enum UserRole {
  User = "User",
  Admin = "Admin",
}

export enum LegalDocumentType {
  TermsOfService = "TermsOfService",
  PrivacyPolicy = "PrivacyPolicy",
}

export enum LegalDocumentStatus {
  Draft = "Draft",
  Active = "Active",
  Inactive = "Inactive",
}

export interface CreateLegalDocumentRequest {
  type?: LegalDocumentType;
  title?: string | null;
  content?: string | null;
}

export interface LegalDocumentDto {
  /** @format uuid */
  id?: string;
  type?: LegalDocumentType;
  /** @format int32 */
  version?: number;
  title?: string | null;
  status?: LegalDocumentStatus;
  /** @format date-time */
  activatedAt?: string | null;
  /** @format date-time */
  createdAt?: string;
  /** @format date-time */
  updatedAt?: string | null;
  content?: string | null;
}

export interface LegalDocumentSummaryDto {
  /** @format uuid */
  id?: string;
  type?: LegalDocumentType;
  /** @format int32 */
  version?: number;
  title?: string | null;
  status?: LegalDocumentStatus;
  /** @format date-time */
  activatedAt?: string | null;
  /** @format date-time */
  createdAt?: string;
  /** @format date-time */
  updatedAt?: string | null;
}

export interface ListLegalDocumentsResponse {
  /** @format int32 */
  totalCount?: number;
  items?: LegalDocumentSummaryDto[] | null;
}

export interface ListUsersResponse {
  /** @format int32 */
  totalCount?: number;
  items?: UserSummaryDto[] | null;
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

export interface UpdateLegalDocumentRequest {
  title?: string | null;
  content?: string | null;
}

export interface UserSummaryDto {
  /** @format uuid */
  id?: string;
  email?: string | null;
  role?: UserRole;
  status?: UserStatus;
  emailVerified?: boolean;
  /** @format date-time */
  createdAt?: string;
  /** @format date-time */
  updatedAt?: string | null;
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
 * @title Auth
 * @version v1
 */
export class Api<SecurityDataType extends unknown> {
  http: HttpClient<SecurityDataType>;

  constructor(http: HttpClient<SecurityDataType>) {
    this.http = http;
  }

  legalDocuments = {
    /**
     * No description
     *
     * @tags LegalDocuments
     * @name List
     * @request GET:/v1/legal-documents
     */
    list: (
      query?: {
        /** @format int32 */
        Offset?: number;
        /** @format int32 */
        Limit?: number;
        Type?: LegalDocumentType;
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
     * @request GET:/v1/legal-documents/active
     */
    getActive: (
      query?: {
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
  users = {
    /**
     * No description
     *
     * @tags Users
     * @name List
     * @request GET:/v1/users
     */
    list: (
      query?: {
        /** @format int32 */
        Offset?: number;
        /** @format int32 */
        Limit?: number;
        Search?: string;
        Role?: UserRole;
        Status?: UserStatus;
      },
      params: RequestParams = {},
    ) =>
      this.http.request<ListUsersResponse, any>({
        path: `/v1/users`,
        method: "GET",
        query: query,
        format: "json",
        ...params,
      }),
  };
}
