import { createApp, markRaw } from 'vue'
import { createPinia } from 'pinia'
import naive from 'naive-ui'

import App from './App.vue'
import router from './router'

import JIcon from '@/components/JIcon.vue'
import ProductThumb from '@/components/ProductThumb.vue'
import Stars from '@/components/Stars.vue'
import TrendChart from '@/components/TrendChart.vue'

import '@/styles/base.css'
import '@/styles/workspace.css'

const pinia = createPinia()
// make the router available inside store actions as `this.router`
pinia.use(({ store }) => { store.router = markRaw(router) })

const app = createApp(App)

app.use(pinia)
app.use(router)
app.use(naive)

// global UI components (kebab tags used throughout the templates)
app.component('j-icon', JIcon)
app.component('product-thumb', ProductThumb)
app.component('stars', Stars)
app.component('trend-chart', TrendChart)

app.mount('#app')
