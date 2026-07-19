/* ============================================================
   Open Jam Creator Studio — 靜態展示資料
   分類卡面（ProductThumb 縮圖字符）與上架精靈的標籤建議。
   ============================================================ */

/** 商品分類。 */
export interface Category {
  id: string
  label: string
  glyph: string
}

export const CATEGORIES: Category[] = [
  { id: 'music', label: '樂譜 / 音樂', glyph: 'note' },
  { id: 'photo', label: '攝影 / 照片集', glyph: 'image' },
  { id: 'ebook', label: '電子書 / 文件', glyph: 'book' },
]

export const TAGS: Record<string, string[]> = {
  music: ['鋼琴', '爵士', '古典', '流行', '配樂', '吉他', '電子'],
  photo: ['風景', '人像', '街拍', '黑白', '旅行', '空拍', '微距'],
  ebook: ['設計', '攝影教學', '商業', '寫作', '行銷', '生產力'],
}
