class AppEnv {
  [key: string]: string;

  PORTAL_PAGE_URL = 'http://localhost:5173'
  CATALOG_API_URL = 'https://api.openjam.co/catalog-service'
  STORE_API_URL = 'https://api.openjam.co/store-service'
  ORDER_API_URL = 'https://api.openjam.co/order-service'
  // 本機開發 fallback：無子網域時用此 slug 決定店面（正式由 <slug>.openjam.co 子網域推導）；
  // pin-jui 為系統測試商店，查無商店會導向 404 頁
  STORE_SLUG = 'pin-jui'
  // OIDC（消費者免註冊，但若已於其他子網域登入，靜默讀取 Hydra session 帶入信箱）
  OIDC_AUTHORITY = 'https://hydra.openjam.co'
  OIDC_CLIENT_ID = 'open-jam-web'
  AUTH_PAGE_URL = 'http://localhost:5169'

  constructor() {
    Object.keys(this).forEach((key) => {
      const meta = document.querySelector(`meta[name="${key}"]`)
      const content = meta?.getAttribute('content')
      if (content) {
        this[key] = content
      }
    })
    // 尾斜線正規化：部署設定可能帶尾斜線（如 https://openjam.co/），
    // 使用端一律以 `${PORTAL_PAGE_URL}/path` 串接，避免產生 // 造成 404
    this.PORTAL_PAGE_URL = this.PORTAL_PAGE_URL.replace(/\/+$/, '')
  }
}

export const env = new AppEnv()
