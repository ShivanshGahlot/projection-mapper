// Projector window: draws the project it receives from the editor, full window.
// With ?demo it builds a small project when no editor is connected.

import { assetStorage } from '../persist/db';
import { createRenderer } from '../render/renderer';
import { CHANNEL_NAME, type EditorToOutput, type OutputToEditor } from '../sync/messages';
import { activeScene, createContentLayer, createProject } from '../state/defaults';
import type { Project } from '../state/types';

const stage = document.getElementById('stage') as HTMLDivElement;
const hint = document.getElementById('hint') as HTMLDivElement;

const renderer = createRenderer(document.createElement('canvas'), assetStorage);
stage.appendChild(renderer.canvas);
const overlay = document.createElement('canvas');
overlay.id = 'overlay';
stage.appendChild(overlay);
const octx = overlay.getContext('2d')!;

const channel = new BroadcastChannel(CHANNEL_NAME);
const send = (m: OutputToEditor) => channel.postMessage(m);

let project: Project | null = null;
let identifyUntil = 0;
let hintUntil = performance.now() + 3000;
let lastMove = performance.now();
let W = 0;
let H = 0;
const errors: Record<string, string> = {};

renderer.onStatusChange = (id, st) => {
  if (st.state === 'error') errors[id] = st.message;
  else delete errors[id];
};

function demoProject(): Project {
  const p = createProject('Demo');
  const scene = activeScene(p);
  const colors = ['#e63946', '#2a9d8f', '#e9c46a'];
  const names = ['Left', 'Centre', 'Right'];
  colors.forEach((color, i) => {
    const layer = createContentLayer(names[i], { type: 'surface', fill: { type: 'color', color } }, p.canvas, [(i - 1) * p.canvas.width * 0.32, 0]);
    scene.layers.push(layer);
  });
  p.output.testPattern = new URLSearchParams(location.search).has('grid');
  return p;
}

function setProject(p: Project): void {
  project = p;
  renderer.setProject(p);
}

channel.onmessage = (e: MessageEvent<EditorToOutput>) => {
  const m = e.data;
  if (m.type === 'project') setProject(m.project);
  else if (m.type === 'identify') identifyUntil = performance.now() + 3000;
  else if (m.type === 'ping') sendStatus();
};

function sendStatus(): void {
  send({ type: 'status', width: W, height: H, fullscreen: !!document.fullscreenElement, fps: Math.round(renderer.stats.fps), errors: { ...errors } });
}

/** Sizes the stage, renderer canvas and overlay for the current window. Returns the fitted project size in device px. */
function layout(): { fw: number; fh: number; dpr: number } {
  const dpr = window.devicePixelRatio || 1;
  W = Math.max(1, Math.round(window.innerWidth * dpr));
  H = Math.max(1, Math.round(window.innerHeight * dpr));
  const pw = project?.canvas.width ?? W;
  const ph = project?.canvas.height ?? H;
  // Exact fill when sizes match; otherwise letterbox, preserving aspect.
  const scale = pw === W && ph === H ? 1 : Math.min(W / pw, H / ph);
  const fw = Math.max(1, Math.round(pw * scale));
  const fh = Math.max(1, Math.round(ph * scale));
  const cw = fw / dpr;
  const ch = fh / dpr;
  stage.style.width = `${cw}px`;
  stage.style.height = `${ch}px`;
  stage.style.left = `${(window.innerWidth - cw) / 2}px`;
  stage.style.top = `${(window.innerHeight - ch) / 2}px`;
  // Backing store == fitted size, so CSS maps it 1:1 to device pixels.
  if (renderer.canvas.width !== fw || renderer.canvas.height !== fh) {
    renderer.canvas.width = fw;
    renderer.canvas.height = fh;
  }
  renderer.canvas.style.width = `${cw}px`;
  renderer.canvas.style.height = `${ch}px`;
  overlay.style.cssText = `left:0;top:0;width:${cw}px;height:${ch}px`;
  if (overlay.width !== fw || overlay.height !== fh) {
    overlay.width = fw;
    overlay.height = fh;
  }
  return { fw, fh, dpr };
}

function drawIdentify(fw: number, fh: number): void {
  octx.clearRect(0, 0, fw, fh);
  if (!project || performance.now() > identifyUntil) return;
  const k = fw / project.canvas.width;
  octx.textAlign = 'center';
  octx.textBaseline = 'middle';
  octx.lineJoin = 'round';
  octx.font = `bold ${Math.round(Math.max(24, fh / 14))}px system-ui, sans-serif`;
  octx.lineWidth = Math.max(4, fh / 120);
  for (const layer of activeScene(project).layers) {
    if (layer.kind !== 'content' || !layer.visible) continue;
    const cx = (layer.corners.reduce((s, c) => s + c[0], 0) / 4) * k;
    const cy = (layer.corners.reduce((s, c) => s + c[1], 0) / 4) * k;
    octx.strokeStyle = '#000';
    octx.strokeText(layer.name, cx, cy);
    octx.fillStyle = '#fff';
    octx.fillText(layer.name, cx, cy);
  }
}

function frame(now: number): void {
  const { fw, fh } = layout();
  if (project) {
    renderer.tick(now);
    renderer.renderOutput(fw, fh);
  }
  drawIdentify(fw, fh);
  const fs = !!document.fullscreenElement;
  hint.classList.toggle('hidden', fs || now > hintUntil);
  document.documentElement.style.cursor = fs || now - lastMove > 2000 ? 'none' : '';
  requestAnimationFrame(frame);
}

function toggleFullscreen(): void {
  if (document.fullscreenElement) void document.exitFullscreen();
  else void document.documentElement.requestFullscreen().catch(() => {});
}

window.addEventListener('keydown', (e) => {
  if (e.key === 'f' || e.key === 'F') toggleFullscreen();
});
window.addEventListener('dblclick', toggleFullscreen);
window.addEventListener('mousemove', () => {
  lastMove = performance.now();
});
window.addEventListener('resize', () => {
  layout();
  sendStatus();
});
document.addEventListener('fullscreenchange', () => {
  layout();
  sendStatus();
});
window.addEventListener('pagehide', () => send({ type: 'bye' }));
setInterval(sendStatus, 1000);

layout();
send({ type: 'hello' });
if (new URLSearchParams(location.search).has('demo')) {
  // Give a connected editor a moment to answer 'hello' before falling back to the demo.
  setTimeout(() => {
    if (!project) {
      setProject(demoProject());
      identifyUntil = performance.now() + 3000;
    }
  }, 300);
}
requestAnimationFrame(frame);
