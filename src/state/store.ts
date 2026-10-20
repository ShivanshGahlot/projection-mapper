// Minimal observable store: mutate the state in place, then listeners run.

export class Store<T> {
  private listeners = new Set<(state: T) => void>();

  constructor(public readonly state: T) {}

  update(mutate: (state: T) => void): void {
    mutate(this.state);
    for (const l of this.listeners) l(this.state);
  }

  subscribe(listener: (state: T) => void): () => void {
    this.listeners.add(listener);
    return () => this.listeners.delete(listener);
  }
}
