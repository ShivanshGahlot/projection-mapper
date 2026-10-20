// Contract for project and asset storage (IndexedDB). Implemented in
// persist/db.ts; used by the editor (projects, assets) and the output window
// (assets only).

import type { Project } from '../state/types';

export interface ProjectSummary {
  id: string;
  name: string;
  /** ms since epoch */
  updatedAt: number;
}

export interface ProjectStorage {
  list(): Promise<ProjectSummary[]>;
  load(id: string): Promise<Project | null>;
  save(project: Project): Promise<void>;
  remove(id: string): Promise<void>;
  /** Id of the project open most recently, for auto-load on startup. */
  lastOpenedId(): string | null;
  setLastOpenedId(id: string): void;
}

export interface AssetStorage {
  /** Stores a file (image or video) and returns its new asset id. */
  put(file: Blob, fileName: string): Promise<string>;
  /** Object URL for the asset, cached per id. Null if missing. */
  getAssetUrl(assetId: string): Promise<string | null>;
  get(assetId: string): Promise<{ blob: Blob; fileName: string } | null>;
}

/** Portable project file: the project plus every asset it references, base64-encoded. */
export interface ProjectFile {
  format: 'projection-mapper-project';
  version: 1;
  project: Project;
  assets: Record<string, { fileName: string; mimeType: string; base64: string }>;
}
