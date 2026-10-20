// Layers panel: the active scene's layers, top of the stack first.

import { activeScene } from '../../state/defaults';
import type { Ctx } from '../ctx';
import { h } from '../dom';
import { icon, type IconName } from '../icons';
import { addLayer, deleteLayer, duplicateLayer, layerTypeKey, moveLayer, updateLayer, type NewLayerType } from '../layerOps';
import { PANEL_LABELS } from '../uiState';
import { startRename } from '../widgets/inlineRename';
import { openContextMenu } from '../widgets/overlays';
import { panelShell } from '../widgets/panel';
import { iconButton } from '../widgets/rows';

export const TYPE_ICON: Record<NewLayerType, IconName> = {
  surface: 'surface',
  shader: 'shader',
  text: 'text',
  webcam: 'webcam',
  mask: 'mask',
};

const ADD_BUTTONS: { type: NewLayerType; title: string }[] = [
  { type: 'surface', title: 'New surface: a flat shape you can fill with a colour, gradient, image or video' },
  { type: 'shader', title: 'New shader: an animated generative visual' },
  { type: 'webcam', title: 'New webcam: live camera picture' },
  { type: 'text', title: 'New text: words you can style' },
  { type: 'mask', title: 'New mask: hides or isolates part of the layers below it' },
];

export function createLayersPanel(ctx: Ctx) {
  const shell = panelShell(PANEL_LABELS.layers.title, PANEL_LABELS.layers.key, 'layers-panel');
  for (const b of ADD_BUTTONS) shell.actions.append(iconButton(TYPE_ICON[b.type], b.title, () => addLayer(ctx, b.type), 15));
  const list = h('div', 'list');
  shell.body.append(list);
  list.addEventListener('pointerdown', (e) => {
    if (e.target === list) ctx.ui.select(null);
  });
  let sig = '';
  let dragId: string | null = null;

  function refresh(): void {
    const scene = activeScene(ctx.store.state);
    const sel = ctx.ui.state.selectedId;
    const next = JSON.stringify([sel, scene.layers.map((l) => [l.id, l.name, l.visible, l.locked, layerTypeKey(l)])]);
    if (next === sig) return;
    sig = next;
    const rows = [...scene.layers].reverse().map((l) => {
      const name = h('span', 'row-name', l.name);
      const lock = iconButton(l.locked ? 'lock' : 'unlock', l.locked ? 'Locked: cannot be moved on the canvas. Click to unlock.' : 'Lock so it cannot be moved by accident', (e) => {
        e.stopPropagation();
        updateLayer(ctx, l.id, (x) => { x.locked = !x.locked; });
      });
      const eye = iconButton(l.visible ? 'eye' : 'eyeOff', l.visible ? 'Visible. Click to hide.' : 'Hidden. Click to show.', (e) => {
        e.stopPropagation();
        updateLayer(ctx, l.id, (x) => { x.visible = !x.visible; });
      });
      lock.classList.add('toggle', l.locked ? 'on' : 'off');
      eye.classList.add('toggle', l.visible ? 'off' : 'on');
      const row = h('div', `item${l.id === sel ? ' selected' : ''}${l.visible ? '' : ' dim'}`, icon(TYPE_ICON[layerTypeKey(l)], 14), name, h('span', 'row-toggles', lock, eye));
      row.draggable = true;
      row.dataset.id = l.id;
      row.title = 'Click to select. Double-click to rename. Drag to change stacking order.';
      row.addEventListener('click', () => ctx.ui.select(l.id));
      name.addEventListener('dblclick', (e) => {
        e.stopPropagation();
        startRename(name, l.name, (v) => updateLayer(ctx, l.id, (x) => { x.name = v; }));
      });
      row.addEventListener('contextmenu', (e) => {
        e.preventDefault();
        ctx.ui.select(l.id);
        openContextMenu(e.clientX, e.clientY, [
          { label: 'Rename', run: () => startRename(name, l.name, (v) => updateLayer(ctx, l.id, (x) => { x.name = v; })) },
          { label: 'Duplicate', shortcut: '⌘D', run: () => duplicateLayer(ctx, l.id) },
          { label: l.locked ? 'Unlock' : 'Lock', run: () => updateLayer(ctx, l.id, (x) => { x.locked = !x.locked; }) },
          { label: l.visible ? 'Hide' : 'Show', run: () => updateLayer(ctx, l.id, (x) => { x.visible = !x.visible; }) },
          { separator: true },
          { label: 'Delete', shortcut: '⌫', danger: true, run: () => deleteLayer(ctx, l.id) },
        ]);
      });
      row.addEventListener('dragstart', (e) => {
        dragId = l.id;
        e.dataTransfer?.setData('text/plain', l.id);
        if (e.dataTransfer) e.dataTransfer.effectAllowed = 'move';
      });
      row.addEventListener('dragend', () => {
        dragId = null;
        for (const r of list.children) r.classList.remove('drop-above', 'drop-below');
      });
      row.addEventListener('dragover', (e) => {
        if (!dragId || dragId === l.id) return;
        e.preventDefault();
        const r = row.getBoundingClientRect();
        const above = e.clientY < r.top + r.height / 2;
        row.classList.toggle('drop-above', above);
        row.classList.toggle('drop-below', !above);
      });
      row.addEventListener('dragleave', () => row.classList.remove('drop-above', 'drop-below'));
      row.addEventListener('drop', (e) => {
        e.preventDefault();
        if (!dragId || dragId === l.id) return;
        const r = row.getBoundingClientRect();
        const above = e.clientY < r.top + r.height / 2;
        const layers = activeScene(ctx.store.state).layers.filter((x) => x.id !== dragId);
        const idx = layers.findIndex((x) => x.id === l.id);
        moveLayer(ctx, dragId, above ? idx + 1 : idx);
      });
      return row;
    });
    list.replaceChildren(...rows);
    if (rows.length === 0) list.append(h('div', 'empty-hint', 'No layers yet. Use the buttons above to add a surface, shader, webcam, text or mask.'));
  }
  return { el: shell.el, refresh };
}
