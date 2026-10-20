// Every .fs file in the project's shaders/ folder, bundled at build time so the
// app works offline. Drop new files into shaders/ and reload to see them.
const files = import.meta.glob('/shaders/*.fs', {
  query: '?raw',
  import: 'default',
  eager: true,
}) as Record<string, string>;

export interface LibraryShader {
  fileName: string;
  source: string;
}

export const shaderLibrary: LibraryShader[] = Object.entries(files)
  .map(([path, source]) => ({ fileName: path.split('/').pop() ?? path, source }))
  .sort((a, b) => a.fileName.localeCompare(b.fileName, undefined, { numeric: true }));
