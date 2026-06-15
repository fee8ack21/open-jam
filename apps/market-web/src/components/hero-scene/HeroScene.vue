<script setup lang="ts">
/* ============================================================
   HeroScene — looping "creators building the park" background
   animation for the market hero. Pure simulation lives in
   scene-state.ts; this component only draws it to a canvas,
   adds gentle mouse parallax, and manages the rAF lifecycle.
   Decorative only: pointer-events are off so it never blocks
   the search box. Hidden on phones via CSS; pauses when the
   hero scrolls off-screen and respects prefers-reduced-motion.
   ============================================================ */
import { onMounted, onBeforeUnmount, ref } from 'vue';
import {
  createScene,
  completedScene,
  updateScene,
  BRICK_ROWS,
  BRICK_COLS,
  MAX_BRICKS,
  WHEEL_CABINS,
  type SceneState,
  type CharKind,
} from './scene-state';

const root = ref<HTMLDivElement | null>(null);
const canvas = ref<HTMLCanvasElement | null>(null);

const OUTLINE = '#1a1626';
const PALETTE = ['#6c4cf1', '#ff4d9d', '#aef03e', '#1fd6c6', '#ffc83a'];

let ctx: CanvasRenderingContext2D;
let state: SceneState;
let raf = 0;
let lastT = 0;
let w = 0;
let h = 0;
let groundY = 0;

// parallax: target follows the pointer, current eases toward it
const px = { cur: 0, tgt: 0 };
let reduced = false;
let visible = true;

function resize() {
  const el = root.value;
  const cv = canvas.value;
  if (!el || !cv) return;
  const dpr = window.devicePixelRatio || 1;
  w = el.clientWidth;
  h = el.clientHeight;
  cv.width = w * dpr;
  cv.height = h * dpr;
  ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
  groundY = h - 54;
}

/* ---------- drawing primitives ---------- */
function rrect(x: number, y: number, bw: number, bh: number, r: number) {
  ctx.beginPath();
  ctx.roundRect(x, y, bw, bh, r);
}

function drawCloud(cx: number, cy: number, s: number) {
  ctx.fillStyle = '#fff';
  ctx.strokeStyle = OUTLINE;
  ctx.lineWidth = 2;
  ctx.beginPath();
  ctx.arc(cx, cy, 12 * s, 0, 7);
  ctx.arc(cx + 15 * s, cy + 4 * s, 15 * s, 0, 7);
  ctx.arc(cx + 32 * s, cy, 11 * s, 0, 7);
  ctx.rect(cx, cy, 32 * s, 13 * s);
  ctx.fill();
  ctx.stroke();
}

/** A finished creator storefront: striped awning over a coloured door. */
function drawStall(x: number, col: string) {
  ctx.save();
  ctx.translate(x, groundY);
  ctx.lineWidth = 2.4;
  ctx.strokeStyle = OUTLINE;
  ctx.fillStyle = '#fff';
  rrect(-21, -44, 42, 44, 5);
  ctx.fill();
  ctx.stroke();
  ctx.fillStyle = col;
  rrect(-8, -25, 16, 25, 3);
  ctx.fill();
  ctx.stroke();
  const seg = 9.8;
  for (let i = 0; i < 5; i++) {
    ctx.fillStyle = i % 2 ? col : '#fff';
    ctx.beginPath();
    ctx.moveTo(-25 + i * seg, -44);
    ctx.lineTo(-25 + (i + 1) * seg, -44);
    ctx.lineTo(-25 + (i + 1) * seg - 5, -36);
    ctx.lineTo(-25 + i * seg - 5, -36);
    ctx.closePath();
    ctx.fill();
    ctx.stroke();
  }
  ctx.restore();
}

/** A crate carried overhead, with two little arms reaching up to it. */
function drawCarriedCrate(carryCol: string) {
  ctx.fillStyle = carryCol;
  rrect(-8, -46, 16, 11, 3);
  ctx.fill();
  ctx.stroke();
  ctx.beginPath();
  ctx.moveTo(-6, -26);
  ctx.lineTo(-7, -37);
  ctx.moveTo(6, -26);
  ctx.lineTo(7, -37);
  ctx.stroke();
}

