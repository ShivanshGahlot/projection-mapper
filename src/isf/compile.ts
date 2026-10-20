// Turns a parsed ISF shader into a WebGL program: injects the declarations the
// host is expected to provide, compiles as GLSL ES 1.00 and builds uniform setters.

import type { InputValue, IsfInput, ParsedIsf } from './parse';

/** Uniforms the host provides to every shader, plus the audio ones Ghost Arcade shaders expect. */
export const HOST_UNIFORMS: ReadonlyArray<readonly [glslType: string, name: string]> = [
  ['float', 'TIME'],
  ['float', 'TIMEDELTA'],
  ['int', 'FRAMEINDEX'],
  ['vec2', 'RENDERSIZE'],
  ['vec4', 'DATE'],
  ['float', 'audioBass'],
  ['float', 'audioMid'],
  ['float', 'audioHigh'],
  ['float', 'audioBeat'],
  ['float', 'audioLevel'],
];

const INPUT_GLSL_TYPE: Record<string, string> = {
  float: 'float',
  long: 'int',
  bool: 'bool',
  event: 'bool',
  color: 'vec4',
  point2D: 'vec2',
  image: 'sampler2D',
};

export interface HostValues {
  time: number;
  timeDelta: number;
  frameIndex: number;
  renderWidth: number;
  renderHeight: number;
  /** year, month (1-12), day, seconds since midnight */
  date: readonly [number, number, number, number];
  audioBass: number;
  audioMid: number;
  audioHigh: number;
  audioBeat: number;
  audioLevel: number;
}

export const VERTEX_SOURCE = `#version 100
attribute vec2 a_pos;
varying vec2 isf_FragNormCoord;
void main() {
  isf_FragNormCoord = a_pos * 0.5 + 0.5;
  gl_Position = vec4(a_pos, 0.0, 1.0);
}
`;

