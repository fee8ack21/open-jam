<script lang="ts">
import { useDashboardStore } from '@/stores/dashboard'
import { JFmt } from '@/utils/format'
import { ME } from '@/data'

export default {
  name: 'SettingsView',
  setup() { return { store: useDashboardStore(), F: JFmt } },
  data() {
    const me = ME
    return {
      form: { name: me.name, handle: me.handle, email: me.email, bio: me.bio },
      notif: { sales: true, weekly: true, promo: false },
    }
  },
  computed: {
    s() { return this.store },
    me() { return ME },
  },
}
</script>

<template>
  <div data-screen-label="設定">
    <div class="page-head">
      <div>
        <p class="h-eyebrow">帳號</p>
        <h1 class="h-title">設定</h1>
      </div>
    </div>

    <div class="dash-grid" style="gap:18px;">
      <!-- profile -->
      <div class="card-pad">
        <div class="set-grid">
          <div class="sg-k">
            <div class="sgk-t">個人檔案</div>
            <div class="sgk-d">這些資訊會顯示在你的創作者頁面與作品卡片上。</div>
          </div>
          <div>
            <div class="avatar-edit" style="margin-bottom:20px;">
              <span class="avatar" :style="{ background: me.avatar }">{{ F.initials(form.name) }}</span>
              <div>
                <n-button size="small">更換頭像</n-button>
                <div style="font-size:11.5px; color:var(--text-faint); margin-top:6px; font-family:var(--oj-mono);">JPG / PNG · 建議 400×400</div>
              </div>
            </div>
            <div class="field"><label class="field-label">顯示名稱</label><n-input v-model:value="form.name" size="large" /></div>
            <div class="field"><label class="field-label">帳號代稱</label><n-input v-model:value="form.handle" size="large"><template #prefix><span style="color:var(--text-faint)">@</span></template></n-input></div>
            <div class="field"><label class="field-label">電子信箱</label><n-input v-model:value="form.email" size="large"><template #prefix><j-icon name="mail" :size="16" style="color:var(--text-faint)" /></template></n-input></div>
            <div class="field"><label class="field-label">個人簡介</label><n-input v-model:value="form.bio" type="textarea" :autosize="{ minRows: 2, maxRows: 4 }" maxlength="120" show-count /></div>
          </div>
        </div>
      </div>

      <!-- payout -->
      <div class="card-pad">
        <div class="set-grid">
          <div class="sg-k">
            <div class="sgk-t">收款設定</div>
            <div class="sgk-d">每月 5 日自動撥款，最低門檻 $20。</div>
          </div>
          <div>
            <div style="display:flex; align-items:center; gap:14px; padding:16px; border:1.5px solid var(--border); border-radius:var(--r-md); background:var(--surface-2);">
              <span class="kpi-ic" style="background:var(--c-violet)"><j-icon name="wallet" :size="20" /></span>
              <div style="flex:1;">
                <div style="font-weight:700; font-size:14.5px;">銀行轉帳 · 永豐銀行 ••• 4821</div>
                <div style="font-family:var(--oj-mono); font-size:12px; color:var(--text-faint); margin-top:3px;">下次撥款 2026/06/05 · 待結算 {{ F.money(store.pendingPayout) }}</div>
              </div>
              <n-button size="small">變更</n-button>
            </div>
          </div>
        </div>
      </div>

      <!-- preferences -->
      <div class="card-pad">
        <div class="set-grid">
          <div class="sg-k">
            <div class="sgk-t">偏好設定</div>
            <div class="sgk-d">外觀與通知方式。</div>
          </div>
          <div>
            <div style="display:flex; align-items:center; justify-content:space-between; padding:14px 0; border-bottom:1.5px solid var(--border);">
              <div><div style="font-weight:600; font-size:14px;">售出通知</div><div style="font-size:12.5px; color:var(--text-faint); margin-top:2px;">有人購買作品時寄信通知</div></div>
              <n-switch v-model:value="notif.sales" />
            </div>
            <div style="display:flex; align-items:center; justify-content:space-between; padding:14px 0; border-bottom:1.5px solid var(--border);">
              <div><div style="font-weight:600; font-size:14px;">每週摘要</div><div style="font-size:12.5px; color:var(--text-faint); margin-top:2px;">每週一寄送銷售與瀏覽摘要</div></div>
              <n-switch v-model:value="notif.weekly" />
            </div>
            <div style="display:flex; align-items:center; justify-content:space-between; padding:14px 0;">
              <div><div style="font-weight:600; font-size:14px;">行銷推播</div><div style="font-size:12.5px; color:var(--text-faint); margin-top:2px;">活動與創作者社群消息</div></div>
              <n-switch v-model:value="notif.promo" />
            </div>
          </div>
        </div>
      </div>

      <div style="display:flex; justify-content:flex-end; gap:10px;">
        <n-button size="large">取消</n-button>
        <n-button type="primary" size="large" strong>儲存變更</n-button>
      </div>
    </div>
  </div>
</template>
