// Assembles the editor: layout, panels, the per-frame loop and store wiring.

import { APP_NAME } from '../config';
import { createRenderer } from '../render/renderer';
import { assetStorage } from '../persist/db';
import { startAutosave } from '../persist/autosave';
import { OutputLink } from '../sync/outputLink';
import { activeScene } from '../state/defaults';
import type { Project } from '../state/types';
import { Store } from '../state/store';
import type { Ctx } from './ctx';
import { h } from './dom';
import { installKeyboard } from './keyboard';
import { createInputPanel } from './panels/input';
import { createLayersPanel } from './panels/layers';
import { createOutputPanel } from './panels/output';
import { createPropertiesPanel } from './panels/properties';
import { createScenesPanel } from './panels/scenes';
import { createProjectActions } from './projects';
import { createTopbar } from './topbar';
import { UiStore } from './uiState';
import { hooks } from './widgets/numberField';

export interface Editor {
  ctx: Ctx;
}

export function createEditor(root: HTMLElement, project: Project): Editor {
  const store = new Store<Project>(project);
  const ui = new UiStore();
  const link = new OutputLink();
  const canvas = document.createElement('canvas');
  const renderer = createRenderer(canvas, assetStorage);
  hooks.endMerge = () => store.endMerge();

  const autosave = startAutosave(() => store.state, (cb) => store.subscribe(cb));
  const base = { store, ui, renderer, link };
  const projects = createProjectActions(base, () => autosave);
  const ctx: Ctx = { ...base, projects };

  const topbar = createTopbar(ctx);
  const scenes = createScenesPanel(ctx);
  const layers = createLayersPanel(ctx);
  const input = createInputPanel(ctx);
  const output = createOutputPanel(ctx);
  const props = createPropertiesPanel(ctx);

  const left = h('div', 'col col-left', scenes.el, layers.el);
  const centre = h('div', 'col col-centre', input.el, output.el);
  const right = h('div', 'col col-right', props.el);
  root.replaceChildren(topbar.el, h('main', 'workspace', left, centre, right));

  installKeyboard(ctx, output.nudge);

  // Keep the renderer, output window and panels in step with the project.
  let needsRefresh = true;
  // The editor is the clock master: the output window follows the clocks sent from here.
  let clocksDirty = true;
  let clocksSig = '';
  let clocksSentAt = -Infinity;
  const ratesSig = (p: Project): string => {
    let sig = `${p.playback.speed}|${p.playback.paused}`;
    for (const l of activeScene(p).layers) {
      if (l.kind !== 'content') continue;
      const c = l.content;
      sig += c.type === 'shader' ? `|${l.id}:${c.speed}:${c.paused}:${c.timeOffset}` : c.type === 'surface' && c.fill.type === 'video' ? `|${l.id}:${c.fill.playbackRate}:${c.fill.loop}` : '';
    }
    return sig;
  };
  const onProject = () => {
    renderer.setProject(store.state);
    link.sendProject(store.state);
    const sig = ratesSig(store.state);
    if (sig !== clocksSig) {
      clocksSig = sig;
      clocksDirty = true;
    }
    needsRefresh = true;
  };
  link.onHello = () => { clocksDirty = true; };
  store.subscribe(onProject);
  ui.subscribe(() => { needsRefresh = true; });
  link.onStatus(() => {
    needsRefresh = true;
    props.syncErrors();
  });
  renderer.onStatusChange = () => { needsRefresh = true; props.syncErrors(); };
  onProject();

  function refresh(): void {
    topbar.refresh();
    scenes.refresh();
    layers.refresh();
    props.refresh();
    const p = ui.state.panels;
    scenes.el.hidden = !p.scenes;
    layers.el.hidden = !p.layers;
    left.hidden = !p.scenes && !p.layers;
    input.el.hidden = !p.input;
    output.el.hidden = !p.output;
    centre.hidden = !p.input && !p.output;
    right.hidden = !p.properties;
    const title = `${store.state.name} - ${APP_NAME}`;
    if (document.title !== title) document.title = title;
  }

  function frame(now: number): void {
    if (needsRefresh) {
      needsRefresh = false;
      refresh();
    }
    renderer.tick(now);
    if (clocksDirty || now - clocksSentAt >= 250) {
      clocksDirty = false;
      clocksSentAt = now;
      link.sendClocks(renderer.getClocks());
    }
    output.render(now);
    input.render();
    requestAnimationFrame(frame);
  }
  requestAnimationFrame(frame);

  window.addEventListener('beforeunload', () => void autosave.flush());
  document.addEventListener('visibilitychange', () => {
    if (document.visibilityState === 'hidden') void autosave.flush();
  });

  return { ctx };
}
