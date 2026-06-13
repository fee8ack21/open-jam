import 'pinia'
import type { Router } from 'vue-router'

declare module 'pinia' {
  // main.ts 透過 Pinia plugin 將 router 實例注入每個 store，供 action 內以 `this.router` 使用。
  interface PiniaCustomProperties {
    router: Router
  }
}
