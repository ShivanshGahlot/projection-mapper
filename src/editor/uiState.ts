// Editor-only state that never goes into the project file: selection, which
// panels are open, X-ray. Panel visibility is remembered in localStorage.

export type PanelId = 'scenes' | 'layers' | 'input' | 'output' | 'properties';

export const PANEL_LABELS: Record<PanelId, { title: string; key: string }> = {
  scenes: { title: 'Scenes', key: 'S' },
  layers: { title: 'Layers', key: 'L' },
  input: { title: 'Input', key: 'I' },
  output: { title: 'Output', key: 'O' },
  properties: { title: 'Properties', key: 'P' },
};

export interface UiState {
  selectedId: string | null;
  /** Index of the selected corner (content layer) or point (mask); null = whole shape. */
  selectedCorner: number | null;
  /** Layer under the pointer in the Output panel. */
  hoverId: string | null;
  panels: Record<PanelId, boolean>;
  /** Show outlines of every layer, including hidden ones. */
  xray: boolean;
  showStats: boolean;
}

const STORAGE_KEY = 'projection-mapper.panels';

function loadPanels(): Record<PanelId, boolean> {
  const panels: Record<PanelId, boolean> = { scenes: true, layers: true, input: true, output: true, properties: true };
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (raw) {
      const saved = JSON.parse(raw) as Partial<Record<PanelId, boolean>>;
      for (const k of Object.keys(panels) as PanelId[]) if (typeof saved[k] === 'boolean') panels[k] = saved[k]!;
    }
  } catch {
    // storage unavailable: defaults
  }
  return panels;
}

export class UiStore {
  state: UiState = {
    selectedId: null,
    selectedCorner: null,
    hoverId: null,
    panels: loadPanels(),
    xray: false,
    showStats: false,
  };
  private listeners = new Set<() => void>();

  subscribe(cb: () => void): () => void {
    this.listeners.add(cb);
    return () => this.listeners.delete(cb);
  }

  /** Notifies listeners (also used when only a view changed, e.g. zoom). */
  invalidate(): void {
    for (const l of this.listeners) l();
  }

  set(patch: Partial<UiState>): void {
    Object.assign(this.state, patch);
    this.invalidate();
  }

  select(id: string | null): void {
    if (this.state.selectedId === id) return;
    this.set({ selectedId: id, selectedCorner: null });
  }

  togglePanel(id: PanelId): void {
    this.setPanel(id, !this.state.panels[id]);
  }

  setPanel(id: PanelId, visible: boolean): void {
    this.state.panels = { ...this.state.panels, [id]: visible };
    try {
      localStorage.setItem(STORAGE_KEY, JSON.stringify(this.state.panels));
    } catch {
      // not persisted
    }
    this.invalidate();
  }
}
