<script setup lang="ts">
/* ============================================================
   ReviewWidget — 商品評價（僅出現在購買後的下載頁）。
   後端僅允許已購買者撰寫；同一人一商品一則，可重新送出更新。
   ============================================================ */
import { ref, onMounted } from 'vue';
import { useI18n } from 'vue-i18n';
import { catalogApi } from '@/api';
import { useAuthStore } from '@/stores/auth';
import AppIcon from '@/components/app-icon';

const props = defineProps<{ catalogId: string }>();
const auth = useAuthStore();
const { t } = useI18n();

const rating = ref(0);
const comment = ref('');
const submitting = ref(false);
const saved = ref(false);     // 已有 / 已送出評價
const editing = ref(false);   // 重新編輯既有評價
const error = ref('');

onMounted(async () => {
  if (!auth.isAuthenticated || !props.catalogId) return;
  try {
    const res = await catalogApi.catalogReviews.getMine(props.catalogId);
    const r = res.data;
    if (r?.rating) {
      rating.value = r.rating;
      comment.value = r.comment ?? '';
      saved.value = true;
    }
  } catch {
    // 204 / 尚未評價：保持空白
  }
});

async function submit() {
  if (!rating.value || submitting.value) return;
  submitting.value = true;
  error.value = '';
  try {
    await catalogApi.catalogReviews.upsertMine(props.catalogId, {
      rating: rating.value,
      comment: comment.value.trim() || null,
    });
    saved.value = true;
    editing.value = false;
  } catch {
    error.value = t('review.error');
  } finally {
    submitting.value = false;
  }
}
</script>

<template>
  <div class="review-widget">
    <!-- 未登入：購買驗證需登入身分 -->
    <div v-if="!auth.isAuthenticated" class="rw-login">
      <span><app-icon name="star" :size="14" /> {{ t('review.loginPrompt') }}</span>
      <n-button size="tiny" tertiary @click="auth.login()">{{ t('review.login') }}</n-button>
    </div>

    <!-- 已評價且非編輯狀態：顯示結果 + 修改入口 -->
    <div v-else-if="saved && !editing" class="rw-done">
      <span class="rw-done-label"><app-icon name="check" :size="14" /> {{ t('review.reviewed') }}</span>
      <n-rate :value="rating" readonly size="small" />
      <button type="button" class="rw-edit" @click="editing = true">{{ t('review.edit') }}</button>
    </div>

    <!-- 撰寫 / 編輯 -->
    <div v-else class="rw-form">
      <div class="rw-row">
        <span class="rw-label">{{ t('review.ratePrompt') }}</span>
        <n-rate v-model:value="rating" size="small" />
      </div>
      <n-input
        v-model:value="comment"
        type="textarea"
        :rows="2"
        maxlength="2000"
        show-count
        :placeholder="t('review.commentPlaceholder')" />
      <div class="rw-foot">
        <span v-if="error" class="rw-err">{{ error }}</span>
        <n-button
          size="small"
          type="primary"
          :disabled="!rating"
          :loading="submitting"
          @click="submit">
          {{ saved ? t('review.update') : t('review.submit') }}
        </n-button>
      </div>
    </div>
  </div>
</template>

<style scoped>
.review-widget {
  margin-top: 12px;
  padding-top: 12px;
  border-top: 1px dashed var(--border);
}
.rw-login, .rw-done {
  display: flex;
  align-items: center;
  gap: 10px;
  font-size: 13px;
  color: var(--text-soft);
}
.rw-done-label {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  color: var(--oj-primary, #16a07a);
  font-weight: 600;
}
.rw-edit {
  margin-left: auto;
  border: 0;
  background: transparent;
  color: var(--text-faint);
  font-size: 12.5px;
  cursor: pointer;
  text-decoration: underline;
}
.rw-form {
  display: flex;
  flex-direction: column;
  gap: 8px;
}
.rw-row {
  display: flex;
  align-items: center;
  gap: 10px;
}
.rw-label {
  font-size: 13px;
  font-weight: 600;
  color: var(--text-soft);
}
.rw-foot {
  display: flex;
  align-items: center;
  justify-content: flex-end;
  gap: 12px;
}
.rw-err {
  font-size: 12.5px;
  color: var(--err, #e5484d);
}
</style>