function stripComments(src: string): string {
  return src.replace(/\/\*[\s\S]*?\*\//g, (c) => c.replace(/[^\n]/g, ' ')).replace(/\/\/.*$/gm, '');
}

function declaredNames(code: string, qualifier: 'uniform' | 'varying'): Set<string> {
  const names = new Set<string>();
  const re = new RegExp(`\\b${qualifier}\\s+(?:(?:lowp|mediump|highp)\\s+)?\\w+\\s+([^;]+);`, 'g');
  for (const m of code.matchAll(re)) {
    for (const part of m[1].split(',')) {
      const n = /^\s*(\w+)/.exec(part);
      if (n) names.add(n[1]);
    }
  }
  return names;
}

/**
 * Builds the GLSL ES 1.00 fragment source. The prelude ends with `#line 1`, so
 * compiler errors report line numbers from the original .fs file.
 */
export function buildFragmentSource(parsed: ParsedIsf): string {
  const extensions: string[] = [];
  // #version must come first and #extension before any code, so lift them out
  // (replacing each with an empty line to keep line numbers intact).
  const body = parsed.body
    .replace(/^[ \t]*#[ \t]*version\b.*$/gm, '')
    .replace(/^[ \t]*#[ \t]*extension\b.*$/gm, (line) => {
      extensions.push(line.trim());
      return '';
    });

  const code = stripComments(body);
  const uniforms = declaredNames(code, 'uniform');
  const prelude = ['#version 100', ...extensions, 'precision highp float;', 'precision highp int;'];
  for (const [type, name] of HOST_UNIFORMS) {
    if (!uniforms.has(name)) prelude.push(`uniform ${type} ${name};`);
  }
  for (const input of parsed.meta.inputs) {
    const type = INPUT_GLSL_TYPE[input.type];
    if (type && !uniforms.has(input.name)) prelude.push(`uniform ${type} ${input.name};`);
  }
  if (/\bisf_FragNormCoord\b/.test(code) && !declaredNames(code, 'varying').has('isf_FragNormCoord')) {
    prelude.push('varying vec2 isf_FragNormCoord;');
  }
  prelude.push('#line 1');
  return prelude.join('\n') + '\n' + body;
}

type Setter = (value: InputValue) => void;

/** One compiled ISF shader. Compiles in the background when the browser supports it. */
export class IsfProgram {
  private readonly program: WebGLProgram;
  private readonly fragShader: WebGLShader;
  private state: 'pending' | 'ready' | 'error' = 'pending';
  private errorLog = '';
  private hostLocs = new Map<string, WebGLUniformLocation>();
  private inputSetters = new Map<string, Setter>();
  private readonly date = new Float32Array(4);

  constructor(
    private readonly gl: WebGL2RenderingContext,
    private readonly parsed: ParsedIsf,
  ) {
    this.fragShader = gl.createShader(gl.FRAGMENT_SHADER)!;
    gl.shaderSource(this.fragShader, buildFragmentSource(parsed));
    gl.compileShader(this.fragShader);
    this.program = gl.createProgram()!;
    gl.attachShader(this.program, sharedVertexShader(gl));
    gl.attachShader(this.program, this.fragShader);
    gl.bindAttribLocation(this.program, 0, 'a_pos');
    gl.linkProgram(this.program);
    if (!parallelCompileExt(gl)) this.finish();
  }

  get inputs(): IsfInput[] {
    return this.parsed.meta.inputs;
  }

  /** Checks whether compilation has finished. Cheap; call once per frame. */
  poll(): 'pending' | 'ready' | 'error' {
    if (this.state === 'pending') {
      const ext = parallelCompileExt(this.gl);
      if (!ext || this.gl.getProgramParameter(this.program, ext.COMPLETION_STATUS_KHR)) this.finish();
    }
    return this.state;
  }

  get error(): string {
    return this.errorLog;
  }

  private finish(): void {
    const gl = this.gl;
    if (!gl.getProgramParameter(this.program, gl.LINK_STATUS)) {
      const fragLog = gl.getShaderParameter(this.fragShader, gl.COMPILE_STATUS) ? '' : gl.getShaderInfoLog(this.fragShader);
      this.errorLog = (fragLog || gl.getProgramInfoLog(this.program) || 'Unknown compile error').trim();
      this.state = 'error';
      return;
    }
    const count = gl.getProgramParameter(this.program, gl.ACTIVE_UNIFORMS) as number;
    const active = new Map<string, { loc: WebGLUniformLocation; type: number }>();
    for (let i = 0; i < count; i++) {
      const info = gl.getActiveUniform(this.program, i);
      const loc = info && gl.getUniformLocation(this.program, info.name);
      if (info && loc) active.set(info.name, { loc, type: info.type });
    }
    for (const [, name] of HOST_UNIFORMS) {
      const u = active.get(name);
      if (u) this.hostLocs.set(name, u.loc);
    }
    gl.useProgram(this.program);
    let textureUnit = 0;
    for (const input of this.parsed.meta.inputs) {
      const u = active.get(input.name);
      if (!u) continue; // unused by the shader, optimised away
      if (u.type === gl.SAMPLER_2D) {
        // Image inputs are not wired up yet (ISF phase 2); they sample unit N, left unbound.
        gl.uniform1i(u.loc, textureUnit++);
        continue;
      }
      this.inputSetters.set(input.name, makeSetter(gl, u.loc, u.type));
    }
    this.state = 'ready';
  }

  /** Binds the program and sets every host uniform and input. Only valid when ready. */
  use(host: HostValues, values: Record<string, InputValue>): void {
    const gl = this.gl;
    gl.useProgram(this.program);
    const h = this.hostLocs;
    let loc: WebGLUniformLocation | undefined;
    if ((loc = h.get('TIME'))) gl.uniform1f(loc, host.time);
    if ((loc = h.get('TIMEDELTA'))) gl.uniform1f(loc, host.timeDelta);
    if ((loc = h.get('FRAMEINDEX'))) gl.uniform1i(loc, host.frameIndex);
    if ((loc = h.get('RENDERSIZE'))) gl.uniform2f(loc, host.renderWidth, host.renderHeight);
    if ((loc = h.get('DATE'))) {
      this.date.set(host.date);
      gl.uniform4fv(loc, this.date);
    }
    if ((loc = h.get('audioBass'))) gl.uniform1f(loc, host.audioBass);
    if ((loc = h.get('audioMid'))) gl.uniform1f(loc, host.audioMid);
    if ((loc = h.get('audioHigh'))) gl.uniform1f(loc, host.audioHigh);
    if ((loc = h.get('audioBeat'))) gl.uniform1f(loc, host.audioBeat);
    if ((loc = h.get('audioLevel'))) gl.uniform1f(loc, host.audioLevel);
    for (const [name, set] of this.inputSetters) {
      const v = values[name];
      if (v !== undefined) set(v);
    }
  }

  dispose(): void {
    this.gl.deleteProgram(this.program);
    this.gl.deleteShader(this.fragShader);
  }
}

function makeSetter(gl: WebGL2RenderingContext, loc: WebGLUniformLocation, type: number): Setter {
  const asNumber = (v: InputValue) => (typeof v === 'boolean' ? (v ? 1 : 0) : Array.isArray(v) ? (v[0] ?? 0) : v);
  switch (type) {
    case gl.FLOAT:
      return (v) => gl.uniform1f(loc, asNumber(v));
    case gl.INT:
    case gl.BOOL:
      return (v) => gl.uniform1i(loc, Math.round(asNumber(v)));
    case gl.FLOAT_VEC2:
      return (v) => Array.isArray(v) && gl.uniform2f(loc, v[0] ?? 0, v[1] ?? 0);
    case gl.FLOAT_VEC4:
      return (v) => Array.isArray(v) && gl.uniform4f(loc, v[0] ?? 0, v[1] ?? 0, v[2] ?? 0, v[3] ?? 1);
    default:
      return () => {};
  }
}

const vertexShaders = new WeakMap<WebGL2RenderingContext, WebGLShader>();
function sharedVertexShader(gl: WebGL2RenderingContext): WebGLShader {
  let vs = vertexShaders.get(gl);
  if (!vs) {
    vs = gl.createShader(gl.VERTEX_SHADER)!;
    gl.shaderSource(vs, VERTEX_SOURCE);
    gl.compileShader(vs);
    vertexShaders.set(gl, vs);
  }
  return vs;
}

interface ParallelCompileExt {
  COMPLETION_STATUS_KHR: number;
}
const parallelExts = new WeakMap<WebGL2RenderingContext, ParallelCompileExt | null>();
function parallelCompileExt(gl: WebGL2RenderingContext): ParallelCompileExt | null {
  if (!parallelExts.has(gl)) parallelExts.set(gl, gl.getExtension('KHR_parallel_shader_compile'));
  return parallelExts.get(gl) ?? null;
}
