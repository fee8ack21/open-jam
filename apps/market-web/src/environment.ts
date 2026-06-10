class AppEnv {
  WORKSPACE_URL = 'http://localhost:5174';
  CREATOR_BASE_URL = 'http://localhost:5174';
  HYDRA_PUBLIC_URL = 'https://hydra.openjam.co';
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
