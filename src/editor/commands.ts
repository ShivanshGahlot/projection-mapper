// Small project-level commands used by the top bar and the keyboard.

import type { Core } from './layerOps';

export function setPaused(core: Core, paused: boolean): void {
  if (core.store.state.playback.paused === paused) return;
  const now = performance.timeOrigin + performance.now();
  core.store.update((p) => {
    const sync = p.playback.videoSync;
    if (sync) {
      if (paused) sync.position += Math.max(0, now - sync.at) / 1000;
      sync.at = now;
    }
    p.playback.paused = paused;
  }, { history: 'none' });
}

export function togglePause(core: Core): void {
  setPaused(core, !core.store.state.playback.paused);
}

/** One shared epoch reaches both renderers before playback begins. */
export function syncVideos(core: Core): void {
  core.store.update((p) => {
    p.playback.videoSync = { at: performance.timeOrigin + performance.now() + 750, position: 0 };
    p.playback.paused = false;
  }, { history: 'none' });
}

export function toggleBlackout(core: Core): void {
  core.store.update((p) => { p.output.blackout = !p.output.blackout; }, { history: 'none' });
}

/** Runs an undo/redo step without touching live state: pause and black-out are not edits. */
function keepingLiveState(core: Core, step: () => void): void {
  const { paused, videoSync } = structuredClone(core.store.state.playback);
  const { blackout } = core.store.state.output;
  step();
  const p = core.store.state;
  if (p.playback.paused === paused && JSON.stringify(p.playback.videoSync) === JSON.stringify(videoSync) && p.output.blackout === blackout) return;
  core.store.update((s) => {
    s.playback.paused = paused;
    s.playback.videoSync = videoSync;
    s.output.blackout = blackout;
  }, { history: 'none' });
}

export function undo(core: Core): void {
  keepingLiveState(core, () => core.store.undo());
}

export function redo(core: Core): void {
  keepingLiveState(core, () => core.store.redo());
}
