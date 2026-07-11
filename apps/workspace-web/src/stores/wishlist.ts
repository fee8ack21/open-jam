import { ref } from 'vue'
import { defineStore } from 'pinia'
import { catalogApi, storeApi } from '@/api'
import { env } from '@/environment'
import i18n from '@/i18n'
import type { CatalogCategoryDto } from '@/api/catalog-service'
import type { StoreDto } from '@/api/store-service'

/** 後端頂層分類 slug → 前端店面分類鍵（決定卡片縮圖字符 / 標籤）。 */
const ROOT_SLUG_TO_KEY: Record<string, string> = {
  music: 'music',
  photography: 'photo',
  ebook: 'ebook',
}

/** 願望清單卡片顯示模型（相容 ProductThumb / Stars 所需欄位）。 */
export interface WishlistEntry {
  id: string
  cat: string
  hue: number
  title: string
  creator: string
  handle: string
  avatar: string
  price: number
  rating: number
  ratingCount: number
  tags: string[]
  formats: string[]
  totalSize: string
  /** 前往創作者子網域商品頁；商店 slug 未知時為 null。 */
  productUrl: string | null
}

/** 由後端 RFC 9457 Problem Details 取出可顯示的錯誤訊息。 */
function messageOf(err: unknown, fallback: string): string {
  const response = err as { error?: { detail?: string; title?: string } } | null
  const problem = response?.error
  return problem?.detail ?? problem?.title ?? fallback
}

/** 由 coverHue 產生穩定的頭像底色（avatar 欄位是 CSS 顏色而非圖片）。 */
function hueColor(hue: number): string {
  return `hsl(${hue} 70% 58%)`
}

/**
 * 買家「願望清單」store：以 CatalogService 收藏 API 為單一來源。
 * `GET /v1/catalogs/favorites` 僅回商品 ID 清單，逐筆取商品詳情與所屬商店資訊
 * （商店以 storeId 快取，避免重複請求）組出卡片。收藏可橫跨多個商店。
 */
export const useWishlistStore = defineStore('wishlist', () => {
  const items = ref<WishlistEntry[]>([])
  /** 收藏數（側欄徽章用，可先以 loadCount 便宜取得，不需載入完整詳情）。 */
  const count = ref(0)
  const loading = ref(false)
  const error = ref<string | null>(null)
  const loaded = ref(false)

  // 跨呼叫快取：分類清單與已查過的商店（同一批收藏常屬同店）。
  let categories: CatalogCategoryDto[] = []
  const storeCache = new Map<string, StoreDto>()

  /** 沿 parentId 走到頂層分類，以其 slug 對應前端分類鍵；無法對應時回 'music'。 */
  function categoryKey(categoryId?: string | null): string {
    const byId = new Map(categories.filter((c) => c.id).map((c) => [c.id as string, c]))
    let cur = categoryId ? byId.get(categoryId) : undefined
    const seen = new Set<string>()
    while (cur?.parentId && cur.id && !seen.has(cur.id)) {
      seen.add(cur.id)
      cur = byId.get(cur.parentId)
    }
    return (cur?.slug && ROOT_SLUG_TO_KEY[cur.slug]) || 'music'
  }

  /** 取商店資訊（含 storeName / storeSlug），以 storeId 快取。取不到回 null。 */
  async function fetchStore(storeId: string): Promise<StoreDto | null> {
    const cached = storeCache.get(storeId)
    if (cached) return cached
    try {
      const res = await storeApi.stores.get(storeId)
      storeCache.set(storeId, res.data)
      return res.data
    } catch {
      return null
    }
  }

  /** 僅取收藏數（單一請求，不載入商品詳情）；未登入 / 失敗則歸零。 */
  async function loadCount() {
    try {
      const res = await catalogApi.catalogFavorites.listMine()
      count.value = (res.data.catalogIds ?? []).length
    } catch {
      count.value = 0
    }
  }

  /** 載入完整願望清單（收藏 ID → 商品詳情 → 商店資訊）。 */
  async function load() {
    loading.value = true
    error.value = null
    try {
      const favRes = await catalogApi.catalogFavorites.listMine()
      const ids = (favRes.data.catalogIds ?? []).filter((id): id is string => Boolean(id))
      count.value = ids.length
      if (!ids.length) {
        items.value = []
        loaded.value = true
        return
      }

      // 分類清單只需載入一次（供分類鍵解析）。
      if (!categories.length) {
        try {
          const catRes = await catalogApi.catalogCategories.list()
          categories = catRes.data ?? []
        } catch {
          categories = []
        }
      }

      // 逐筆取商品詳情；個別失敗（已下架 / 已刪除）以 null 略過，保留其餘。
      const details = await Promise.all(
        ids.map(async (id) => {
          try {
            return (await catalogApi.catalogs.get(id)).data
          } catch {
            return null
          }
        }),
      )
      const catalogs = details.filter((c): c is NonNullable<typeof c> => c != null)

      // 預取所有涉及的商店（去重）。
      const storeIds = [...new Set(catalogs.map((c) => c.storeId).filter((s): s is string => Boolean(s)))]
      await Promise.all(storeIds.map(fetchStore))

      items.value = catalogs.map((dto) => {
        const store = dto.storeId ? storeCache.get(dto.storeId) : undefined
        const slug = store?.storeSlug ?? null
        const hue = dto.coverHue ?? 256
        return {
          id: dto.id ?? '',
          cat: categoryKey(dto.categoryId),
          hue,
          title: dto.name ?? '',
          creator: store?.storeName ?? '',
          handle: slug ? '@' + slug : '',
          avatar: hueColor(hue),
          price: dto.price ?? 0,
          rating: dto.ratingAverage ?? 0,
          ratingCount: dto.ratingCount ?? 0,
          tags: dto.tags ?? [],
          formats: [],
          totalSize: '—',
          productUrl: slug
            ? `${env.CREATOR_PAGE_BASE_URL.replace('<store-slug>', slug)}/product/${dto.id}`
            : null,
        }
      })
      loaded.value = true
    } catch (err) {
      error.value = messageOf(err, i18n.global.t('storeError.loadWishlistFailed'))
      items.value = []
    } finally {
      loading.value = false
    }
  }

  /**
   * 取消收藏：樂觀移除卡片並呼叫 CatalogService，失敗則還原並回報錯誤。
   */
  async function remove(id: string) {
    const idx = items.value.findIndex((x) => x.id === id)
    if (idx < 0) return
    const [removed] = items.value.splice(idx, 1)
    count.value = Math.max(0, count.value - 1)
    try {
      await catalogApi.catalogFavorites.remove(id)
    } catch (err) {
      items.value.splice(idx, 0, removed)
      count.value += 1
      error.value = messageOf(err, i18n.global.t('storeError.removeWishlistFailed'))
    }
  }

  return { items, count, loading, error, loaded, load, loadCount, remove }
})
