// Top bar: File and Panels menus, output window status, project name and saved dot,
// and undo/redo, pause, black-out and identify on the right.

import { redo, syncVideos, toggleBlackout, togglePause, undo } from './commands';
import type { Ctx } from './ctx';
import { h } from './dom';
import { addLayer } from './layerOps';
import { icon, type IconName } from './icons';
import { PANEL_LABELS, type PanelId } from './uiState';
import { startRename } from './widgets/inlineRename';
import { openMenu } from './widgets/overlays';

export function createTopbar(ctx: Ctx) {
  const { store, ui, link, projects } = ctx;

  const mkBtn = (name: IconName, title: string, onClick: (e: MouseEvent) => void, label?: string) => {
    const b = h('button', `tb-btn${label ? ' with-label' : ''}`);
    b.type = 'button';
    b.title = title;
    b.append(icon(name, 15));
    if (label) b.append(h('span', undefined, label));
    b.addEventListener('click', onClick);
    return b;
  };

  const fileBtn = mkBtn('folder', 'File: new, open, save, export and import projects', () =>
    openMenu(fileBtn, [
      { label: 'New Project', shortcut: '⌘N', run: () => void projects.newProject() },
      { label: 'Duplicate Project', shortcut: '⌘D', run: () => void projects.duplicateProject() },
      { label: 'Projects...', shortcut: '⌘O', run: () => projects.openProjectsDialog() },
      { separator: true },
      { label: 'Save Project', shortcut: '⌘S', run: () => void projects.save() },
      { label: 'Export Project', shortcut: '⌘E', run: () => void projects.exportProject() },
      { label: 'Import Project', shortcut: '⌘I', run: () => projects.importProject() },
      { separator: true },
      { label: 'Delete Project', danger: true, run: () => void projects.deleteProject() },
    ]),
  );
  const panelsBtn = mkBtn('grid', 'Panels: show or hide parts of the editor', () =>
    openMenu(
      panelsBtn,
      (Object.keys(PANEL_LABELS) as PanelId[]).map((id) => ({
        label: PANEL_LABELS[id].title,
        shortcut: PANEL_LABELS[id].key,
        dot: ui.state.panels[id],
        run: () => ui.togglePanel(id),
      })),
    ),
  );
  const monitorBtn = mkBtn('monitor', 'Output window: click to open or bring it to the front', () => void link.openOutputWindow());
  monitorBtn.classList.add('monitor');
  const maskBtn = mkBtn('mask', 'Add a mask layer. Select Invert mask in Properties to keep only the inside.', () => addLayer(ctx, 'mask'), 'Add mask');

  const dot = h('span', 'saved-dot');
  dot.title = 'Saved automatically';
  const nameEl = h('span', 'project-name');
  nameEl.title = 'Click to rename the project';
  nameEl.addEventListener('click', () => startRename(nameEl, store.state.name, (v) => projects.renameProject(v)));
  const centre = h('div', 'tb-centre', dot, nameEl);

  const undoBtn = mkBtn('undo', 'Undo (⌘Z)', () => undo(ctx));
  const redoBtn = mkBtn('redo', 'Redo (⇧⌘Z)', () => redo(ctx));
  const pauseBtn = mkBtn('pause', 'Pause or play every shader and video (Space)', () => togglePause(ctx), 'Pause');
  const syncBtn = mkBtn('play', 'Restart all video layers together from zero and keep them synchronized', () => syncVideos(ctx), 'Sync videos');
  const blackBtn = mkBtn('power', 'Black out: the projector shows pure black (B)', () => toggleBlackout(ctx), 'Black out');
  const identBtn = mkBtn('crosshair', 'Identify: flash each layer name on the projector so you can tell which shape is which', () => link.identify(), 'Identify');

  const el = h(
    'header',
    'topbar',
    h('div', 'tb-left', fileBtn, panelsBtn, h('span', 'tb-sep'), monitorBtn, maskBtn),
    centre,
    h('div', 'tb-right', undoBtn, redoBtn, h('span', 'tb-sep'), pauseBtn, syncBtn, blackBtn, identBtn),
  );

  let saveTimer = 0;
  const markDirty = () => {
    dot.classList.add('saving');
    dot.title = 'Saving...';
    clearTimeout(saveTimer);
    saveTimer = window.setTimeout(() => {
      dot.classList.remove('saving');
      dot.title = 'Saved automatically';
    }, 900);
  };
  store.subscribe(markDirty);

  function refresh(): void {
    const p = store.state;
    if (nameEl.isConnected && nameEl.textContent !== p.name) nameEl.textContent = p.name;
    undoBtn.disabled = !store.canUndo;
    redoBtn.disabled = !store.canRedo;
    syncBtn.disabled = !p.scenes.some((s) => s.layers.some((l) => l.kind === 'content' && l.content.type === 'surface' && l.content.fill.type === 'video'));
    const paused = p.playback.paused;
    pauseBtn.replaceChildren(icon(paused ? 'play' : 'pause', 15), h('span', undefined, paused ? 'Play' : 'Pause'));
    pauseBtn.classList.toggle('active', paused);
    blackBtn.classList.toggle('active-danger', p.output.blackout);
    const st = link.status;
    const info = st.info;
    const mismatch = st.state === 'connected' && info !== null && info.fullscreen && (info.width !== p.canvas.width || info.height !== p.canvas.height);
    monitorBtn.classList.toggle('connected', st.state === 'connected' && !mismatch);
    monitorBtn.classList.toggle('mismatch', mismatch);
    monitorBtn.title = st.state === 'disconnected'
      ? 'Output window: not open. Click to open it.'
      : mismatch
        ? `Output window is ${info!.width}x${info!.height} but the canvas is ${p.canvas.width}x${p.canvas.height}. Click to bring it to the front.`
        : info && !info.fullscreen ? 'Output preview connected. Move it to the projector and press F for full resolution.' : 'Output window: connected. Click to bring it to the front.';
  }
  return { el, refresh, monitorBtn };
}
