// Messages between the editor window and the output (projector) window over
// BroadcastChannel. The editor owns the project; the output window only draws it.

import type { LayerClock } from '../render/api';
import type { Project } from '../state/types';

export const CHANNEL_NAME = 'projection-mapper-sync';

export type EditorToOutput =
  /** Full project. Sent on every change, at most once per animation frame. */
  | { type: 'project'; project: Project }
  /**
   * The editor's per-layer clocks (shader time, video position) at machine-wide time
   * `at` (performance.timeOrigin + frame time, ms). The output window follows them.
   * Sent about 4x/s, right after any speed/pause/offset change, and on 'hello'.
   */
  | { type: 'clocks'; at: number; layers: Record<string, LayerClock> }
  /** Flash every layer's name on its shape for a few seconds. */
  | { type: 'identify' }
  /** Ask the output window to report its status now. */
  | { type: 'ping' };

export type OutputToEditor =
  /** Sent when the output window opens; the editor replies with 'project'. */
  | { type: 'hello'; outputId?: string }
  | {
      type: 'status';
      outputId?: string;
      /** Drawable size of the output window in device pixels. */
      width: number;
      height: number;
      fullscreen: boolean;
      fps: number;
      /** layerId -> error message, for layers that fail in the output window. */
      errors: Record<string, string>;
    }
  | { type: 'bye'; outputId?: string };
