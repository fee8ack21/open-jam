<script setup lang="ts">
/* ============================================================
   OnboardingGuide — first-visit flow for the marketplace home.
   1) intro modal (matches the Auth legal-modal style) explaining
      this is a test side-project on Stripe Test Mode;
   2) a dimmed spotlight + arrow coaching the top-right header
      icons (GitHub repo / spec docs);
   then writes a cookie so it never shows again.
   ============================================================ */
import { ref, computed, onMounted, onBeforeUnmount, nextTick } from 'vue';
import { useI18n } from 'vue-i18n';
import AppIcon from '@/components/app-icon/AppIcon.vue';
import { env } from '@/environment.js';
import { hasCookie, setCookie } from '@/utils/cookies';

const { t } = useI18n();

const COOKIE = 'oj_market_onboarded';

type Step = 'hidden' | 'modal' | 'coach';
const step = ref<Step>('hidden');

const spot = ref({ top: 0, left: 0, width: 0, height: 0, right: 0 });
const viewport = ref({ w: 0, h: 0 });
const ready = ref(false);

const cardStyle = computed(() => ({
  top: spot.value.top + spot.value.height + 16 + 'px',
  right: spot.value.right + 'px',
}));

// full-screen blurred dim-veil with a rounded-rect hole punched out over the
// spotlight (clip-path evenodd) so the highlighted icons stay sharp and the
// clear area matches the spotlight ring's rounded corners
const veilStyle = computed(() => {
  const { left: x, top: y, width: w, height: h } = spot.value;
  const r = Math.min(14, w / 2, h / 2);
  const outer = `M0 0H${viewport.value.w}V${viewport.value.h}H0Z`;
  const hole =
    `M${x + r} ${y}` +
    `H${x + w - r}A${r} ${r} 0 0 1 ${x + w} ${y + r}` +
    `V${y + h - r}A${r} ${r} 0 0 1 ${x + w - r} ${y + h}` +
    `H${x + r}A${r} ${r} 0 0 1 ${x} ${y + h - r}` +
    `V${y + r}A${r} ${r} 0 0 1 ${x + r} ${y}Z`;
  const path = `path(evenodd, "${outer}${hole}")`;
  return { clipPath: path, WebkitClipPath: path };
});

function measure() {
  const links = document.querySelectorAll<HTMLElement>('.nav-actions .nav-ic');
  if (!links.length) {
    ready.value = false;
    return;
  }
  let l = Infinity, t = Infinity, r = -Infinity, b = -Infinity;
  links.forEach((el) => {
    const rect = el.getBoundingClientRect();
    l = Math.min(l, rect.left);
    t = Math.min(t, rect.top);
    r = Math.max(r, rect.right);
    b = Math.max(b, rect.bottom);
  });
  const pad = 8;
  viewport.value = { w: window.innerWidth, h: window.innerHeight };
  spot.value = {
    left: l - pad,
    top: t - pad,
    width: r - l + pad * 2,
    height: b - t + pad * 2,
    right: window.innerWidth - (r + pad),
  };
  ready.value = true;
}

function goCoach() {
  step.value = 'coach';
  nextTick(measure);
}

function finish() {
  setCookie(COOKIE, '1', 365);
  step.value = 'hidden';
}

function onResize() {
  if (step.value === 'coach') measure();
}

onMounted(() => {
  if (hasCookie(COOKIE)) return; // already onboarded — stay hidden
  step.value = 'modal';
  window.addEventListener('resize', onResize, { passive: true });
});
onBeforeUnmount(() => window.removeEventListener('resize', onResize));
</script>

