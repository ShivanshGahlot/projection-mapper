// Contract for the content library: built-in animations plus the shaders/
// folder. Implemented in library/index.ts.

export interface LibraryShader {
  /** Stable id stored in projects, e.g. 'builtin/mesh-gradient' or 'project/Frame 2 - A3 Fireworks.fs'. */
  id: string;
  /** Display name, e.g. 'Mesh Gradient'. */
  name: string;
  /** Group shown in the shader dropdown, e.g. 'Gradients', 'Noise & Organic', 'Project Shaders'. */
  category: string;
  fileName: string;
  source: string;
}

export interface Library {
  /** Every shader, grouped by category in display order. */
  shaders: LibraryShader[];
  byId(id: string): LibraryShader | undefined;
  /** The shader new Shader layers start with. */
  defaultShader: LibraryShader;
}
