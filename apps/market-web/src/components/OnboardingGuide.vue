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

function hasCookie(name: string): boolean {
  return document.cookie.split('; ').some((c) => c.startsWith(name + '='));
}
function setCookie(name: string, value: string, days: number) {
  const d = new Date();
  d.setTime(d.getTime() + days * 864e5);
  document.cookie = `${name}=${value}; expires=${d.toUTCString()}; path=/; SameSite=Lax`;
}

function measure() {
  const links = document.querySelectorAll<HTMLElement>('.nav-actions .nav-link');
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
  <!-- 1) intro modal — Auth legal-modal styling -->
  <div v-if="step === 'modal'" class="modal-scrim" @click.self="goCoach">
    <div class="modal-card" role="dialog" aria-modal="true" :aria-label="t('onboarding.modalAria')">
      <div class="modal-head">
        <div class="modal-badge info"><app-icon name="sparkle" :size="22" /></div>
        <h3 class="modal-title">{{ t('onboarding.title') }}</h3>
        <p class="modal-meta">{{ t('onboarding.meta') }}</p>
        <button class="modal-x" :aria-label="t('onboarding.closeAria')" @click="goCoach">
          <app-icon name="close" :size="16" :stroke="2.2" />
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
        <button class="btn-pop violet" @click="goCoach">
          <app-icon name="check" :size="17" :stroke="2.4" /> {{ t('onboarding.start') }}
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
      <button class="btn-pop violet coach-done" @click="finish">
        <app-icon name="check" :size="16" :stroke="2.4" /> {{ t('onboarding.coachDone') }}
      </button>
    </div>
  </div>
</template>

<style scoped>
/* ---------- intro modal (ported from Auth legal-modal) ---------- */
.modal-scrim {
  position: fixed; inset: 0; z-index: 200;
  background: rgba(26, 22, 38, 0.42); backdrop-filter: blur(4px);
  display: grid; place-items: center; padding: 28px;
  animation: scrim-in 0.22s ease;
}
@keyframes scrim-in { from { opacity: 0; } to { opacity: 1; } }
.modal-card {
  width: 100%; max-width: 520px; max-height: min(82vh, 720px);
  background: var(--surface); border: 1.5px solid var(--border-strong); border-radius: var(--r-lg);
  box-shadow: 8px 8px 0 var(--text), 0 40px 90px -30px rgba(20, 16, 60, 0.6);
  display: flex; flex-direction: column; overflow: hidden;
  animation: modal-in 0.34s cubic-bezier(0.2, 1.15, 0.4, 1);
}
@keyframes modal-in {
  from { opacity: 0; transform: translateY(18px) scale(0.97); }
  to { opacity: 1; transform: none; }
}
.modal-head {
  position: relative; flex: none; padding: 24px 26px 18px;
  border-bottom: 1.5px solid var(--border);
  background: radial-gradient(300px 120px at 0% 0%, rgba(108, 76, 241, 0.1), transparent 70%), var(--surface);
}
.modal-badge {
  width: 44px; height: 44px; border-radius: 13px; display: grid; place-items: center;
  color: #fff; border: 1.5px solid var(--text); box-shadow: 3px 3px 0 var(--text); margin-bottom: 14px;
}
.modal-badge.info { background: linear-gradient(135deg, var(--c-violet), var(--c-cyan)); }
.modal-title {
  font-family: var(--oj-display); font-weight: 800; font-size: 25px;
  letter-spacing: -0.7px; margin: 0; color: var(--text);
}
.modal-meta {
  font-family: var(--oj-mono); font-size: 11.5px; letter-spacing: 0.06em;
  color: var(--text-faint); margin: 7px 0 0; text-transform: uppercase;
}
.modal-x {
  position: absolute; top: 20px; right: 20px; width: 36px; height: 36px; border-radius: 10px;
  cursor: pointer; border: 1.5px solid var(--border-strong); background: var(--surface);
  box-shadow: 2px 2px 0 var(--border-strong); color: var(--text);
  display: grid; place-items: center; transition: transform 0.14s, box-shadow 0.14s, color 0.14s;
}
.modal-x:hover { transform: translate(-1px, -1px); box-shadow: 3px 3px 0 var(--border-strong); color: var(--c-pink); }
.modal-body { overflow-y: auto; padding: 22px 26px 8px; }
.legal-sec { margin-bottom: 20px; }
.legal-sec h4 {
  font-family: var(--oj-display); font-weight: 700; font-size: 16px; color: var(--text);
  margin: 0 0 8px; display: flex; align-items: baseline; gap: 9px;
}
.legal-sec h4 .num { font-family: var(--oj-mono); font-size: 12px; color: var(--oj-primary); font-weight: 600; }
.legal-sec p { margin: 0; font-size: 14.5px; line-height: 1.72; color: var(--text-soft); }
.legal-sec p b { color: var(--text); font-weight: 700; }
.modal-foot {
  flex: none; padding: 16px 26px 20px; border-top: 1.5px solid var(--border);
  display: flex; gap: 12px; align-items: center; justify-content: flex-end;
}

