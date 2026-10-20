// Factories for new projects, scenes and layers.

import { DEFAULT_OUTPUT_HEIGHT, DEFAULT_OUTPUT_WIDTH } from '../config';
import { defaultValues, parseIsf } from '../isf/parse';
import type {
  Content,
  ContentLayer,
  ContentType,
  MaskLayer,
  Project,
  Quad,
  Scene,
  ShaderContent,
  Vec2,
} from './types';

export const newId = (): string => crypto.randomUUID();

/** A centred rectangle of the given size. */
export function centredQuad(canvasW: number, canvasH: number, w: number, h: number, offset: Vec2 = [0, 0]): Quad {
  const x0 = (canvasW - w) / 2 + offset[0];
  const y0 = (canvasH - h) / 2 + offset[1];
  return [
    [x0, y0],
    [x0 + w, y0],
    [x0 + w, y0 + h],
    [x0, y0 + h],
  ];
}

export function shaderContent(source: string, fileName: string, builtinId: string | null): ShaderContent {
  let values = {};
  try {
    values = defaultValues(parseIsf(source).meta.inputs);
  } catch {
    // A broken header shows as an error on the layer; values stay empty.
  }
  return { type: 'shader', builtinId, fileName, source, values, speed: 1, paused: false, timeOffset: 0 };
}

export function defaultContent(type: ContentType): Content {
  switch (type) {
    case 'surface':
      return { type: 'surface', fill: { type: 'color', color: '#ffffff' } };
    case 'shader':
      // The editor replaces this with the first built-in shader.
      return shaderContent('', '', null);
    case 'text':
      return {
        type: 'text',
        text: 'Sample Text',
        fontFamily: 'system-ui',
        italic: false,
        weight: 400,
        fontSize: 96,
        lineHeight: 1.2,
        letterSpacing: 0,
        uppercase: false,
        align: 'center',
        color: '#ffffff',
        background: null,
      };
    case 'webcam':
      return { type: 'webcam', deviceId: null, mirror: true };
  }
}

export function createContentLayer(name: string, content: Content, canvas: Project['canvas'], offset: Vec2 = [0, 0]): ContentLayer {
  const w = Math.round(canvas.width * 0.3);
  const h = Math.round(canvas.height * 0.3);
  return {
    kind: 'content',
    id: newId(),
    name,
    visible: true,
    locked: false,
    opacity: 1,
    corners: centredQuad(canvas.width, canvas.height, w, h, offset),
    shape: 'rect',
    cornerRadius: 0,
    edgeFeather: 0,
    blend: 'normal',
    crop: { x: 0, y: 0, w: 1, h: 1 },
    quality: 1,
    content,
  };
}

export function createMaskLayer(name: string, canvas: Project['canvas'], offset: Vec2 = [0, 0]): MaskLayer {
  const q = centredQuad(canvas.width, canvas.height, canvas.width * 0.2, canvas.height * 0.2, offset);
  return {
    kind: 'mask',
    id: newId(),
    name,
    visible: true,
    locked: false,
    opacity: 1,
    points: q.map((p) => [...p] as Vec2),
    feather: 0,
    invert: false,
    target: 'below',
    color: '#3b82f6',
  };
}

export function createScene(name: string): Scene {
  return { id: newId(), name, layers: [] };
}

export function createProject(name = 'Untitled Project'): Project {
  const scene = createScene('Scene 1');
  return {
    version: 1,
    id: newId(),
    name,
    canvas: { width: DEFAULT_OUTPUT_WIDTH, height: DEFAULT_OUTPUT_HEIGHT },
    output: { brightness: 1, background: '#000000', blackout: false, testPattern: false },
    playback: { speed: 1, paused: false },
    scenes: [scene],
    activeSceneId: scene.id,
  };
}

export function activeScene(project: Project): Scene {
  return project.scenes.find((s) => s.id === project.activeSceneId) ?? project.scenes[0];
}
