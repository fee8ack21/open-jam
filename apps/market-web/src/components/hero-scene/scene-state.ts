/* ============================================================
   scene-state — pure simulation for the hero "building the park"
   background animation. No DOM / canvas access so it can be unit
   tested in isolation. All horizontal positions are normalised
   (0 = left edge, 1 = right edge); the renderer scales to pixels.
   ============================================================ */

/** Drifting background cloud. */
export interface Cloud {
  x: number;     // normalised horizontal position
  y: number;     // vertical position in px from the top of the stage
  scale: number; // size multiplier
  speed: number; // normalised units per ms
}

/** A hard-hat worker walking across the site carrying a crate. */
export interface Worker {
  x: number;     // normalised horizontal position
  speed: number; // normalised units per ms
  carry: number; // palette index of the carried crate
  phase: number; // leg-swing animation phase (radians)
}

/** Whole-scene simulation state. */
export interface SceneState {
  clouds: Cloud[];
  workers: Worker[];
  bricks: number;       // bricks currently stacked, 0..MAX_BRICKS
  buildTimerMs: number; // time accumulated toward the next brick
  holdMs: number;       // time held after completion before resetting
  complete: boolean;    // true while the flag/sparkles show
}

export const BRICK_ROWS = 5;
export const BRICK_COLS = 3;
export const MAX_BRICKS = BRICK_ROWS * BRICK_COLS;

/** ms between each brick being laid. */
export const BRICK_INTERVAL_MS = 620;
/** ms to celebrate (flag + sparkles) before the build loops. */
export const HOLD_MS = 2000;

/** Number of palette colours the renderer cycles through. */
export const PALETTE_SIZE = 5;

const WORKER_RESET_X = -0.08;
const WORKER_WRAP_X = 1.08;
const CLOUD_RESET_X = -0.2;
const CLOUD_WRAP_X = 1.2;

/** Build the initial, deterministic scene state. */
export function createScene(): SceneState {
  return {
    clouds: [
      { x: 0.12, y: 46, scale: 1.0, speed: 0.00003 },
      { x: 0.52, y: 32, scale: 0.8, speed: 0.000022 },
      { x: 0.84, y: 60, scale: 1.15, speed: 0.000034 },
    ],
    workers: [
      { x: 0.05, speed: 0.00016, carry: 0, phase: 0 },
      { x: 0.40, speed: 0.00013, carry: 1, phase: 2 },
      { x: 0.72, speed: 0.00018, carry: 2, phase: 4 },
      { x: 0.94, speed: 0.00015, carry: 4, phase: 1 },
    ],
    bricks: 0,
    buildTimerMs: 0,
    holdMs: 0,
    complete: false,
  };
}

/**
 * Advance the simulation by `dtMs` milliseconds. Mutates and returns the
 * same state object. Deterministic: identical inputs yield identical output.
 */
export function updateScene(state: SceneState, dtMs: number): SceneState {
  // clamp dt so a backgrounded tab resuming doesn't teleport everything
  const dt = Math.min(dtMs, 100);

  for (const c of state.clouds) {
    c.x += c.speed * dt;
    if (c.x > CLOUD_WRAP_X) c.x = CLOUD_RESET_X;
  }

  for (const w of state.workers) {
    w.x += w.speed * dt;
    w.phase += dt * 0.012;
    if (w.x > WORKER_WRAP_X) {
      w.x = WORKER_RESET_X;
      // pick the next crate colour so the loop stays visually varied
      w.carry = (w.carry + 2) % PALETTE_SIZE;
    }
  }

  if (state.complete) {
    state.holdMs += dt;
    if (state.holdMs >= HOLD_MS) {
      state.bricks = 0;
      state.holdMs = 0;
      state.complete = false;
    }
  } else {
    state.buildTimerMs += dt;
    if (state.buildTimerMs >= BRICK_INTERVAL_MS) {
      state.buildTimerMs = 0;
      state.bricks += 1;
      if (state.bricks >= MAX_BRICKS) {
        state.bricks = MAX_BRICKS;
        state.complete = true;
      }
    }
  }

  return state;
}

/** A finished, fully-built frame — used when motion is reduced. */
export function completedScene(): SceneState {
  const state = createScene();
  state.bricks = MAX_BRICKS;
  state.complete = true;
  return state;
}
