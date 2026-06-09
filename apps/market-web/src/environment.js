class AppEnv {
  WORKSPACE_URL = 'http://localhost:5174'
  CREATOR_BASE_URL = 'http://localhost:5174'

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