/* ---------- shared pop button ---------- */
.btn-pop {
  cursor: pointer; border: 1.5px solid var(--text);
  font-family: var(--oj-font); font-weight: 700; font-size: 15px; color: #1a1626;
  padding: 13px 20px; border-radius: 14px;
  background: linear-gradient(135deg, var(--c-yellow), var(--c-orange));
  box-shadow: 4px 4px 0 var(--text);
  transition: transform 0.14s, box-shadow 0.14s;
  display: inline-flex; align-items: center; justify-content: center; gap: 9px;
}
.btn-pop:hover { transform: translate(-2px, -2px); box-shadow: 6px 6px 0 var(--text); }
.btn-pop:active { transform: translate(1px, 1px); box-shadow: 1px 1px 0 var(--text); }
.btn-pop.violet { background: linear-gradient(135deg, var(--c-violet), var(--c-pink)); color: #fff; }

/* ---------- coach-marks ---------- */
.coach { position: fixed; inset: 0; z-index: 210; cursor: pointer; animation: scrim-in 0.22s ease; }
/* blurred dim-veil — same backdrop blur as the dialog scrim, with a
   rounded-rect hole (clip-path, set inline) over the spotlight so the
   highlighted icons stay sharp */
.coach-veil {
  position: fixed; inset: 0; pointer-events: none;
  background: rgba(20, 16, 40, 0.62);
  backdrop-filter: blur(4px);
  -webkit-backdrop-filter: blur(4px);
}
.coach-spot {
  position: fixed; border-radius: 14px; pointer-events: none;
  border: 2px solid rgba(255, 255, 255, 0.92);
  animation: spot-pulse 1.8s ease-in-out infinite;
}
@keyframes spot-pulse {
  0%, 100% { box-shadow: 0 0 0 0 rgba(255, 255, 255, 0.5); }
  50% { box-shadow: 0 0 0 7px rgba(255, 255, 255, 0); }
}
.coach-card {
  position: fixed; width: 300px; max-width: calc(100vw - 32px);
  background: var(--surface); border: 1.5px solid var(--border-strong); border-radius: var(--r-md);
  box-shadow: 6px 6px 0 var(--text), 0 30px 60px -28px rgba(20, 16, 60, 0.6);
  padding: 16px 16px 14px; cursor: default;
  animation: modal-in 0.3s cubic-bezier(0.2, 1.15, 0.4, 1);
}
.coach-arrow {
  position: absolute; top: -8px; right: 26px; width: 14px; height: 14px;
  background: var(--surface);
  border-left: 1.5px solid var(--border-strong); border-top: 1.5px solid var(--border-strong);
  transform: rotate(45deg);
}
.coach-eyebrow {
  font-family: var(--oj-mono); font-size: 11px; letter-spacing: 0.12em; text-transform: uppercase;
  color: var(--oj-primary); margin: 0 0 12px;
}
.coach-row {
  display: flex; align-items: center; gap: 11px; text-decoration: none;
  padding: 9px; border-radius: var(--r-sm); transition: background 0.14s;
}
.coach-row + .coach-row { margin-top: 2px; }
.coach-row:hover { background: var(--oj-wash); }
.coach-ic {
  width: 38px; height: 38px; flex: none; border-radius: 11px; display: grid; place-items: center;
  color: var(--text); background: var(--surface);
  border: 1.5px solid var(--border-strong); box-shadow: 2px 2px 0 var(--border-strong);
}
.coach-txt { display: flex; flex-direction: column; min-width: 0; }
.coach-txt b { font-family: var(--oj-display); font-weight: 700; font-size: 14.5px; color: var(--text); letter-spacing: -0.2px; }
.coach-txt span { font-size: 12px; color: var(--text-soft); margin-top: 1px; }
.coach-done { width: 100%; margin-top: 14px; padding: 11px 18px; font-size: 14px; border-radius: 12px; }

@media (max-width: 560px) {
  .modal-scrim { padding: 0; align-items: flex-end; }
  .modal-card {
    max-width: 100%; max-height: 90vh;
    border-radius: var(--r-lg) var(--r-lg) 0 0;
    box-shadow: 0 -8px 40px rgba(20, 16, 60, 0.4); border-bottom: 0;
  }
}
</style>
