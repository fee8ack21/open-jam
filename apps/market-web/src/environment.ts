class AppEnv {
  WORKSPACE_PAGE_URL = 'http://localhost:5174';
  CREATOR_PAGE_BASE_URL = 'http://localhost:5174';
  OIDC_AUTHORITY = 'https://hydra.openjam.co';
  OIDC_CLIENT_ID = 'open-jam-web';
  AUTH_PAGE_URL = 'http://localhost:5169/';
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
