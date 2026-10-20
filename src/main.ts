// Wires the store, the render engine and the sidebar together.
// Step 1 has a single layer: choosing or opening a shader replaces it.

import './ui/styles.css';
import { APP_NAME, DEFAULT_OUTPUT_HEIGHT, DEFAULT_OUTPUT_WIDTH } from './config';
import { defaultValues, parseIsf } from './isf/parse';
import type { LayerStatus } from './layers/layer';
import { Engine } from './render/engine';
import { shaderLibrary } from './shaderLibrary';
import { Store } from './state/store';
import type { LayerState, ProjectState } from './state/types';
import { ControlsPanel, sliderControl } from './ui/controlsPanel';

function byId<T extends HTMLElement>(id: string): T {
  return document.getElementById(id) as T;
}

function makeLayer(fileName: string, source: string): LayerState {
  let values = {};
  try {
    values = defaultValues(parseIsf(source).meta.inputs);
  } catch {
    // A shader that fails to parse still becomes a layer; it shows the error.
  }
  return {
    id: crypto.randomUUID(),
    name: fileName.replace(/\.[^.]+$/, ''),
    kind: 'shader',
    fileName,
    source,
    values,
    visible: true,
    opacity: 1,
    blend: 'normal',
    speed: 1,
    paused: false,
    timeOffset: 0,
    quality: 1,
  };
}

document.title = APP_NAME;
byId('app-name').textContent = APP_NAME;

const params = new URLSearchParams(location.search);
const initial = shaderLibrary[Number(params.get('shader')) || 0];

const store = new Store<ProjectState>({
  layers: initial ? [makeLayer(initial.fileName, initial.source)] : [],
  settings: {
    outputWidth: DEFAULT_OUTPUT_WIDTH,
    outputHeight: DEFAULT_OUTPUT_HEIGHT,
    globalSpeed: 1,
    globalPaused: false,
  },
});

const canvas = byId<HTMLCanvasElement>('output');
const engine = new Engine(canvas);
const panel = new ControlsPanel(byId('controls'), store);

const statuses = new Map<string, LayerStatus>();
engine.onStatusChange = (id, status) => {
  statuses.set(id, status);
  panel.setStatus(id, status);
};
store.subscribe((project) => engine.sync(project));

function showLayer(): void {
  const layer = store.state.layers[0];
  panel.show(layer?.id ?? null);
  const status = layer && statuses.get(layer.id);
  if (layer && status) panel.setStatus(layer.id, status);
}

function loadShader(fileName: string, source: string): void {
  const layer = makeLayer(fileName, source);
  store.update((p) => {
    p.layers = [layer];
  });
  showLayer();
}

// Shader picker
const select = byId<HTMLSelectElement>('shader-select');
shaderLibrary.forEach((s, i) => {
  const o = document.createElement('option');
  o.value = String(i);
  o.textContent = s.fileName.replace(/\.fs$/, '');
  select.append(o);
});
select.value = String(Math.max(0, shaderLibrary.indexOf(initial)));
select.addEventListener('change', () => {
  const s = shaderLibrary[Number(select.value)];
  loadShader(s.fileName, s.source);
});

// Open a .fs file from disk
let openedOption: HTMLOptionElement | undefined;
const fileInput = byId<HTMLInputElement>('file-input');
byId('open-file').addEventListener('click', () => fileInput.click());
fileInput.addEventListener('change', async () => {
  const file = fileInput.files?.[0];
  fileInput.value = '';
  if (!file) return;
  loadShader(file.name, await file.text());
  openedOption ??= select.appendChild(document.createElement('option'));
  openedOption.textContent = `${file.name} (opened file)`;
  select.selectedIndex = select.options.length - 1;
});

// Global playback
const global = byId('global');
const heading = document.createElement('h3');
heading.textContent = 'All layers';
const playButton = document.createElement('button');
playButton.type = 'button';
playButton.className = 'primary';
const refreshPlay = () => {
  playButton.textContent = store.state.settings.globalPaused ? 'Play' : 'Pause';
  playButton.classList.toggle('paused', store.state.settings.globalPaused);
};
playButton.addEventListener('click', () => {
  store.update((p) => {
    p.settings.globalPaused = !p.settings.globalPaused;
  });
  refreshPlay();
});
refreshPlay();
const speed = sliderControl(
  {
    label: 'Global speed',
    def: 1,
    get: () => store.state.settings.globalSpeed,
    set: (v) =>
      store.update((p) => {
        p.settings.globalSpeed = v;
      }),
  },
  0,
  4,
  0.05,
);
global.append(heading, playButton, speed.el);

// FPS readout, throttled so the DOM is touched about four times a second.
const fpsEl = byId('fps');
setInterval(() => {
  fpsEl.textContent = `${Math.round(engine.fps)} fps`;
}, 250);

engine.sync(store.state);
showLayer();
engine.start();
