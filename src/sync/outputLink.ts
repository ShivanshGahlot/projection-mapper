// Editor-side link to the output window: sends the project, opens/places the
// window on the projector and tracks its status.

import type { ClockSnapshot } from '../render/api';
import type { Project } from '../state/types';
import { CHANNEL_NAME, type EditorToOutput, type OutputToEditor } from './messages';

export type OutputStatus = Extract<OutputToEditor, { type: 'status' }>;

export interface OutputLinkStatus {
  state: 'disconnected' | 'connected';
  /** Last status report from the output window; null while disconnected. */
  info: OutputStatus | null;
}

const STALE_MS = 15000;

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
  private timer = 0;
  private win: Window | null = null;
  private current: OutputLinkStatus = { state: 'disconnected', info: null };
  private lastSeen = 0;
  private activeOutputId: string | null = null;
  private openedOutputId: string | null = null;
  /** Called when an output window says hello (so the editor can send clocks too). */
  onHello: (() => void) | null = null;
  private listeners = new Set<(s: OutputLinkStatus) => void>();
  private watchdog: number;
  private outputUrl = new URL('output.html', location.href).href;

  constructor() {
    this.channel.onmessage = (e: MessageEvent<OutputToEditor>) => this.onMessage(e.data);
    this.watchdog = window.setInterval(() => {
      if (this.win?.closed) { this.win = null; this.openedOutputId = null; this.setDisconnected(); }
      else if (this.current.state === 'connected' && performance.now() - this.lastSeen > STALE_MS) this.setDisconnected();
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

  /**
   * Queues the project; at most one post per animation frame. A timer backs the frame up:
   * a hidden or occluded editor window gets no animation frames, but the projector must
   * still see edits.
   */
  sendProject(project: Project): void {
    this.pending = project;
    this.lastProject = project;
    if (this.raf) return;
    const flush = () => {
      cancelAnimationFrame(this.raf);
      clearTimeout(this.timer);
      this.raf = 0;
      if (this.pending) this.post({ type: 'project', project: this.pending });
      this.pending = null;
    };
    this.raf = requestAnimationFrame(flush);
    this.timer = window.setTimeout(flush, 50);
  }

  sendClocks(snap: ClockSnapshot): void {
    this.post({ type: 'clocks', at: snap.at, layers: snap.layers });
  }

  identify(): void {
    this.post({ type: 'identify' });
  }

  async openOutputWindow(): Promise<void> {
    if (this.win && !this.win.closed) {
      this.activeOutputId = this.openedOutputId;
      this.post({ type: 'ping' });
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
    this.openedOutputId = crypto.randomUUID();
    this.activeOutputId = this.openedOutputId;
    const url = new URL(this.outputUrl); url.searchParams.set('outputId', this.openedOutputId);
    this.win = window.open(url.href, 'output', features);
    if (!this.win) { this.openedOutputId = null; this.activeOutputId = null; }
    this.win?.focus();
  }

  dispose(): void {
    clearInterval(this.watchdog);
    cancelAnimationFrame(this.raf);
    clearTimeout(this.timer);
    this.channel.close();
    this.listeners.clear();
  }

  private post(msg: EditorToOutput): void {
    this.channel.postMessage(msg);
  }

  private onMessage(msg: OutputToEditor): void {
    // Old/secondary output windows must not alternate our dimensions or disconnect
    // the active projector. Prefer a fullscreen window when discovering one on reload.
    if (this.activeOutputId && msg.outputId !== this.activeOutputId) {
      if (!this.openedOutputId && msg.type === 'status' && msg.outputId && msg.fullscreen && !this.current.info?.fullscreen) {
        this.activeOutputId = msg.outputId;
      } else return;
    }
    if (!this.activeOutputId && msg.outputId) this.activeOutputId = msg.outputId;
    switch (msg.type) {
      case 'hello':
        if (this.lastProject) this.post({ type: 'project', project: this.lastProject });
        this.post({ type: 'ping' });
        this.onHello?.();
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
    this.activeOutputId = null;
    this.set({ state: 'disconnected', info: null });
  }

  private set(s: OutputLinkStatus): void {
    this.current = s;
    for (const cb of this.listeners) cb(s);
  }
}
