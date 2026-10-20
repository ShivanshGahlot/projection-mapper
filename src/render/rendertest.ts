// Manual renderer test page. ?bench=1 runs 8 fireworks layers at 1920x1080 and reports fps.

import { shaderLibrary } from '../shaderLibrary';
import { createContentLayer, createMaskLayer, createProject, shaderContent } from '../state/defaults';
import type { ContentLayer, Project } from '../state/types';
import { createRenderer } from './renderer';

const bench = new URLSearchParams(location.search).has('bench');
const info = document.getElementById('info')!;
const outCanvas = document.getElementById('out') as HTMLCanvasElement;
const contentCanvas = document.getElementById('content') as HTMLCanvasElement;
const outCtx = outCanvas.getContext('2d')!;
const contentCtx = contentCanvas.getContext('2d')!;

const lib = (name: string) => {
  const s = shaderLibrary.find((l) => l.fileName.includes(name));
  if (!s) throw new Error(`Library shader "${name}" not found`);
  return s;
};

function buildProject(): { project: Project; shaderLayerId: string } {
  const project = createProject('Render test');
  const scene = project.scenes[0];
  const fw = lib('Fireworks');

  if (bench) {
    project.canvas = { width: 1920, height: 1080 };
    for (let i = 0; i < 8; i++) {
      const content = shaderContent(fw.source, fw.fileName, null);
      const l = createContentLayer(`fw${i}`, content, project.canvas);
      const x = (i % 4) * 400;
      const y = Math.floor(i / 4) * 440 + 40;
      // each layer ~ 25% of the canvas: 960 x 540
      l.corners = [[x * 0.6 + 0, y], [x * 0.6 + 960, y], [x * 0.6 + 960, y + 540], [x * 0.6, y + 540]];
      l.quality = 0.6;
      scene.layers.push(l);
    }
    return { project, shaderLayerId: scene.layers[0].id };
  }

  project.canvas = { width: 1280, height: 720 };
  project.output.background = '#101820';

  const calContent = shaderContent(fw.source, fw.fileName, null);
  calContent.values = { ...calContent.values, calibrate: true };
  const shader = createContentLayer('Warped shader', calContent, project.canvas);
  shader.corners = [[200, 50], [560, 110], [610, 440], [40, 500]];
  scene.layers.push(shader);

  const grad = createContentLayer('Gradient ellipse', { type: 'surface', fill: { type: 'gradient', kind: 'linear', angle: 45, stops: [{ pos: 0, color: '#ff3b6b' }, { pos: 0.5, color: '#ffd23b' }, { pos: 1, color: '#3b8bff' }] } }, project.canvas);
  grad.corners = [[690, 40], [1010, 40], [1010, 290], [690, 290]];
  grad.shape = 'ellipse';
  scene.layers.push(grad);

  const text = createContentLayer('Text', { type: 'text', text: 'Projection\nMapper', fontFamily: 'system-ui', italic: false, weight: 700, fontSize: 64, lineHeight: 1.1, letterSpacing: 2, uppercase: true, align: 'center', color: '#ffffff', background: '#2a2f6b' }, project.canvas);
  text.corners = [[700, 320], [1230, 320], [1230, 470], [700, 470]];
  scene.layers.push(text);

  const rect = createContentLayer('Rounded rect', { type: 'surface', fill: { type: 'color', color: '#ff8a1f' } }, project.canvas);
  rect.corners = [[700, 510], [980, 510], [980, 700], [700, 700]];
  rect.cornerRadius = 60;
  scene.layers.push(rect);

  const soft = createContentLayer('Feathered radial', { type: 'surface', fill: { type: 'gradient', kind: 'radial', angle: 0, stops: [{ pos: 0, color: '#ffffff' }, { pos: 1, color: '#2bd4a0' }] } }, project.canvas);
  soft.corners = [[1020, 510], [1250, 510], [1250, 700], [1020, 700]];
  soft.edgeFeather = 30;
  soft.blend = 'add';
  scene.layers.push(soft);

  const mask = createMaskLayer('Cut', project.canvas);
  mask.points = [[280, 230], [440, 230], [440, 340], [280, 340]];
  mask.target = 'below';
  scene.layers.push(mask);

  return { project, shaderLayerId: shader.id };
}

const { project, shaderLayerId } = buildProject();
const canvas = document.createElement('canvas');
const renderer = createRenderer(canvas, { getAssetUrl: async () => null });
renderer.onStatusChange = (id, s) => console.log('status', id, JSON.stringify(s));
renderer.setProject(project);

const W = project.canvas.width;
const H = project.canvas.height;
if (bench) {
  outCanvas.width = 960;
  outCanvas.height = 540;
}

let frames = 0;
let t0 = performance.now();
const contentLayers = project.scenes[0].layers.filter((l): l is ContentLayer => l.kind === 'content');
const statusLine = () => contentLayers.map((l) => { const s = renderer.status(l.id); return `${l.name}: ${s.state === 'error' ? s.message : s.state}`; }).join('  ');

function loop(now: number): void {
  requestAnimationFrame(loop);
  renderer.tick(now);
  renderer.renderOutput(W, H);
  renderer.copyTo(outCtx);
  if (!bench) {
    renderer.renderContent(shaderLayerId, 240, 135);
    renderer.copyTo(contentCtx);
  }
  frames++;
  if (now - t0 >= 1000) {
    const raw = (frames * 1000) / (now - t0);
    info.textContent = `${W}x${H} ${bench ? '8 shader layers' : 'test scene'}  fps(tick-smoothed)=${renderer.stats.fps.toFixed(1)}  fps(raw 1s)=${raw.toFixed(1)}\n${statusLine()}`;
    document.title = `fps ${renderer.stats.fps.toFixed(1)}`;
    frames = 0;
    t0 = now;
  }
}
requestAnimationFrame(loop);
