/*{
  "DESCRIPTION": "Background behind the gallery: a giant glowing lotus blooms from behind the frames, its petals reaching through every gap and around the edges. It breathes open and partly closed, slowly turns, shifts colour over time, with golden light at its heart and pollen sparkles drifting up. Edges fade softly and nothing is projected on the sofa.",
  "CREDIT": "Created for Ghost Arcade",
  "ISFVSN": "2",
  "CATEGORIES": ["Generator", "Projection Mapping"],
  "INPUTS": [
    {"NAME": "speed", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.25, "MAX": 3.0, "LABEL": "Speed"},
    {"NAME": "brightness", "TYPE": "float", "DEFAULT": 0.85, "MIN": 0.1, "MAX": 1.0, "LABEL": "Brightness"},
    {"NAME": "lotusSize", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.6, "MAX": 1.5, "LABEL": "Lotus Size"},
    {"NAME": "centerX", "TYPE": "float", "DEFAULT": 0.5, "MIN": 0.2, "MAX": 0.8, "LABEL": "Centre Across"},
    {"NAME": "centerY", "TYPE": "float", "DEFAULT": 0.42, "MIN": 0.2, "MAX": 0.8, "LABEL": "Centre Down"},
    {"NAME": "colorMode", "TYPE": "long", "DEFAULT": 0, "VALUES": [0, 1, 2], "LABELS": ["Changing Over Time", "Classic Pink", "Golden"], "LABEL": "Lotus Colour"},
    {"NAME": "sparkleAmount", "TYPE": "float", "DEFAULT": 0.6, "MIN": 0.0, "MAX": 1.0, "LABEL": "Pollen Sparkles"},
    {"NAME": "sofaLine", "TYPE": "float", "DEFAULT": 0.19, "MIN": 0.0, "MAX": 0.5, "LABEL": "Sofa Line (from bottom)"},
    {"NAME": "featherTop", "TYPE": "float", "DEFAULT": 0.12, "MIN": 0.0, "MAX": 0.4, "LABEL": "Fade Top Edge"},
    {"NAME": "featherSides", "TYPE": "float", "DEFAULT": 0.10, "MIN": 0.0, "MAX": 0.4, "LABEL": "Fade Side Edges"}
  ]
}*/

#ifdef GL_ES
precision highp float;
#endif

const float TAU = 6.28318530718;
const float PI = 3.14159265359;
const vec2 CANVAS = vec2(64.0, 42.0);

float hash21(vec2 p) {
  p = fract(p * vec2(123.34, 456.21));
  p += dot(p, p + 45.32);
  return fract(p.x * p.y);
}

float vnoise(vec2 p) {
  vec2 i = floor(p);
  vec2 f = fract(p);
  f = f * f * (3.0 - 2.0 * f);
  float a = hash21(i);
  float b = hash21(i + vec2(1.0, 0.0));
  float c = hash21(i + vec2(0.0, 1.0));
  float d = hash21(i + vec2(1.0, 1.0));
  return mix(mix(a, b, f.x), mix(c, d, f.x), f.y);
}

float lineMask(float d, float w, float aa) {
  return 1.0 - smoothstep(w, w + aa, d);
}

float segDist(vec2 p, vec2 a, vec2 b, out float h) {
  vec2 pa = p - a;
  vec2 ba = b - a;
  h = clamp(dot(pa, ba) / max(dot(ba, ba), 0.0000001), 0.0, 1.0);
  return length(pa - ba * h);
}

// Soft fades at the projection edges and nothing projected on the sofa.
float wallMask(vec2 uv, vec2 w, float sofaY, float fTop, float fSides) {
  float m = 1.0 - smoothstep(sofaY - 0.4, sofaY + 0.4, w.y);
  m *= smoothstep(0.0, max(fSides * 0.5, 0.001), min(uv.x, 1.0 - uv.x));
  m *= smoothstep(0.0, max(fTop, 0.001), 1.0 - uv.y);
  return m;
}


vec3 petalPalette(float k, float along) {
  float i = mod(k, 4.0);
  vec3 base = vec3(0.90, 0.25, 0.50);
  vec3 tip = vec3(1.00, 0.80, 0.88);
  if (i > 0.5 && i < 1.5) { base = vec3(1.00, 0.50, 0.10); tip = vec3(1.00, 0.88, 0.45); }
  if (i > 1.5 && i < 2.5) { base = vec3(0.80, 0.12, 0.55); tip = vec3(1.00, 0.70, 0.90); }
  if (i > 2.5) { base = vec3(0.62, 0.40, 0.95); tip = vec3(0.95, 0.88, 1.00); }
  return mix(base, tip, smoothstep(0.2, 1.0, along));
}

vec3 lotusColor(int mode, float tm, float along) {
  if (mode == 1) return petalPalette(0.0, along);
  if (mode == 2) return petalPalette(1.0, along);
  float ph = tm / 20.0;
  float idx = floor(ph);
  float fr = smoothstep(0.6, 1.0, fract(ph));
  return mix(petalPalette(idx, along), petalPalette(idx + 1.0, along), fr);
}

