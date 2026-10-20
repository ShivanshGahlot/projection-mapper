// Project edits shared by the panels, canvas and keyboard: finding, adding,
// duplicating, deleting and reordering layers and scenes.

import { activeScene, createContentLayer, createMaskLayer, createScene, defaultContent, newId, shaderContent } from '../state/defaults';
import type { Store } from '../state/store';
import type { ContentLayer, ContentType, Layer, Project, Scene, Vec2 } from '../state/types';
import type { UiStore } from './uiState';

export interface Core {
  store: Store<Project>;
  ui: UiStore;
}

export function findLayer(project: Project, id: string | null): Layer | null {
  if (!id) return null;
  for (const s of project.scenes) {
    const l = s.layers.find((x) => x.id === id);
    if (l) return l;
  }
  return null;
}

export function selectedLayer(core: Core): Layer | null {
  const l = findLayer(core.store.state, core.ui.state.selectedId);
  // A layer from another scene is not selectable.
  return l && activeScene(core.store.state).layers.includes(l) ? l : null;
}

export type NewLayerType = ContentType | 'mask';

const LABELS: Record<NewLayerType, string> = { surface: 'Surface', shader: 'Shader', text: 'Text', webcam: 'Webcam', mask: 'Mask' };

export function layerTypeKey(l: Layer): NewLayerType {
  return l.kind === 'mask' ? 'mask' : l.content.type;
}

export function newContentFor(type: ContentType) {
  // A shader layer starts with no shader chosen: it shows the alignment grid
  // until one is picked in Properties.
  if (type === 'shader') return shaderContent('', '', null);
  return defaultContent(type);
}

/** Adds a layer on top of the stack of the active scene and selects it. */
export function addLayer(core: Core, type: NewLayerType): void {
  const { store, ui } = core;
  const scene = activeScene(store.state);
  const count = scene.layers.filter((l) => layerTypeKey(l) === type).length + 1;
  const name = `${LABELS[type]} ${count}`;
  const n = scene.layers.length;
  const offset: Vec2 = [((n % 5) - 2) * 40, ((n % 4) - 1.5) * 30];
  const layer = type === 'mask' ? createMaskLayer(name, store.state.canvas, offset) : createContentLayer(name, newContentFor(type), store.state.canvas, offset);
  store.update((p) => activeScene(p).layers.push(layer));
  ui.select(layer.id);
}

export function duplicateLayer(core: Core, id: string): void {
  const { store, ui } = core;
  const copy = structuredClone(findLayer(store.state, id));
  if (!copy) return;
  copy.id = newId();
  copy.name = `${copy.name} copy`;
  if (copy.kind === 'content') copy.corners = copy.corners.map(([x, y]) => [x + 24, y + 24]) as ContentLayer['corners'];
  else copy.points = copy.points.map(([x, y]) => [x + 24, y + 24] as Vec2);
  store.update((p) => {
    const layers = activeScene(p).layers;
    layers.splice(layers.findIndex((l) => l.id === id) + 1, 0, copy);
  });
  ui.select(copy.id);
}

export function deleteLayer(core: Core, id: string): void {
  const { store, ui } = core;
  store.update((p) => {
    for (const s of p.scenes) {
      s.layers = s.layers.filter((l) => l.id !== id);
      for (const l of s.layers) if (l.kind === 'mask' && l.target === id) l.target = 'below';
    }
  });
  if (ui.state.selectedId === id) ui.select(null);
}

/** Moves a layer to a new position in draw order (index 0 = bottom). */
export function moveLayer(core: Core, id: string, toIndex: number): void {
  core.store.update((p) => {
    const layers = activeScene(p).layers;
    const from = layers.findIndex((l) => l.id === id);
    if (from < 0) return;
    const [l] = layers.splice(from, 1);
    layers.splice(Math.max(0, Math.min(layers.length, toIndex)), 0, l);
  });
}

export function updateLayer<T extends Layer = Layer>(core: Core, id: string, fn: (layer: T) => void, merge?: string): void {
  core.store.update(
    (p) => {
      const l = findLayer(p, id);
      if (l) fn(l as T);
    },
    merge ? { history: 'merge', key: `${merge}:${id}` } : {},
  );
}

export function addScene(core: Core): void {
  const { store, ui } = core;
  const scene = createScene(`Scene ${store.state.scenes.length + 1}`);
  store.update((p) => {
    p.scenes.push(scene);
    p.activeSceneId = scene.id;
  });
  ui.select(null);
}

export function duplicateScene(core: Core, id: string): void {
  const src = core.store.state.scenes.find((s) => s.id === id);
  if (!src) return;
  const copy: Scene = structuredClone(src);
  copy.id = newId();
  copy.name = `${src.name} copy`;
  for (const l of copy.layers) l.id = newId();
  core.store.update((p) => {
    p.scenes.splice(p.scenes.findIndex((s) => s.id === id) + 1, 0, copy);
    p.activeSceneId = copy.id;
  });
  core.ui.select(null);
}

export function deleteScene(core: Core, id: string): void {
  if (core.store.state.scenes.length < 2) return;
  core.store.update((p) => {
    p.scenes = p.scenes.filter((s) => s.id !== id);
    if (p.activeSceneId === id) p.activeSceneId = p.scenes[0].id;
  });
  core.ui.select(null);
}

export function setActiveScene(core: Core, id: string): void {
  if (core.store.state.activeSceneId === id) return;
  core.store.update((p) => {
    p.activeSceneId = id;
  }, { history: 'none' });
  core.ui.select(null);
}
