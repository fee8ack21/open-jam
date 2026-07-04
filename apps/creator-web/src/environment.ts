class AppEnv {
  [key: string]: string;

  MARKET_PAGE_URL = 'http://localhost:5173'
  CATALOG_API_URL = 'https://api.openjam.co/catalog-service'
  STORE_API_URL = 'https://api.openjam.co/store-service'
  ORDER_API_URL = 'https://api.openjam.co/order-service'
  // 本機開發 fallback：無子網域時用此 slug 決定店面（正式由 <slug>.openjam.co 子網域推導）
  STORE_SLUG = 'xiaoming-shop'
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
  }
}

export const env = new AppEnv()
