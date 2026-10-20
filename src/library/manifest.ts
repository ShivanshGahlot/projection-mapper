// Display name, category and test hints for each built-in shader in library/builtin/<slug>.fs.
// The order here is the order shown in the shader dropdown.

export interface BuiltinInfo {
  slug: string;
  name: string;
  category: string;
  /** True when the picture is meant to stay still (skipped by the selftest's animation check). */
  static?: boolean;
}

export const builtinManifest: BuiltinInfo[] = [
  { slug: 'mesh-gradient', name: 'Mesh Gradient', category: 'Gradients' },
  { slug: 'radial-gradient', name: 'Radial Gradient', category: 'Gradients' },
  { slug: 'grain-gradient', name: 'Grain Gradient', category: 'Gradients' },
  { slug: 'linear-sweep', name: 'Linear Sweep', category: 'Gradients' },

  { slug: 'simplex-noise', name: 'Simplex Noise', category: 'Noise & Organic' },
  { slug: 'perlin-clouds', name: 'Perlin Clouds', category: 'Noise & Organic' },
  { slug: 'neuro-noise', name: 'Neuro Noise', category: 'Noise & Organic' },
  { slug: 'voronoi-cells', name: 'Voronoi Cells', category: 'Noise & Organic' },
  { slug: 'metaballs', name: 'Metaballs', category: 'Noise & Organic' },

  { slug: 'waves', name: 'Waves', category: 'Motion & Fluid' },
  { slug: 'swirl', name: 'Swirl', category: 'Motion & Fluid' },
  { slug: 'spiral', name: 'Spiral', category: 'Motion & Fluid' },
  { slug: 'smoke-ring', name: 'Smoke Ring', category: 'Motion & Fluid' },
  { slug: 'domain-warp', name: 'Domain Warp', category: 'Motion & Fluid' },
  { slug: 'plasma', name: 'Plasma', category: 'Motion & Fluid' },

  { slug: 'dot-grid', name: 'Dot Grid', category: 'Patterns & Dots' },
  { slug: 'dot-orbit', name: 'Dot Orbit', category: 'Patterns & Dots' },
  { slug: 'colour-panels', name: 'Colour Panels', category: 'Patterns & Dots' },
  { slug: 'stripes', name: 'Stripes', category: 'Patterns & Dots' },
  { slug: 'test-card', name: 'Checker / Test Card', category: 'Patterns & Dots', static: true },

  { slug: 'god-rays', name: 'God Rays', category: 'Effects' },
  { slug: 'pulsing-border', name: 'Pulsing Border', category: 'Effects' },
  { slug: 'strobe-flash', name: 'Strobe / Flash', category: 'Effects' },
  { slug: 'solid-colour', name: 'Solid Colour', category: 'Effects', static: true },
];
