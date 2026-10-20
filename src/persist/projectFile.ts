// Portable project files: the project JSON plus every referenced asset as base64.

import type { Fill, Project } from '../state/types';
import type { AssetStorage, ProjectFile } from './api';

export const PROJECT_FILE_EXTENSION = '.mapping.json';

type MediaFill = Extract<Fill, { type: 'image' | 'video' }>;

/** Every image/video fill across all scenes. */
function mediaFills(project: Project): MediaFill[] {
  const out: MediaFill[] = [];
  for (const scene of project.scenes) {
    for (const layer of scene.layers) {
      if (layer.kind !== 'content' || layer.content.type !== 'surface') continue;
      const fill = layer.content.fill;
      if (fill.type === 'image' || fill.type === 'video') out.push(fill);
    }
  }
  return out;
}

function blobToBase64(blob: Blob): Promise<string> {
  return new Promise((resolve, reject) => {
    const reader = new FileReader();
    reader.onload = () => {
      const s = String(reader.result);
      resolve(s.slice(s.indexOf(',') + 1));
    };
    reader.onerror = () => reject(reader.error);
    reader.readAsDataURL(blob);
  });
}

function base64ToBlob(base64: string, mimeType: string): Blob {
  const bin = atob(base64);
  const bytes = new Uint8Array(bin.length);
  for (let i = 0; i < bin.length; i++) bytes[i] = bin.charCodeAt(i);
  return new Blob([bytes], { type: mimeType });
}

export async function exportProjectFile(project: Project, assets: AssetStorage): Promise<Blob> {
  const out: ProjectFile['assets'] = {};
  for (const fill of mediaFills(project)) {
    if (out[fill.assetId]) continue;
    const asset = await assets.get(fill.assetId);
    if (!asset) continue; // missing asset: the reference stays and shows as an error on import
    out[fill.assetId] = {
      fileName: asset.fileName,
      mimeType: asset.blob.type,
      base64: await blobToBase64(asset.blob),
    };
  }
  const file: ProjectFile = { format: 'projection-mapper-project', version: 1, project, assets: out };
  return new Blob([JSON.stringify(file)], { type: 'application/json' });
}

export async function importProjectFile(file: File, assets: AssetStorage): Promise<Project> {
  let data: ProjectFile;
  try {
    data = JSON.parse(await file.text()) as ProjectFile;
  } catch {
    throw new Error('Not a valid project file (could not read JSON).');
  }
  if (!data || data.format !== 'projection-mapper-project') throw new Error('Not a project file.');
  if (data.version !== 1) throw new Error(`Unsupported project file version: ${String(data.version)}.`);
  const project = data.project;
  if (!project || !Array.isArray(project.scenes) || !project.canvas) throw new Error('Project file is incomplete.');

  const idMap = new Map<string, string>();
  for (const [oldId, a] of Object.entries(data.assets ?? {})) {
    idMap.set(oldId, await assets.put(base64ToBlob(a.base64, a.mimeType), a.fileName));
  }
  for (const fill of mediaFills(project)) {
    const newId = idMap.get(fill.assetId);
    if (newId) fill.assetId = newId;
  }
  project.id = crypto.randomUUID();
  return project;
}

export function downloadBlob(blob: Blob, fileName: string): void {
  const url = URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = fileName;
  document.body.appendChild(a);
  a.click();
  a.remove();
  setTimeout(() => URL.revokeObjectURL(url), 10_000);
}
