class AppEnv {
  [key: string]: string;

  MARKET_PAGE_URL = 'http://localhost:5173'
  CATALOG_API_URL = 'https://api.openjam.co/catalog-service'
  STORE_API_URL = 'https://api.openjam.co/store-service'
  // 本機開發 fallback：無子網域時用此 slug 決定店面（正式由 <slug>.openjam.co 子網域推導）
  STORE_SLUG = 'xiaoming-shop'

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
