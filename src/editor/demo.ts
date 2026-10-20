// First-run project: six small shader shapes, like a starter scene.

import { library } from '../library/index';
import { createContentLayer, createProject, shaderContent } from '../state/defaults';
import type { Quad } from '../state/types';

/** Shape layout as fractions of the canvas: x, y, w, h and an optional corner skew in px/1000. */
const LAYOUT: { x: number; y: number; w: number; h: number; skew?: number }[] = [
  { x: 0.23, y: 0.27, w: 0.2, h: 0.28 },
  { x: 0.5, y: 0.25, w: 0.12, h: 0.2, skew: 18 },
  { x: 0.38, y: 0.62, w: 0.1, h: 0.14 },
  { x: 0.54, y: 0.62, w: 0.045, h: 0.07 },
  { x: 0.6, y: 0.64, w: 0.15, h: 0.15, skew: -14 },
  { x: 0.74, y: 0.44, w: 0.13, h: 0.26 },
];

export function createDemoProject() {
  const project = createProject();
  const { width: W, height: H } = project.canvas;
  const shaders = library.shaders;
  const scene = project.scenes[0];
  LAYOUT.forEach((r, i) => {
    const s = shaders[Math.floor((i * shaders.length) / LAYOUT.length) % shaders.length];
    const layer = createContentLayer(`Shader ${i + 1}`, shaderContent(s.source, s.fileName, s.id), project.canvas);
    const x = r.x * W;
    const y = r.y * H;
    const w = r.w * W;
    const hh = r.h * H;
    const k = ((r.skew ?? 0) / 1000) * W;
    layer.corners = [[x + k, y], [x + w, y - k / 2], [x + w - k, y + hh], [x, y + hh + k / 2]] as Quad;
    scene.layers.push(layer);
  });
  return project;
}