/** A hard-hat worker with swinging legs, optionally carrying a crate. */
function drawWorker(x: number, phase: number, carryCol: string | null, bodyCol: string) {
  const bob = Math.abs(Math.sin(phase * 2)) * 1.5;
  ctx.save();
  ctx.translate(x, groundY - bob);
  ctx.lineWidth = 2.4;
  ctx.strokeStyle = OUTLINE;
  const swing = Math.sin(phase) * 4;
  ctx.beginPath();
  ctx.moveTo(-3, -12);
  ctx.lineTo(-3 + swing, 0);
  ctx.moveTo(3, -12);
  ctx.lineTo(3 - swing, 0);
  ctx.stroke();
  ctx.fillStyle = bodyCol;
  rrect(-6, -26, 12, 15, 4);
  ctx.fill();
  ctx.stroke();
  ctx.fillStyle = '#ffe0c2';
  ctx.beginPath();
  ctx.arc(0, -31, 6, 0, 7);
  ctx.fill();
  ctx.stroke();
  ctx.fillStyle = '#ffc83a';
  ctx.beginPath();
  ctx.arc(0, -33, 6.5, Math.PI, 0);
  ctx.fill();
  ctx.stroke();
  ctx.beginPath();
  ctx.moveTo(-7, -33);
  ctx.lineTo(7, -33);
  ctx.stroke();
  if (carryCol) drawCarriedCrate(carryCol);
  ctx.restore();
}

/** A round little monster that hops along, eyes + horns, optional crate. */
function drawMonster(x: number, phase: number, carryCol: string | null, bodyCol: string) {
  const hop = Math.max(0, Math.sin(phase * 1.4)) * 7; // springy bounce
  ctx.save();
  ctx.translate(x, groundY - hop);
  ctx.lineWidth = 2.4;
  ctx.strokeStyle = OUTLINE;
  // feet (tuck up while airborne)
  const tuck = hop * 0.4;
  ctx.beginPath();
  ctx.moveTo(-5, -8);
  ctx.lineTo(-5, -2 + tuck);
  ctx.moveTo(5, -8);
  ctx.lineTo(5, -2 + tuck);
  ctx.stroke();
  // horns
  ctx.fillStyle = bodyCol;
  ctx.beginPath();
  ctx.moveTo(-6, -24);
  ctx.lineTo(-9, -32);
  ctx.lineTo(-2, -26);
  ctx.closePath();
  ctx.moveTo(6, -24);
  ctx.lineTo(9, -32);
  ctx.lineTo(2, -26);
  ctx.closePath();
  ctx.fill();
  ctx.stroke();
  // body blob
  ctx.beginPath();
  ctx.arc(0, -15, 11, 0, 7);
  ctx.fill();
  ctx.stroke();
  // big eyes
  ctx.fillStyle = '#fff';
  ctx.beginPath();
  ctx.arc(-4, -17, 4, 0, 7);
  ctx.arc(4, -17, 4, 0, 7);
  ctx.fill();
  ctx.stroke();
  ctx.fillStyle = OUTLINE;
  ctx.beginPath();
  ctx.arc(-3.5, -16.5, 1.7, 0, 7);
  ctx.arc(4.5, -16.5, 1.7, 0, 7);
  ctx.fill();
  if (carryCol) {
    ctx.fillStyle = carryCol;
    rrect(-8, -40, 16, 11, 3);
    ctx.fill();
    ctx.stroke();
  }
  ctx.restore();
}

