// Contract between the renderer and its users (the editor and the output window).
// The renderer implements `createRenderer`; nothing else touches WebGL.

import type { Project } from '../state/types';

export type LayerStatus =
  | { state: 'loading' }
  | { state: 'ok' }
  | { state: 'error'; message: string };

export interface RenderStats {
  /** Smoothed frames per second of tick() calls. */
  fps: number;
  /** Per-layer GPU time estimate in ms, when the timer extension is available. */
  layerMs: Record<string, number>;
}

/** Resolves stored image/video files to URLs. Implemented by persist/assets.ts. */
export interface AssetResolver {
  getAssetUrl(assetId: string): Promise<string | null>;
}

export interface Renderer {
  /** The WebGL canvas. The output window shows it directly; the editor copies from it. */
  readonly canvas: HTMLCanvasElement;
  /** Reconciles GPU resources with the project. Cheap to call on every change. */
  setProject(project: Project): void;
  /** Advances every clock (shaders, video playback state). Call once per animation frame. */
  tick(nowMs: number): void;
  /**
   * Draws the active scene's composite at width x height (output canvas pixels
   * scaled to fit) into the bottom-left of the canvas, growing the canvas if
   * needed. Masks, blackout, brightness and test pattern are applied.
   */
  renderOutput(width: number, height: number): void;
  /**
   * Draws one layer's raw, unwarped content (no crop) at width x height into the
   * bottom-left of the canvas. Used by the editor's Input panel.
   */
  renderContent(layerId: string, width: number, height: number): void;
  /**
   * Copies the region last drawn by renderOutput/renderContent to a 2D canvas
   * context, filling it. Must be called in the same task as the render call.
   */
  copyTo(ctx: CanvasRenderingContext2D): void;
  /** Native content size (video/image/webcam pixels); null for generated content. */
  contentSize(layerId: string): { width: number; height: number } | null;
  status(layerId: string): LayerStatus;
  /** Called when any layer's status changes. */
  onStatusChange: ((layerId: string, status: LayerStatus) => void) | null;
  readonly stats: RenderStats;
  dispose(): void;
}

export type CreateRenderer = (canvas: HTMLCanvasElement, assets: AssetResolver) => Renderer;
