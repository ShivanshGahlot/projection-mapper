// The interface every layer type implements. The renderer only ever asks a
// layer for "a texture of this size for this frame", so new layer types
// (image, video, webcam, solid colour, text) plug in without renderer changes.

import type { LayerState } from '../state/types';

export interface AudioValues {
  bass: number;
  mid: number;
  high: number;
  beat: number;
  level: number;
}

export interface FrameContext {
  /** Real seconds since the last frame (clamped), before any speed scaling. */
  dt: number;
  frameIndex: number;
  date: readonly [number, number, number, number];
  audio: AudioValues;
}

export type LayerStatus =
  | { state: 'loading' }
  | { state: 'ok' }
  | { state: 'error'; message: string };

export interface LayerRuntime {
  /** Applies new settings. Called whenever the layer's state changes. */
  update(state: LayerState): void;
  /** Advances the layer's clock. Called every frame, even for hidden layers, so they stay in time. */
  tick(dt: number): void;
  /**
   * Draws the current frame into a texture of the given size and returns it,
   * or null if there is nothing to draw (still loading, or broken).
   */
  render(ctx: FrameContext, width: number, height: number): WebGLTexture | null;
  readonly status: LayerStatus;
  dispose(): void;
}
