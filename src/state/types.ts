// The serialisable project model. Everything both windows need to draw lives
// here; GPU objects are built from it and never stored in it. All positions are
// in output-canvas pixels (origin top-left, y down).

import type { InputValue } from '../isf/parse';

export type Vec2 = [x: number, y: number];
/** Corners in order: top-left, top-right, bottom-right, bottom-left. */
export type Quad = [Vec2, Vec2, Vec2, Vec2];
export type BlendMode = 'normal' | 'add' | 'screen' | 'multiply';

/** Normalised (0-1) rectangle of the content that is mapped onto the shape. y down. */
export interface CropRect {
  x: number;
  y: number;
  w: number;
  h: number;
}

export interface GradientStop {
  /** 0-1 */
  pos: number;
  /** #rrggbb */
  color: string;
}

export type Fill =
  | { type: 'none' }
  /** Alignment grid: outline, square cells, centre cross. What new surfaces start with. */
  | { type: 'grid' }
  | { type: 'color'; color: string }
  | { type: 'gradient'; kind: 'linear' | 'radial'; angle: number; stops: GradientStop[] }
  | { type: 'image'; assetId: string; fileName: string }
  | { type: 'video'; assetId: string; fileName: string; loop: boolean; playbackRate: number };

export interface SurfaceContent {
  type: 'surface';
  fill: Fill;
}

export interface ShaderContent {
  type: 'shader';
  /** Set when the shader comes from the built-in library; null for user files. */
  builtinId: string | null;
  fileName: string;
  /** Full ISF source, kept in the project so it is portable. */
  source: string;
  values: Record<string, InputValue>;
  /** Playback speed of this layer's own clock. */
  speed: number;
  paused: boolean;
  /** Seconds added to this layer's clock. */
  timeOffset: number;
}

export interface TextContent {
  type: 'text';
  text: string;
  fontFamily: string;
  italic: boolean;
  /** CSS font weight, 100-900. */
  weight: number;
  /** px at content resolution */
  fontSize: number;
  lineHeight: number;
  /** px */
  letterSpacing: number;
  uppercase: boolean;
  align: 'left' | 'center' | 'right' | 'justify';
  color: string;
  /** Background colour or null for transparent. */
  background: string | null;
}

export interface WebcamContent {
  type: 'webcam';
  /** null = default camera */
  deviceId: string | null;
  mirror: boolean;
}

export type Content = SurfaceContent | ShaderContent | TextContent | WebcamContent;
export type ContentType = Content['type'];

interface LayerBase {
  id: string;
  name: string;
  visible: boolean;
  /** Locked layers can't be selected or dragged on the canvas. */
  locked: boolean;
  /** 0-1 */
  opacity: number;
}

/** A shape that shows content, warped onto the output with a corner-pin. */
export interface ContentLayer extends LayerBase {
  kind: 'content';
  corners: Quad;
  shape: 'rect' | 'ellipse';
  /** Rounded corners in output px (rect shape only). */
  cornerRadius: number;
  /** Soft edge width in output px. */
  edgeFeather: number;
  blend: BlendMode;
  /** Part of the content mapped onto the shape. */
  crop: CropRect;
  /** Render resolution scale for generated content, 0.25-1. */
  quality: number;
  content: Content;
}

/** A polygon that cuts layers to black (or keeps only its inside). Never warped. */
export interface MaskLayer extends LayerBase {
  kind: 'mask';
  points: Vec2[];
  /** Soft edge width in output px. */
  feather: number;
  /** false: hide what is inside the polygon. true: show only what is inside. */
  invert: boolean;
  /** 'below' = every layer under this mask in the stack; otherwise one layer id. */
  target: 'below' | string;
  /** Colour the mask is drawn with in the editor (never on the projector). */
  color: string;
}

export type Layer = ContentLayer | MaskLayer;

export interface Scene {
  id: string;
  name: string;
  /** Draw order: index 0 is the bottom. The Layers panel shows them reversed. */
  layers: Layer[];
}

export interface Project {
  version: 1;
  id: string;
  name: string;
  /** Output canvas size in px; should match the projector. */
  canvas: { width: number; height: number };
  output: {
    /** Master brightness 0-1. */
    brightness: number;
    /** Background colour behind all layers. */
    background: string;
    /** Black-out: projector shows pure black. */
    blackout: boolean;
    /** Draws a test grid over the whole output, for squaring up the projector. */
    testPattern: boolean;
  };
  /** Global playback for every shader/video clock. */
  playback: { speed: number; paused: boolean };
  scenes: Scene[];
  activeSceneId: string;
}