/** A rhinoceros beetle scuttling low, with a horn and a crate on its back. */
function drawBeetle(x: number, phase: number, carryCol: string | null, bodyCol: string) {
  const bob = Math.abs(Math.sin(phase * 2)) * 1;
  ctx.save();
  ctx.translate(x, groundY - bob);
  ctx.lineWidth = 2.2;
  ctx.strokeStyle = OUTLINE;
  // six little legs
  const s = Math.sin(phase) * 2.5;
  for (const lx of [-7, 0, 7]) {
    ctx.beginPath();
    ctx.moveTo(lx, -6);
    ctx.lineTo(lx + s, 0);
    ctx.stroke();
  }
  // shell
  ctx.fillStyle = bodyCol;
  ctx.beginPath();
  ctx.ellipse(0, -11, 13, 8, 0, 0, 7);
  ctx.fill();
  ctx.stroke();
  // wing-case split line
  ctx.beginPath();
  ctx.moveTo(2, -18);
  ctx.lineTo(2, -4);
  ctx.stroke();
  // head + horn (獨角仙)
  ctx.fillStyle = bodyCol;
  ctx.beginPath();
  ctx.arc(13, -11, 4.5, 0, 7);
  ctx.fill();
  ctx.stroke();
  ctx.beginPath();
  ctx.moveTo(16, -13);
  ctx.lineTo(23, -19);
  ctx.lineTo(19, -11);
  ctx.closePath();
  ctx.fill();
  ctx.stroke();
  if (carryCol) {
    ctx.fillStyle = carryCol;
    rrect(-9, -27, 16, 11, 3);
    ctx.fill();
    ctx.stroke();
  }
  ctx.restore();
}

/** Dispatch to the right creature renderer. */
function drawCharacter(kind: CharKind, x: number, phase: number, carryCol: string, tint: string) {
  if (kind === 'monster') drawMonster(x, phase, carryCol, tint);
  else if (kind === 'beetle') drawBeetle(x, phase, carryCol, tint);
  else drawWorker(x, phase, carryCol, tint);
}

/** A slowly turning ferris wheel — the centrepiece of the little park. */
function drawFerrisWheel(cx: number, baseY: number, r: number, angle: number) {
  const hubY = baseY - r - 8;
  ctx.lineWidth = 2.4;
  ctx.strokeStyle = 'rgba(26,22,38,.75)';
  // A-frame support legs
  ctx.beginPath();
  ctx.moveTo(cx - 22, baseY);
  ctx.lineTo(cx, hubY);
  ctx.lineTo(cx + 22, baseY);
  ctx.stroke();
  // outer rim
  ctx.beginPath();
  ctx.arc(cx, hubY, r, 0, 7);
  ctx.stroke();
  // spokes + cabins
  for (let i = 0; i < WHEEL_CABINS; i++) {
    const a = angle + (i * Math.PI * 2) / WHEEL_CABINS;
    const ex = cx + Math.cos(a) * r;
    const ey = hubY + Math.sin(a) * r;
    ctx.strokeStyle = 'rgba(26,22,38,.55)';
    ctx.beginPath();
    ctx.moveTo(cx, hubY);
    ctx.lineTo(ex, ey);
    ctx.stroke();
    // cabin hangs below the rim point, upright regardless of rotation
    ctx.fillStyle = PALETTE[i % PALETTE.length];
    ctx.strokeStyle = OUTLINE;
    ctx.lineWidth = 2;
    rrect(ex - 6, ey + 1, 12, 9, 3);
    ctx.fill();
    ctx.stroke();
  }
  // hub
  ctx.fillStyle = '#fff';
  ctx.strokeStyle = OUTLINE;
  ctx.lineWidth = 2.2;
  ctx.beginPath();
  ctx.arc(cx, hubY, 4, 0, 7);
  ctx.fill();
  ctx.stroke();
}

