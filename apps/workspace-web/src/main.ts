import { createApp, markRaw } from 'vue'
import { createPinia } from 'pinia'
import naive from 'naive-ui'

import App from './App.vue'
import router from './router'
import i18n from './i18n'

import AppIcon from '@/components/app-icon'
import ProductThumb from '@/components/ProductThumb.vue'
import Stars from '@/components/Stars.vue'
import TrendChart from '@/components/TrendChart.vue'

import '@/assets/styles/base.css'
import '@/assets/styles/workspace.css'

import { getUser, login } from '@/oidc/auth'

async function bootstrap() {
  const user = await getUser().catch(() => null)
  if (!user || user.expired) {
    login(window.location.href)
    return
  }

  const pinia = createPinia()
  // make the router available inside store actions as `this.router`
  pinia.use(({ store }) => { store.router = markRaw(router) })

  const app = createApp(App)

  app.use(pinia)
  app.use(router)
  app.use(naive)
  app.use(i18n)

  // global UI components (kebab tags used throughout the templates)
  app.component('app-icon', AppIcon)
  app.component('product-thumb', ProductThumb)
  app.component('stars', Stars)
  app.component('trend-chart', TrendChart)

  app.mount('#app')
}

bootstrap()
