// Everything the editor modules share, passed around as one object.

import type { Renderer } from '../render/api';
import type { OutputLink } from '../sync/outputLink';
import type { Core } from './layerOps';

export interface Ctx extends Core {
  renderer: Renderer;
  link: OutputLink;
  /** Project-level actions (new, open, save...). Filled in by projects.ts. */
  projects: ProjectActions;
}

export interface ProjectActions {
  newProject(): Promise<void>;
  duplicateProject(): Promise<void>;
  openProjectsDialog(): void;
  save(): Promise<void>;
  exportProject(): Promise<void>;
  importProject(): void;
  deleteProject(): Promise<void>;
  renameProject(name: string): void;
}
