// Registry of content sources. Adding a content type = one entry here.

import type { Content, ContentType } from '../../state/types';
import type { ContentSource, SourceEnv } from '../source';
import { ShaderSource } from './shaderSource';
import { SurfaceSource } from './surfaceSource';
import { TextSource } from './textSource';
import { WebcamSource } from './webcamSource';

type Factory = (env: SourceEnv, content: Content) => ContentSource;

const factories: { [K in ContentType]: Factory } = {
  shader: (env, c) => new ShaderSource(env.gl, c as Extract<Content, { type: 'shader' }>),
  surface: (env, c) => new SurfaceSource(env, c as Extract<Content, { type: 'surface' }>),
  text: (env, c) => new TextSource(env.gl, c as Extract<Content, { type: 'text' }>),
  webcam: (env, c) => new WebcamSource(env, c as Extract<Content, { type: 'webcam' }>),
};

export function createSource(env: SourceEnv, content: Content): ContentSource {
  return factories[content.type](env, content);
}
