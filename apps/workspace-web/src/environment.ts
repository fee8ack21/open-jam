class AppEnv {
  [key: string]: string;

  WORKSPACE_PAGE_URL = "http://localhost:5175";
  OIDC_AUTHORITY = "https://hydra.openjam.co";
  OIDC_CLIENT_ID = "open-jam-web";
  AUTH_PAGE_URL = "http://localhost:5169";
  STORE_API_URL = "https://api.openjam.co/store-service";

  constructor() {
    Object.keys(this).forEach((key) => {
      const meta = document.querySelector(`meta[name="${key}"]`);
      const content = meta?.getAttribute("content");
      if (content) this[key] = content;
    });
  }
}

export const env = new AppEnv();
