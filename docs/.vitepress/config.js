import { defineConfig } from 'vitepress'

// Convert Obsidian wiki links [[PageName]] to markdown links
function wikiLinkPlugin(md) {
  md.core.ruler.push('wiki_links', (state) => {
    for (const token of state.tokens) {
      if (token.type !== 'inline' || !token.children) continue
      const newChildren = []
      for (const child of token.children) {
        if (child.type !== 'text') { newChildren.push(child); continue }
        const parts = child.content.split(/\[\[([^\]]+)\]\]/)
        if (parts.length === 1) { newChildren.push(child); continue }
        for (let i = 0; i < parts.length; i++) {
          if (i % 2 === 0) {
            if (parts[i]) {
              const t = new state.Token('text', '', 0)
              t.content = parts[i]
              newChildren.push(t)
            }
          } else {
            const name = parts[i]
            const slug = name.toLowerCase()
            const open = new state.Token('html_inline', '', 0)
            open.content = `<a href="/${slug}">${name}</a>`
            newChildren.push(open)
          }
        }
      }
      token.children = newChildren
    }
  })
}

export default defineConfig({
  lang: 'zh-TW',
  title: 'Open Jam',
  titleTemplate: ':title · 專案文件',
  description: '面向創作者的數位商品上架與販售平台',
  appearance: true,

  head: [
    ['link', { rel: 'icon', type: 'image/x-icon', href: '/favicon.ico' }],
  ],

  markdown: {
    config: (md) => wikiLinkPlugin(md),
  },

  themeConfig: {
    nav: [],

    sidebar: [
      {
        text: '總覽',
        items: [
          { text: '首頁', link: '/' },
          { text: 'MVP 範圍與 Roadmap', link: '/mvp' },
        ],
      },
      {
        text: '功能規格',
        items: [
          { text: '認證授權 Auth', link: '/auth' },
          { text: '商店 Store', link: '/store' },
          { text: '商品 Catalog', link: '/catalog' },
          { text: '訂單與金流 Order', link: '/order' },
          { text: '儲存 Storage', link: '/storage' },
          { text: '通知 Notification', link: '/notification' },
          { text: '信件 Email', link: '/email' },
          { text: '配額 Quota', link: '/quota' },
          { text: '日誌 Log', link: '/log' },
        ],
      },
      {
        text: '維運與開發',
        items: [
          { text: '基礎架構 Infra', link: '/infra' },
          { text: 'CI 建置與推送', link: '/ci' },
          { text: '初始化 Bootstrap', link: '/bootstrap' },
          { text: '開發慣例 Develop', link: '/develop' },
          { text: '視覺設計準則 Design', link: '/design' },
        ],
      },
    ],

    socialLinks: [],

    footer: {
      message: 'Open Jam Internal Docs',
    },

    search: {
      provider: 'local',
    },
  },
})