<template>
  <!-- 1) intro modal — v3 條款 dialog 樣式（膠帶白卡 + 奶油 header/footer） -->
  <div v-if="step === 'modal'" class="modal-scrim" @click.self="goCoach">
    <div class="modal-card" role="dialog" aria-modal="true" :aria-label="t('onboarding.modalAria')">
      <div class="modal-head">
        <div class="modal-badge"><app-icon name="note" :size="24" /></div>
        <h3 class="modal-title">{{ t('onboarding.title') }}</h3>
        <p class="modal-meta">{{ t('onboarding.meta') }}</p>
        <button class="modal-x" :aria-label="t('onboarding.closeAria')" @click="goCoach">
          <app-icon name="close" :size="16" />
        </button>
      </div>
      <div class="modal-body">
        <div class="legal-sec">
          <h4><span class="num">01</span> {{ t('onboarding.s1.title') }}</h4>
          <p>{{ t('onboarding.s1.body') }}</p>
        </div>
        <div class="legal-sec">
          <h4><span class="num">02</span> {{ t('onboarding.s2.title') }}</h4>
          <i18n-t keypath="onboarding.s2.body" tag="p" scope="global">
            <template #testMode><b>{{ t('onboarding.s2.testMode') }}</b></template>
            <template #card><b>4242 4242 4242 4242</b></template>
          </i18n-t>
        </div>
        <div class="legal-sec">
          <h4><span class="num">03</span> {{ t('onboarding.s3.title') }}</h4>
          <p>{{ t('onboarding.s3.body') }}</p>
        </div>
      </div>
      <div class="modal-foot">
        <button class="btn-pop" @click="goCoach">
          <app-icon name="check" :size="16" /> {{ t('onboarding.start') }}
        </button>
      </div>
    </div>
  </div>

  <!-- 2) coach-marks — spotlight the top-right header icons -->
  <div v-if="step === 'coach' && ready" class="coach" @click="finish">
    <div class="coach-veil" :style="veilStyle"></div>
    <div
      class="coach-spot"
      :style="{ top: spot.top + 'px', left: spot.left + 'px', width: spot.width + 'px', height: spot.height + 'px' }"
    ></div>
    <div class="coach-card" :style="cardStyle" @click.stop>
      <span class="coach-arrow"></span>
      <p class="coach-eyebrow">{{ t('onboarding.coachEyebrow') }}</p>
      <a class="coach-row" :href="env.GITHUB_REPO_URL" target="_blank" rel="noopener noreferrer">
        <span class="coach-ic"><app-icon name="github" :size="18" /></span>
        <span class="coach-txt"><b>{{ t('onboarding.coachGithubTitle') }}</b><span>{{ t('onboarding.coachGithubDesc') }}</span></span>
      </a>
      <a class="coach-row" :href="env.DOCS_URL" target="_blank" rel="noopener noreferrer">
        <span class="coach-ic"><app-icon name="book" :size="18" /></span>
        <span class="coach-txt"><b>{{ t('onboarding.coachDocsTitle') }}</b><span>{{ t('onboarding.coachDocsDesc') }}</span></span>
      </a>
      <button class="btn-pop coach-done" @click="finish">
        <app-icon name="check" :size="15" /> {{ t('onboarding.coachDone') }}
      </button>
    </div>
  </div>
</template>