// One ring of petals. Returns colour and coverage.
vec4 petalRing(vec2 p, float n, float len, float wid, float rot, float open, float tm, int mode, float shade, float px) {
  float r = length(p);
  float a = atan(p.y, p.x) - rot;
  float base = floor(a / TAU * n + 0.5);
  vec3 col = vec3(0.0);
  float cov = 0.0;
  for (int j = -1; j <= 1; j++) {
    float k = base + float(j);
    float th = k / n * TAU + rot;
    vec2 dir = vec2(cos(th), sin(th));
    vec2 perp = vec2(-dir.y, dir.x);
    float L = len * open;
    float along = dot(p, dir) / max(L, 0.001);
    float across = dot(p, perp);
    float prof = wid * open * pow(max(sin(PI * pow(clamp(along, 0.0, 1.0), 0.75)), 0.0), 0.8);
    float inside = step(0.0, along) * step(along, 1.0) * (1.0 - smoothstep(prof - px * 2.0, prof + px * 2.0, abs(across)));
    if (inside < 0.001) continue;
    float edge = 1.0 - smoothstep(0.0, max(prof, 0.001) * 0.35, prof - abs(across));
    float vein = 0.5 + 0.5 * cos(across / max(prof, 0.001) * 9.0);
    vec3 pc = lotusColor(mode, tm, along);
    float glowIn = 0.55 + 0.45 * (1.0 - along);
    vec3 c = pc * (0.75 + 0.35 * vein * (1.0 - edge)) * glowIn * shade * 1.25;
    c += mix(pc, vec3(1.0, 0.92, 0.75), 0.4) * edge * 0.9;
    col = mix(col, c, inside);
    cov = max(cov, inside);
  }
  return vec4(col, cov);
}

void main() {
  vec2 uv = gl_FragCoord.xy / RENDERSIZE.xy;
  vec2 w = vec2(uv.x, 1.0 - uv.y) * CANVAS;
  float px = max(CANVAS.x / RENDERSIZE.x, CANVAS.y / RENDERSIZE.y);
  float tm = TIME * speed;
  int mode = int(colorMode);
  float sz = lotusSize;
  float bright = brightness;
  float sparkle = sparkleAmount;
  float sofaY = CANVAS.y * (1.0 - sofaLine);
  vec2 cen = vec2(centerX, centerY) * CANVAS;
  vec2 p = (w - cen) / sz;

  // gentle breathing bloom: never fully closed, never stops
  float breathe = 0.5 + 0.5 * sin(tm * TAU / 24.0);
  float openO = 0.72 + 0.28 * smoothstep(0.0, 1.0, breathe);
  float openM = 0.70 + 0.30 * smoothstep(0.0, 1.0, 0.5 + 0.5 * sin(tm * TAU / 24.0 - 0.6));
  float openI = 0.68 + 0.32 * smoothstep(0.0, 1.0, 0.5 + 0.5 * sin(tm * TAU / 24.0 - 1.2));
  float rot = tm * 0.012;

  vec3 col = vec3(0.0);
  float r = length(p);
  col += vec3(1.0, 0.65, 0.30) * exp(-r / 7.0) * 0.35;
  col += lotusColor(mode, tm, 0.3) * exp(-r / 22.0) * 0.08;

  vec4 o = petalRing(p, 12.0, 30.0, 6.5, rot, openO, tm, mode, 0.75, px);
  col = mix(col, o.rgb, o.a);
  vec4 m = petalRing(p, 12.0, 21.0, 5.0, rot + PI / 12.0, openM, tm, mode, 0.9, px);
  col = mix(col, m.rgb, m.a);
  vec4 i = petalRing(p, 8.0, 12.5, 4.0, -rot * 1.5, openI, tm, mode, 1.05, px);
  col = mix(col, i.rgb, i.a);

  // seed pod at the heart with glowing seeds
  float pod = 1.0 - smoothstep(3.0, 3.2, r);
  vec3 podCol = vec3(0.95, 0.75, 0.25) * (0.6 + 0.4 * exp(-r / 2.0));
  vec2 sq = p * 1.6;
  float seeds = lineMask(length(fract(sq) - 0.5), 0.13, 0.08) * step(0.4, hash21(floor(sq)));
  podCol = mix(podCol, vec3(1.0, 0.95, 0.75) * 1.2, seeds);
  col = mix(col, podCol, pod);
  col += vec3(1.0, 0.85, 0.5) * exp(-r / 2.5) * 0.4;

  // pollen sparkles drifting up from the heart
  float nS = floor(sparkle * 40.0 + 0.5);
  for (int s = 0; s < 40; s++) {
    float fs = float(s);
    if (fs >= nS) break;
    float h0 = hash21(vec2(fs, 3.1));
    float cyc = tm * (0.05 + 0.04 * h0) + h0;
    float k = floor(cyc);
    float ph = fract(cyc);
    float ang = hash21(vec2(fs, k)) * TAU;
    float dist = (4.0 + 26.0 * ph) * (0.6 + 0.4 * hash21(vec2(k, fs)));
    vec2 sp = cen + vec2(cos(ang), sin(ang) * 0.8) * dist * sz + vec2(sin(tm + fs) * 0.6, -ph * 6.0);
    float d = length(w - sp);
    float fade = smoothstep(0.0, 0.1, ph) * (1.0 - smoothstep(0.6, 1.0, ph));
    float tw = 0.6 + 0.4 * sin(tm * 6.0 + fs * 3.0);
    col += vec3(1.0, 0.9, 0.6) * (lineMask(d, 0.09, px) + exp(-d / 0.35) * 0.35) * fade * tw;
  }

  col *= bright * wallMask(uv, w, sofaY, featherTop, featherSides);
  gl_FragColor = vec4(clamp(col, 0.0, 1.0), 1.0);
}
