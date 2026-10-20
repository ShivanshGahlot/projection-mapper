// Debounced autosave of the current project, flushed when the page is hidden.

import type { Project } from '../state/types';
import { projectStorage } from './db';

const DEBOUNCE_MS = 400;

export interface Autosave {
  /** Saves immediately if a change is pending; resolves when the write is done. */
  flush(): Promise<void>;
  stop(): void;
}

export function startAutosave(getProject: () => Project, subscribe: (cb: () => void) => () => void): Autosave {
  let timer: number | null = null;
  let dirty = false;
  let inflight: Promise<void> = Promise.resolve();

  const write = (): Promise<void> => {
    dirty = false;
    const project = getProject();
    inflight = inflight
      .then(() => projectStorage.save(project))
      .then(() => projectStorage.setLastOpenedId(project.id))
      .catch((e) => console.error('Autosave failed', e));
    return inflight;
  };

  const flush = (): Promise<void> => {
    if (timer !== null) {
      clearTimeout(timer);
      timer = null;
    }
    return dirty ? write() : inflight;
  };

  const unsubscribe = subscribe(() => {
    dirty = true;
    if (timer !== null) clearTimeout(timer);
    timer = window.setTimeout(() => {
      timer = null;
      void write();
    }, DEBOUNCE_MS);
  });

  const onVisibility = () => {
    if (document.visibilityState === 'hidden') void flush();
  };
  const onPageHide = () => void flush();
  document.addEventListener('visibilitychange', onVisibility);
  window.addEventListener('pagehide', onPageHide);

  return {
    flush,
    stop() {
      unsubscribe();
      document.removeEventListener('visibilitychange', onVisibility);
      window.removeEventListener('pagehide', onPageHide);
      if (timer !== null) clearTimeout(timer);
      timer = null;
    },
  };
}