<style scoped>
/* ---------- intro modal（v3 條款 dialog：膠帶白卡 + 奶油 header/footer） ---------- */
.modal-scrim {
  position: fixed; inset: 0; z-index: 200;
  background: rgba(26, 26, 26, 0.55); backdrop-filter: blur(4px);
  display: grid; place-items: center; padding: 40px 24px;
  animation: scrim-in 0.22s ease;
}
@keyframes scrim-in { from { opacity: 0; } to { opacity: 1; } }
.modal-card {
  position: relative;
  width: 100%; max-width: 600px; max-height: min(82vh, 720px);
  background: var(--surface); border: 2px solid var(--border-strong); border-radius: 24px;
  box-shadow: 8px 8px 0 rgba(26, 26, 26, 0.9);
  display: flex; flex-direction: column; overflow: hidden;
  animation: modal-in 0.34s cubic-bezier(0.2, 1.15, 0.4, 1);
}
/* 膠帶貼條 */
.modal-card::before {
  content: ''; position: absolute; top: -2px; left: 50%; width: 76px; height: 14px;
  margin-left: -38px; background: rgba(255, 222, 0, 0.9); border-radius: 0 0 3px 3px;
  transform: rotate(-2deg); box-shadow: 0 1px 3px rgba(26, 26, 26, 0.15); z-index: 6;
}
@keyframes modal-in {
  from { opacity: 0; transform: translateY(18px) scale(0.97); }
  to { opacity: 1; transform: none; }
}
.modal-head {
  position: relative; flex: none; padding: 24px 28px 18px;
  border-bottom: 2px solid var(--border-strong);
  background: var(--bg);
}
.modal-badge {
  width: 52px; height: 52px; border-radius: 16px; display: grid; place-items: center;
  color: var(--text); background: var(--c-pink);
  border: 2px solid var(--border-strong); box-shadow: var(--ink-drop-sm);
  transform: rotate(-3deg); margin-bottom: 14px;
}
.modal-title {
  font-family: var(--oj-font); font-weight: 900; font-size: 26px;
  margin: 0; color: var(--text);
}
.modal-meta {
  font-family: var(--oj-display); font-weight: 700; font-size: 12px; letter-spacing: 1px;
  color: var(--text-soft); margin: 6px 0 0;
}
.modal-x {
  position: absolute; top: 18px; right: 20px; width: 38px; height: 38px; border-radius: 999px;
  cursor: pointer; border: 2px solid var(--border-strong); background: var(--surface);
  color: var(--text);
  display: grid; place-items: center; transition: background 0.15s, transform 0.2s var(--ease-pop);
}
.modal-x:hover { background: var(--c-pink); transform: rotate(90deg); }
.modal-body { overflow-y: auto; padding: 24px 28px 8px; }
.legal-sec { margin-bottom: 22px; }
.legal-sec h4 {
  font-family: var(--oj-font); font-weight: 900; font-size: 16px; color: var(--text);
  margin: 0 0 8px; display: flex; align-items: baseline; gap: 10px;
}
.legal-sec h4 .num { font-family: var(--oj-display); font-size: 14px; color: var(--c-violet); font-weight: 700; }
.legal-sec p { margin: 0; font-size: 14px; font-weight: 500; line-height: 1.9; color: #333; }
.legal-sec p b { color: var(--text); font-weight: 900; font-family: var(--oj-display); }
.modal-foot {
  flex: none; padding: 18px 28px; border-top: 2px solid var(--border-strong);
  background: var(--bg);
  display: flex; gap: 14px; align-items: center;
}

/* ---------- shared pop button（黃色膠囊 + 墨色硬底影） ---------- */
.btn-pop {
  flex: 1; cursor: pointer; border: 2px solid var(--border-strong);
  font-family: var(--oj-font); font-weight: 900; font-size: 15px; color: var(--text);
  padding: 13px 20px; border-radius: 999px;
  background: var(--c-yellow);
  box-shadow: var(--ink-drop-sm);
  transition: transform 0.15s, box-shadow 0.15s;
  display: inline-flex; align-items: center; justify-content: center; gap: 8px;
}
.btn-pop:hover { transform: translateY(-2px); box-shadow: var(--ink-drop); }
.btn-pop:active { transform: translateY(2px); box-shadow: none; }

/* ---------- coach-marks ---------- */
.coach { position: fixed; inset: 0; z-index: 210; cursor: pointer; animation: scrim-in 0.22s ease; }
/* blurred dim-veil — same backdrop blur as the dialog scrim, with a
   rounded-rect hole (clip-path, set inline) over the spotlight so the
   highlighted icons stay sharp */
.coach-veil {
  position: fixed; inset: 0; pointer-events: none;
  background: rgba(26, 26, 26, 0.55);
  backdrop-filter: blur(4px);
  -webkit-backdrop-filter: blur(4px);
}
.coach-spot {
  position: fixed; border-radius: 999px; pointer-events: none;
  border: 2px solid rgba(255, 255, 255, 0.92);
  animation: spot-pulse 1.8s ease-in-out infinite;
}
@keyframes spot-pulse {
  0%, 100% { box-shadow: 0 0 0 0 rgba(255, 222, 0, 0.6); }
  50% { box-shadow: 0 0 0 7px rgba(255, 222, 0, 0); }
}
.coach-card {
  position: fixed; width: 300px; max-width: calc(100vw - 32px);
  background: var(--surface); border: 2px solid var(--border-strong); border-radius: 18px;
  box-shadow: 6px 6px 0 rgba(26, 26, 26, 0.9);
  padding: 16px 16px 14px; cursor: default;
  animation: modal-in 0.3s cubic-bezier(0.2, 1.15, 0.4, 1);
}
.coach-arrow {
  position: absolute; top: -9px; right: 26px; width: 14px; height: 14px;
  background: var(--surface);
  border-left: 2px solid var(--border-strong); border-top: 2px solid var(--border-strong);
  transform: rotate(45deg);
}
.coach-eyebrow {
  display: inline-block; margin: 0 0 12px; padding: 4px 12px;
  font-family: var(--oj-font); font-size: 11px; font-weight: 900; letter-spacing: 1px;
  color: var(--c-yellow); background: var(--text); border-radius: 999px;
  transform: rotate(-1deg); white-space: nowrap;
}
.coach-row {
  display: flex; align-items: center; gap: 11px; text-decoration: none;
  padding: 9px; border-radius: 12px; transition: background 0.15s;
}
.coach-row + .coach-row { margin-top: 4px; }
.coach-row:hover { background: var(--bg); }
.coach-ic {
  width: 38px; height: 38px; flex: none; border-radius: 12px; display: grid; place-items: center;
  color: var(--text); background: var(--c-cyan);
  border: 2px solid var(--border-strong); transform: rotate(-3deg);
}
.coach-row + .coach-row .coach-ic { background: var(--c-lime); transform: rotate(3deg); }
.coach-txt { display: flex; flex-direction: column; min-width: 0; }
.coach-txt b { font-family: var(--oj-font); font-weight: 900; font-size: 14px; color: var(--text); }
.coach-txt span { font-size: 12px; font-weight: 500; color: var(--text-soft); margin-top: 1px; }
.coach-done { width: 100%; margin-top: 14px; padding: 11px 18px; font-size: 14px; }

@media (max-width: 560px) {
  .modal-scrim { padding: 0; align-items: flex-end; }
  .modal-card {
    max-width: 100%; max-height: 90vh;
    border-radius: 24px 24px 0 0;
    box-shadow: 0 -8px 40px rgba(26, 26, 26, 0.4); border-bottom: 0;
  }
}
</style>
