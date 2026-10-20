// Boots the editor: opens the last project (or a starter one) and applies debug query params.

import './editor/styles.css';
import { createEditor } from './editor/editor';
import { createDemoProject } from './editor/demo';
import { addLayer, type NewLayerType } from './editor/layerOps';
import { activeScene } from './state/defaults';
import { projectStorage } from './persist/db';
import type { Project } from './state/types';

async function pickProject(params: URLSearchParams): Promise<Project> {
  if (!params.has('fresh')) {
    const id = projectStorage.lastOpenedId();
    if (id) {
      try {
        const p = await projectStorage.load(id);
        if (p) return p;
      } catch (e) {
        console.error('Could not open the last project', e);
      }
    }
  }
  return createDemoProject();
}

async function boot(): Promise<void> {
  const params = new URLSearchParams(location.search);
  const root = document.getElementById('app');
  if (!root) throw new Error('Missing #app');
  const project = await pickProject(params);
  const editor = createEditor(root, project);
  const { ctx } = editor;

  // Debug / screenshot hooks: ?select=<index in draw order>, ?xray=1, ?stats=1, ?settings=1
  // ?add=surface,text,mask,webcam appends layers of those types first.
  for (const t of (params.get('add') ?? '').split(',').filter(Boolean)) addLayer(ctx, t as NewLayerType);
  const sel = params.get('select');
  if (sel !== null) {
    const layer = activeScene(ctx.store.state).layers[Number(sel)];
    if (layer) ctx.ui.select(layer.id);
  }
  if (params.has('xray')) ctx.ui.set({ xray: true });
  if (params.has('stats')) ctx.ui.set({ showStats: true });
  if (params.has('settings')) document.querySelector<HTMLButtonElement>('.output-panel button[title^="Output settings"]')?.click();
  (window as unknown as { __editor: unknown }).__editor = editor;
}

void boot();
