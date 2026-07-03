class AppEnv {
  [key: string]: string;

  WORKSPACE_PAGE_URL = "http://localhost:5175";
  MARKET_PAGE_URL = "http://localhost:5173";
  OIDC_AUTHORITY = "https://hydra.openjam.co";
  OIDC_CLIENT_ID = "open-jam-web";
  AUTH_PAGE_URL = "http://localhost:5169";
  AUTH_API_URL = "https://api.openjam.co/auth-service";
  STORE_API_URL = "https://api.openjam.co/store-service";
  CATALOG_API_URL = "https://api.openjam.co/catalog-service";
  LOG_API_URL = "https://api.openjam.co/log-service";
  ORDER_API_URL = "https://api.openjam.co/order-service";
  STORAGE_API_URL = "https://api.openjam.co/storage-service";
  NOTIFICATION_API_URL = "https://api.openjam.co/notification-service";

  constructor() {
    Object.keys(this).forEach((key) => {
      const meta = document.querySelector(`meta[name="${key}"]`);
      const content = meta?.getAttribute("content");
      if (content) this[key] = content;
    });
  }
}

export const env = new AppEnv();
