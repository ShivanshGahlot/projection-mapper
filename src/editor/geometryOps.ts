// Moving, aligning and rotating the points of a layer (corners or mask points).

import { bounds, centroid, rotate } from '../geometry/homography';
import type { ContentLayer, Layer, Project, Quad, Vec2 } from '../state/types';

export function layerPoints(l: Layer): Vec2[] {
  return l.kind === 'content' ? l.corners : l.points;
}

export function setLayerPoints(l: Layer, pts: Vec2[]): void {
  if (l.kind === 'content') l.corners = pts.map((p) => [p[0], p[1]] as Vec2) as Quad;
  else l.points = pts.map((p) => [p[0], p[1]] as Vec2);
}

export function translateLayer(l: Layer, dx: number, dy: number): void {
  setLayerPoints(l, layerPoints(l).map(([x, y]) => [x + dx, y + dy] as Vec2));
}

export type AlignEdge = 'left' | 'centre' | 'right' | 'top' | 'middle' | 'bottom';

export function alignLayer(l: ContentLayer, canvas: Project['canvas'], edge: AlignEdge): void {
  const b = bounds(l.corners);
  switch (edge) {
    case 'left': return translateLayer(l, -b.x, 0);
    case 'centre': return translateLayer(l, canvas.width / 2 - (b.x + b.w / 2), 0);
    case 'right': return translateLayer(l, canvas.width - (b.x + b.w), 0);
    case 'top': return translateLayer(l, 0, -b.y);
    case 'middle': return translateLayer(l, 0, canvas.height / 2 - (b.y + b.h / 2));
    case 'bottom': return translateLayer(l, 0, canvas.height - (b.y + b.h));
  }
}

export function rotateLayer(l: ContentLayer, degrees: number): void {
  setLayerPoints(l, rotate(l.corners, centroid(l.corners), degrees));
}

/** A centred upright rectangle with the same overall size as the current shape. */
export function resetShape(l: ContentLayer, canvas: Project['canvas']): void {
  const b = bounds(l.corners);
  const w = Math.max(16, b.w);
  const hh = Math.max(16, b.h);
  const x = (canvas.width - w) / 2;
  const y = (canvas.height - hh) / 2;
  l.corners = [[x, y], [x + w, y], [x + w, y + hh], [x, y + hh]];
}
