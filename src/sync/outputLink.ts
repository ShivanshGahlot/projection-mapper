// Editor-side link to the output window: sends the project, opens/places the
// window on the projector and tracks its status.

import type { Project } from '../state/types';
import { CHANNEL_NAME, type EditorToOutput, type OutputToEditor } from './messages';

export type OutputStatus = Extract<OutputToEditor, { type: 'status' }>;

export interface OutputLinkStatus {
  state: 'disconnected' | 'connected';
  /** Last status report from the output window; null while disconnected. */
  info: OutputStatus | null;
}

const STALE_MS = 3000;

interface ScreenDetailed {
  isPrimary: boolean;
  availLeft: number;
  availTop: number;
  width: number;
  height: number;
}

export class OutputLink {
  private channel = new BroadcastChannel(CHANNEL_NAME);
  private pending: Project | null = null;
  private lastProject: Project | null = null;
  private raf = 0;
  private win: Window | null = null;
  private current: OutputLinkStatus = { state: 'disconnected', info: null };
  private lastSeen = 0;
  private listeners = new Set<(s: OutputLinkStatus) => void>();
  private watchdog: number;
  private outputUrl = new URL('output.html', location.href).href;

  constructor() {
    this.channel.onmessage = (e: MessageEvent<OutputToEditor>) => this.onMessage(e.data);
    this.watchdog = window.setInterval(() => {
      if (this.current.state === 'connected' && performance.now() - this.lastSeen > STALE_MS) this.setDisconnected();
    }, 500);
    this.post({ type: 'ping' });
  }

  get status(): OutputLinkStatus {
    return this.current;
  }

  /** Calls cb on every change; returns an unsubscribe function. */
  onStatus(cb: (s: OutputLinkStatus) => void): () => void {
    this.listeners.add(cb);
    return () => this.listeners.delete(cb);
  }

  /** Queues the project; at most one post per animation frame. */
  sendProject(project: Project): void {
    this.pending = project;
    this.lastProject = project;
    if (this.raf) return;
    this.raf = requestAnimationFrame(() => {
      this.raf = 0;
      if (this.pending) this.post({ type: 'project', project: this.pending });
      this.pending = null;
    });
  }

  identify(): void {
    this.post({ type: 'identify' });
  }

  async openOutputWindow(): Promise<void> {
    if (this.win && !this.win.closed) {
      this.win.focus();
      return;
    }
    let features = 'popup,width=1280,height=720';
    if ('getScreenDetails' in window) {
      try {
        const details = await (window as unknown as { getScreenDetails(): Promise<{ screens: ScreenDetailed[] }> }).getScreenDetails();
        const s = details.screens.find((x) => !x.isPrimary);
        if (s) features = `popup,left=${s.availLeft},top=${s.availTop},width=${s.width},height=${s.height}`;
      } catch {
        // permission denied: fall back to the default popup
      }
    }
    this.win = window.open(this.outputUrl, 'output', features);
    this.win?.focus();
  }

  dispose(): void {
    clearInterval(this.watchdog);
    cancelAnimationFrame(this.raf);
    this.channel.close();
    this.listeners.clear();
  }

  private post(msg: EditorToOutput): void {
    this.channel.postMessage(msg);
  }

  private onMessage(msg: OutputToEditor): void {
    switch (msg.type) {
      case 'hello':
        if (this.lastProject) this.post({ type: 'project', project: this.lastProject });
        this.post({ type: 'ping' });
        break;
      case 'status':
        this.lastSeen = performance.now();
        this.set({ state: 'connected', info: msg });
        break;
      case 'bye':
        this.setDisconnected();
        break;
    }
  }

  private setDisconnected(): void {
    this.set({ state: 'disconnected', info: null });
  }

  private set(s: OutputLinkStatus): void {
    this.current = s;
    for (const cb of this.listeners) cb(s);
  }
}