/** A worker beside the build site swinging a hammer. */
function drawBuilder(x: number, t: number) {
  const swing = Math.sin(t * 0.012) * 0.5;
  ctx.save();
  ctx.translate(x, groundY);
  ctx.lineWidth = 2.4;
  ctx.strokeStyle = OUTLINE;
  ctx.beginPath();
  ctx.moveTo(-3, -12);
  ctx.lineTo(-3, 0);
  ctx.moveTo(3, -12);
  ctx.lineTo(3, 0);
  ctx.stroke();
  ctx.fillStyle = '#1fd6c6';
  rrect(-6, -26, 12, 15, 4);
  ctx.fill();
  ctx.stroke();
  ctx.fillStyle = '#ffe0c2';
  ctx.beginPath();
  ctx.arc(0, -31, 6, 0, 7);
  ctx.fill();
  ctx.stroke();
  ctx.fillStyle = '#ffc83a';
  ctx.beginPath();
  ctx.arc(0, -33, 6.5, Math.PI, 0);
  ctx.fill();
  ctx.stroke();
  ctx.save();
  ctx.translate(6, -22);
  ctx.rotate(-0.6 + swing);
  ctx.strokeStyle = OUTLINE;
  ctx.beginPath();
  ctx.moveTo(0, 0);
  ctx.lineTo(10, 0);
  ctx.stroke();
  ctx.fillStyle = OUTLINE;
  rrect(9, -3, 6, 6, 1);
  ctx.fill();
  ctx.restore();
  ctx.restore();
}

/** The growing brick tower, with flag + sparkles once complete. */
function drawBuilding(bx: number, t: number) {
  const bw = 18;
  const bh = 11;
  ctx.lineWidth = 2.2;
  ctx.strokeStyle = 'rgba(26,22,38,.5)';
  ctx.beginPath();
  ctx.moveTo(bx - (BRICK_COLS * bw) / 2 - 6, groundY);
  ctx.lineTo(bx - (BRICK_COLS * bw) / 2 - 6, groundY - BRICK_ROWS * bh - 8);
  ctx.moveTo(bx + (BRICK_COLS * bw) / 2 + 4, groundY);
  ctx.lineTo(bx + (BRICK_COLS * bw) / 2 + 4, groundY - BRICK_ROWS * bh - 8);
  ctx.stroke();

  ctx.lineWidth = 2.2;
  ctx.strokeStyle = OUTLINE;
  for (let i = 0; i < state.bricks; i++) {
    const row = Math.floor(i / BRICK_COLS);
    const col = i % BRICK_COLS;
    ctx.fillStyle = PALETTE[(row + col) % PALETTE.length];
    rrect(bx + col * bw - (BRICK_COLS * bw) / 2, groundY - (row + 1) * bh, bw - 1, bh - 1, 2);
    ctx.fill();
    ctx.stroke();
  }

  if (state.bricks >= MAX_BRICKS) {
    const top = groundY - BRICK_ROWS * bh;
    ctx.strokeStyle = OUTLINE;
    ctx.lineWidth = 2.2;
    ctx.beginPath();
    ctx.moveTo(bx, top);
    ctx.lineTo(bx, top - 22);
    ctx.stroke();
    ctx.fillStyle = '#ff4d9d';
    ctx.beginPath();
    ctx.moveTo(bx, top - 22);
    ctx.lineTo(bx + 16, top - 18);
    ctx.lineTo(bx, top - 14);
    ctx.closePath();
    ctx.fill();
    ctx.stroke();
    for (let k = 0; k < 6; k++) {
      const a = t * 0.004 + k;
      ctx.fillStyle = PALETTE[k % PALETTE.length];
      ctx.beginPath();
      ctx.arc(
        bx + Math.cos(a) * (26 + Math.sin(t * 0.003 + k) * 6),
        top - 6 + Math.sin(a) * 18,
        2,
        0,
        7,
      );
      ctx.fill();
    }
  }
}

