class AppEnv {
  WORKSPACE_PAGE_URL = 'http://localhost:5175';
  CREATOR_PAGE_BASE_URL = 'https://<store-slug>.openjam.co';
  OIDC_AUTHORITY = 'https://hydra.openjam.co';
  OIDC_CLIENT_ID = 'open-jam-web';
  AUTH_PAGE_URL = 'http://localhost:5169';
  GITHUB_REPO_URL = 'https://github.com/fee8ack21/open-jam';
  DOCS_URL = 'https://docs.openjam.co';
  CATALOG_API_URL = 'https://api.openjam.co/catalog-service';
  STORE_API_URL = 'https://api.openjam.co/store-service';
  [key: string]: string;

  constructor() {
    Object.keys(this).forEach((key) => {
      const meta = document.querySelector(`meta[name="${key}"]`);
      const content = meta?.getAttribute('content');
      if (content) this[key] = content;
    });
  }
}

export const env = new AppEnv();
