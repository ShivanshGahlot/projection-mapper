// Scenes panel: list of scenes; click to switch, double-click to rename.

import type { Ctx } from '../ctx';
import { h } from '../dom';
import { icon } from '../icons';
import { addScene, deleteScene, duplicateScene, setActiveScene } from '../layerOps';
import { PANEL_LABELS } from '../uiState';
import { iconButton } from '../widgets/rows';
import { startRename } from '../widgets/inlineRename';
import { openContextMenu } from '../widgets/overlays';
import { panelShell } from '../widgets/panel';

export function createScenesPanel(ctx: Ctx) {
  const shell = panelShell(PANEL_LABELS.scenes.title, PANEL_LABELS.scenes.key, 'scenes-panel');
  shell.badge.hidden = true;
  shell.actions.append(iconButton('plus', 'New scene', () => addScene(ctx), 14));
  const list = h('div', 'list');
  shell.body.append(list);
  let sig = '';

  const menuFor = (id: string, x: number, y: number, nameEl: HTMLElement, name: string) =>
    openContextMenu(x, y, [
      { label: 'Rename', run: () => startRename(nameEl, name, (v) => ctx.store.update((p) => { p.scenes.find((s) => s.id === id)!.name = v; })) },
      { label: 'Duplicate', run: () => duplicateScene(ctx, id) },
      { separator: true },
      { label: 'Delete', danger: true, disabled: ctx.store.state.scenes.length < 2, run: () => deleteScene(ctx, id) },
    ]);

  function refresh(): void {
    const p = ctx.store.state;
    const next = JSON.stringify([p.activeSceneId, p.scenes.map((s) => [s.id, s.name])]);
    if (next === sig) return;
    sig = next;
    shell.setCount(p.scenes.length);
    list.replaceChildren(
      ...p.scenes.map((s) => {
        const name = h('span', 'row-name', s.name);
        const more = iconButton('more', 'Scene options', (e) => {
          e.stopPropagation();
          const r = (e.currentTarget as HTMLElement).getBoundingClientRect();
          menuFor(s.id, r.left, r.bottom + 2, name, s.name);
        });
        more.classList.add('hover-only');
        const row = h('div', `item${s.id === p.activeSceneId ? ' selected' : ''}`, icon('scene', 14), name, more);
        row.title = 'Click to switch to this scene. Double-click to rename it.';
        row.addEventListener('click', () => setActiveScene(ctx, s.id));
        name.addEventListener('dblclick', (e) => {
          e.stopPropagation();
          startRename(name, s.name, (v) => ctx.store.update((pr) => { pr.scenes.find((x) => x.id === s.id)!.name = v; }));
        });
        row.addEventListener('contextmenu', (e) => {
          e.preventDefault();
          menuFor(s.id, e.clientX, e.clientY, name, s.name);
        });
        return row;
      }),
    );
  }
  return { el: shell.el, refresh };
}
