// Observable store with undo/redo. Mutate the state in place inside update();
// listeners run afterwards. History holds snapshots taken before each change.

export interface UpdateOptions {
  /**
   * 'push' (default): new undo step.
   * 'merge': fold into the previous step if it has the same key (slider drags, corner drags).
   * 'none': not undoable (e.g. selection-like or runtime-only changes).
   */
  history?: 'push' | 'merge' | 'none';
  /** Steps with the same key merge when history is 'merge'. */
  key?: string;
}

const MAX_HISTORY = 100;

export class Store<T> {
  private listeners = new Set<(state: T) => void>();
  private undoStack: T[] = [];
  private redoStack: T[] = [];
  private lastKey: string | null = null;

  constructor(private current: T) {}

  get state(): T {
    return this.current;
  }

  update(mutate: (state: T) => void, opts: UpdateOptions = {}): void {
    const history = opts.history ?? 'push';
    if (history !== 'none') {
      const merge = history === 'merge' && opts.key !== undefined && opts.key === this.lastKey;
      if (!merge) {
        this.undoStack.push(structuredClone(this.current));
        if (this.undoStack.length > MAX_HISTORY) this.undoStack.shift();
      }
      this.redoStack = [];
      this.lastKey = history === 'merge' ? (opts.key ?? null) : null;
    }
    mutate(this.current);
    this.emit();
  }

  /** Replaces the whole state (e.g. opening a project). Clears history. */
  replace(state: T): void {
    this.current = state;
    this.undoStack = [];
    this.redoStack = [];
    this.lastKey = null;
    this.emit();
  }

  /** Ends any merge run, so the next 'merge' update starts a new undo step. */
  endMerge(): void {
    this.lastKey = null;
  }

  get canUndo(): boolean {
    return this.undoStack.length > 0;
  }

  get canRedo(): boolean {
    return this.redoStack.length > 0;
  }

  undo(): void {
    const prev = this.undoStack.pop();
    if (!prev) return;
    this.redoStack.push(this.current);
    this.current = prev;
    this.lastKey = null;
    this.emit();
  }

  redo(): void {
    const next = this.redoStack.pop();
    if (!next) return;
    this.undoStack.push(this.current);
    this.current = next;
    this.lastKey = null;
    this.emit();
  }

  subscribe(listener: (state: T) => void): () => void {
    this.listeners.add(listener);
    return () => this.listeners.delete(listener);
  }

  private emit(): void {
    for (const l of this.listeners) l(this.current);
  }
}
