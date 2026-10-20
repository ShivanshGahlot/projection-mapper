// Global keyboard shortcuts.

import { redo, toggleBlackout, togglePause, undo } from './commands';
import type { Ctx } from './ctx';
import { isTyping } from './dom';
import { layerPoints } from './geometryOps';
import { deleteLayer, duplicateLayer, selectedLayer } from './layerOps';
import type { PanelId } from './uiState';
import { keys } from './viewport';
import { hooks } from './widgets/numberField';

const PANEL_KEYS: Record<string, PanelId> = { s: 'scenes', l: 'layers', i: 'input', o: 'output', p: 'properties' };

export function installKeyboard(ctx: Ctx, nudge: (dx: number, dy: number) => void): void {
  const { ui, projects } = ctx;
  let spaceHeld = false;

  window.addEventListener('keydown', (e) => {
    const typing = isTyping(e.target);
    const mod = e.metaKey || e.ctrlKey;
    const k = e.key.toLowerCase();

    if (mod && !e.altKey) {
      switch (k) {
        case 'z':
          if (typing) return;
          e.preventDefault();
          if (e.shiftKey) redo(ctx);
          else undo(ctx);
          return;
        case 'y':
          if (typing) return;
          e.preventDefault();
          redo(ctx);
          return;
        case 's':
          e.preventDefault();
          void projects.save();
          return;
        case 'n':
          e.preventDefault();
          void projects.newProject();
          return;
        case 'o':
          e.preventDefault();
          projects.openProjectsDialog();
          return;
        case 'e':
          e.preventDefault();
          void projects.exportProject();
          return;
        case 'i':
          e.preventDefault();
          projects.importProject();
          return;
        case 'd': {
          if (typing) return;
          e.preventDefault();
          const l = selectedLayer(ctx);
          if (l) duplicateLayer(ctx, l.id);
          else void projects.duplicateProject();
          return;
        }
      }
      return;
    }
    if (typing || e.altKey || mod) return;

    if (e.key === ' ') {
      e.preventDefault();
      if (!e.repeat) {
        spaceHeld = true;
        keys.space = true;
        keys.spacePanned = false;
        document.body.classList.add('space-down');
      }
      return;
    }
    const sel = selectedLayer(ctx);
    switch (e.key) {
      case 'Delete':
      case 'Backspace':
        if (sel) {
          e.preventDefault();
          deleteLayer(ctx, sel.id);
        }
        return;
      case 'ArrowLeft':
      case 'ArrowRight':
      case 'ArrowUp':
      case 'ArrowDown': {
        if (!sel) return;
        e.preventDefault();
        const d = e.shiftKey ? 10 : 1;
        nudge(e.key === 'ArrowLeft' ? -d : e.key === 'ArrowRight' ? d : 0, e.key === 'ArrowUp' ? -d : e.key === 'ArrowDown' ? d : 0);
        return;
      }
      case 'Tab': {
        if (!sel) return;
        e.preventDefault();
        const n = layerPoints(sel).length;
        const cur = ui.state.selectedCorner;
        const next = e.shiftKey ? (cur === null ? n - 1 : (cur - 1 + n) % n) : cur === null ? 0 : (cur + 1) % n;
        ui.set({ selectedCorner: next });
        return;
      }
      case 'Escape':
        if (ui.state.selectedCorner !== null) ui.set({ selectedCorner: null });
        else ui.select(null);
        return;
    }
    if (PANEL_KEYS[k]) {
      ui.togglePanel(PANEL_KEYS[k]);
      return;
    }
    if (k === 'b') toggleBlackout(ctx);
  });

  window.addEventListener('keyup', (e) => {
    if (e.key === ' ' && spaceHeld) {
      spaceHeld = false;
      keys.space = false;
      document.body.classList.remove('space-down');
      if (!keys.spacePanned) togglePause(ctx);
    }
    if (e.key.startsWith('Arrow')) hooks.endMerge();
  });

  window.addEventListener('blur', () => {
    spaceHeld = false;
    keys.space = false;
    document.body.classList.remove('space-down');
  });
}
