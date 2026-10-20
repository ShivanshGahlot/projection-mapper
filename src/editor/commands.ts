// Small project-level commands used by the top bar and the keyboard.

import type { Core } from './layerOps';

export function togglePause(core: Core): void {
  core.store.update((p) => { p.playback.paused = !p.playback.paused; }, { history: 'none' });
}

export function toggleBlackout(core: Core): void {
  core.store.update((p) => { p.output.blackout = !p.output.blackout; }, { history: 'none' });
}

/** Runs an undo/redo step without touching live state: pause and black-out are not edits. */
function keepingLiveState(core: Core, step: () => void): void {
  const { paused } = core.store.state.playback;
  const { blackout } = core.store.state.output;
  step();
  const p = core.store.state;
  if (p.playback.paused === paused && p.output.blackout === blackout) return;
  core.store.update((s) => {
    s.playback.paused = paused;
    s.output.blackout = blackout;
  }, { history: 'none' });
}

export function undo(core: Core): void {
  keepingLiveState(core, () => core.store.undo());
}

export function redo(core: Core): void {
  keepingLiveState(core, () => core.store.redo());
}
