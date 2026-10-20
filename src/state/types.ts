// The serialisable project state. Everything the Output window needs to draw
// lives here; GPU objects are built from it and never stored in it.

import type { InputValue } from '../isf/parse';

export type BlendMode = 'normal' | 'add' | 'screen';

export interface LayerState {
  id: string;
  name: string;
  /** Layer type. Only 'shader' exists today; image, video, webcam, colour and text come later. */
  kind: 'shader';
  /** File name the shader was loaded from, for display. */
  fileName: string;
  /** Full ISF source, kept in the project so it is portable. */
  source: string;
  values: Record<string, InputValue>;
  visible: boolean;
  opacity: number;
  blend: BlendMode;
  /** Playback speed multiplier for this layer's clock. */
  speed: number;
  paused: boolean;
  /** Seconds added to this layer's clock. */
  timeOffset: number;
  /** Render resolution scale, 0.25 to 1. */
  quality: number;
}

export interface ProjectSettings {
  outputWidth: number;
  outputHeight: number;
  globalSpeed: number;
  globalPaused: boolean;
}

export interface ProjectState {
  layers: LayerState[];
  settings: ProjectSettings;
}
