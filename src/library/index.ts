// The content library: built-in animations followed by every file in the project's shaders/ folder.

import { shaderLibrary } from '../shaderLibrary';
import type { Library, LibraryShader } from './api';
import { builtinManifest } from './manifest';

const PROJECT_CATEGORY = 'Project Shaders';
const DEFAULT_SLUG = 'mesh-gradient';

const builtinFiles = import.meta.glob('./builtin/*.fs', {
  query: '?raw',
  import: 'default',
  eager: true,
}) as Record<string, string>;

const builtins: LibraryShader[] = builtinManifest.map((info) => {
  const source = builtinFiles[`./builtin/${info.slug}.fs`];
  if (source === undefined) throw new Error(`Built-in shader file missing: builtin/${info.slug}.fs`);
  return { id: `builtin/${info.slug}`, name: info.name, category: info.category, fileName: `${info.slug}.fs`, source };
});

const projectShaders: LibraryShader[] = shaderLibrary.map((s) => ({
  id: `project/${s.fileName}`,
  name: s.fileName.replace(/\.fs$/, ''),
  category: PROJECT_CATEGORY,
  fileName: s.fileName,
  source: s.source,
}));

const shaders = [...builtins, ...projectShaders];
const byId = new Map(shaders.map((s) => [s.id, s]));

const defaultShader = byId.get(`builtin/${DEFAULT_SLUG}`);
if (!defaultShader) throw new Error(`Default shader builtin/${DEFAULT_SLUG} is not in the manifest`);

export const library: Library = {
  shaders,
  byId: (id) => byId.get(id),
  defaultShader,
};
