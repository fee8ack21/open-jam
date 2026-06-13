class AppEnv {
  [key: string]: string;

  MARKET_URL = 'http://localhost:5173'

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
