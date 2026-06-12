class AppEnv {
  WORKSPACE_URL = 'http://localhost:5174';
  HYDRA_PUBLIC_URL = 'http://localhost:4444';
  OIDC_CLIENT_ID = 'open-jam-web';
  AUTH_PAGE_URL = 'http://localhost:5169/';
  STORE_API_URL = 'http://localhost:5172';

  constructor() {
    Object.keys(this).forEach((key) => {
      const meta = document.querySelector(`meta[name="${key}"]`);
      const content = meta?.getAttribute('content');
      if (content) this[key] = content;
    });
  }
}

export const env = new AppEnv();