/* ---------- frame ---------- */
function render(t: number) {
  ctx.clearRect(0, 0, w, h);

  // ease parallax toward the pointer target
  px.cur += (px.tgt - px.cur) * 0.06;
  const far = px.cur * 8; // clouds drift least
  const near = px.cur * 22; // workers / building drift most

  for (const c of state.clouds) {
    drawCloud(c.x * w + far, c.y, c.scale);
  }

  // grassy band fading down into the cream page background (no hard cut-off)
  const grass = ctx.createLinearGradient(0, groundY, 0, h);
  grass.addColorStop(0, 'rgba(174,240,62,.20)');
  grass.addColorStop(1, 'rgba(174,240,62,0)');
  ctx.fillStyle = grass;
  ctx.fillRect(0, groundY, w, h - groundY);
  // soft ground line (fades out at both ends so it doesn't hit the screen edges hard)
  const line = ctx.createLinearGradient(0, 0, w, 0);
  line.addColorStop(0, 'rgba(26,22,38,0)');
  line.addColorStop(0.12, 'rgba(26,22,38,.85)');
  line.addColorStop(0.88, 'rgba(26,22,38,.85)');
  line.addColorStop(1, 'rgba(26,22,38,0)');
  ctx.strokeStyle = line;
  ctx.lineWidth = 2.6;
  ctx.beginPath();
  ctx.moveTo(0, groundY);
  ctx.lineTo(w, groundY);
  ctx.stroke();

  // ferris wheel sits in the mid-ground (behind the workers / building)
  const mid = px.cur * 14;
  ctx.save();
  ctx.translate(mid, 0);
  drawFerrisWheel(w * 0.8, groundY, 46, state.wheelAngle);
  ctx.restore();

  ctx.save();
  ctx.translate(near, 0);
  drawStall(w * 0.1, '#6c4cf1');
  drawStall(w * 0.22, '#ff4d9d');

  const bx = w * 0.5;
  drawBuilding(bx, t);
  drawBuilder(bx - 34, t);

  for (const wk of state.workers) {
    drawCharacter(wk.kind, wk.x * w, wk.phase, PALETTE[wk.carry], PALETTE[wk.tint]);
  }
  ctx.restore();
}

function frame(t: number) {
  const dt = lastT ? t - lastT : 16;
  lastT = t;
  if (visible) {
    updateScene(state, dt);
    render(t);
  }
  raf = requestAnimationFrame(frame);
}

/* ---------- pointer parallax ---------- */
// The scene has pointer-events: none, so track the pointer on window and
// measure it against the hero bounds. Outside the hero, ease back to centre.
function onPointerMove(e: PointerEvent) {
  const el = root.value;
  if (!el) return;
  const rect = el.getBoundingClientRect();
  const inside =
    e.clientX >= rect.left &&
    e.clientX <= rect.right &&
    e.clientY >= rect.top &&
    e.clientY <= rect.bottom;
  px.tgt = inside ? ((e.clientX - rect.left) / rect.width - 0.5) * 2 : 0;
}

let ro: ResizeObserver | null = null;
let io: IntersectionObserver | null = null;

onMounted(() => {
  const cv = canvas.value;
  const el = root.value;
  if (!cv || !el) return;
  ctx = cv.getContext('2d')!;

  reduced = window.matchMedia?.('(prefers-reduced-motion: reduce)').matches ?? false;
  resize();

  ro = new ResizeObserver(resize);
  ro.observe(el);

  if (reduced) {
    // a single static, finished frame — no animation, no parallax
    state = completedScene();
    render(performance.now());
    return;
  }

  state = createScene();
  window.addEventListener('pointermove', onPointerMove, { passive: true });

  io = new IntersectionObserver(
    ([entry]) => {
      visible = entry.isIntersecting;
    },
    { threshold: 0 },
  );
  io.observe(el);

  raf = requestAnimationFrame(frame);
});

onBeforeUnmount(() => {
  cancelAnimationFrame(raf);
  ro?.disconnect();
  io?.disconnect();
  window.removeEventListener('pointermove', onPointerMove);
});
</script>

<template>
  <div ref="root" class="hero-scene" aria-hidden="true">
    <canvas ref="canvas"></canvas>
  </div>
</template>

<style scoped>
.hero-scene {
  position: absolute;
  top: 0;
  bottom: 0;
  left: 50%;
  width: 100vw; /* full-bleed: break out of the padded .page container */
  transform: translateX(-50%); /* safe — .oj-root has overflow-x: clip */
  z-index: 1; /* above background shapes, below .mkt-hero-inner */
  pointer-events: none; /* purely decorative — never intercepts clicks */
  overflow: hidden;
}
.hero-scene canvas {
  position: absolute;
  inset: 0;
  width: 100%;
  height: 100%;
}
@media (max-width: 600px) {
  .hero-scene {
    display: none;
  }
}
</style>
