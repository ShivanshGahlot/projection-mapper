// Small project-level commands used by the top bar and the keyboard.

import type { Core } from './layerOps';

export function togglePause(core: Core): void {
  core.store.update((p) => { p.playback.paused = !p.playback.paused; }, { history: 'none' });
}

export function toggleBlackout(core: Core): void {
  core.store.update((p) => { p.output.blackout = !p.output.blackout; }, { history: 'none' });
}
