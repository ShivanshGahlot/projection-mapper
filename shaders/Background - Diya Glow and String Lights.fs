/*{
  "DESCRIPTION": "Background for the gallery wall: a warm flickering glow rising from behind the sofa as if many diyas are hidden there, embers drifting up, swags of twinkling lights above and below the frames, and on each side a glowing akash kandil between cascading light strands. Every element can be switched on or off and adjusted, and the layout can be aligned to the real frames. Edges fade softly and nothing is projected on the sofa.",
  "CREDIT": "Created for Ghost Arcade",
  "ISFVSN": "2",
  "CATEGORIES": ["Generator", "Audio Reactive", "Projection Mapping"],
  "INPUTS": [
    {"NAME": "speed", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.25, "MAX": 3.0, "LABEL": "Speed"},
    {"NAME": "masterBrightness", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.1, "MAX": 1.5, "LABEL": "Overall Brightness"},

    {"NAME": "showGlow", "TYPE": "bool", "DEFAULT": true, "LABEL": "Sofa Glow: On"},
    {"NAME": "glowAmount", "TYPE": "float", "DEFAULT": 0.7, "MIN": 0.0, "MAX": 1.5, "LABEL": "Sofa Glow: Brightness"},
    {"NAME": "glowHeight", "TYPE": "float", "DEFAULT": 0.5, "MIN": 0.1, "MAX": 1.0, "LABEL": "Sofa Glow: Height"},
    {"NAME": "glowFlicker", "TYPE": "float", "DEFAULT": 0.5, "MIN": 0.0, "MAX": 1.0, "LABEL": "Sofa Glow: Flicker"},
    {"NAME": "glowColor", "TYPE": "long", "DEFAULT": 0, "VALUES": [0, 1, 2], "LABELS": ["Warm Diya", "Golden", "Rose"], "LABEL": "Sofa Glow: Colour"},

    {"NAME": "showEmbers", "TYPE": "bool", "DEFAULT": true, "LABEL": "Embers: On"},
    {"NAME": "emberAmount", "TYPE": "float", "DEFAULT": 0.6, "MIN": 0.0, "MAX": 1.0, "LABEL": "Embers: Amount"},
    {"NAME": "emberHeight", "TYPE": "float", "DEFAULT": 0.5, "MIN": 0.1, "MAX": 1.0, "LABEL": "Embers: How High They Rise"},

    {"NAME": "showTopLights", "TYPE": "bool", "DEFAULT": true, "LABEL": "Top String Lights: On"},
    {"NAME": "topLightsY", "TYPE": "float", "DEFAULT": 0.0, "MIN": -3.0, "MAX": 3.0, "LABEL": "Top String Lights: Move Up / Down (in)"},
    {"NAME": "topSag", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.0, "MAX": 2.0, "LABEL": "Top String Lights: Droop"},
    {"NAME": "showBottomLights", "TYPE": "bool", "DEFAULT": true, "LABEL": "Bottom String Lights: On"},
    {"NAME": "bottomLightsY", "TYPE": "float", "DEFAULT": 0.0, "MIN": -3.0, "MAX": 3.0, "LABEL": "Bottom String Lights: Move Up / Down (in)"},
    {"NAME": "bottomSag", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.0, "MAX": 2.0, "LABEL": "Bottom String Lights: Droop"},
    {"NAME": "lightColors", "TYPE": "long", "DEFAULT": 0, "VALUES": [0, 1, 2], "LABELS": ["Warm Gold", "Festive Colours", "Cool White"], "LABEL": "String Lights: Colour"},
    {"NAME": "lightsBrightness", "TYPE": "float", "DEFAULT": 0.8, "MIN": 0.0, "MAX": 1.5, "LABEL": "String Lights: Brightness"},
    {"NAME": "chaseSpeed", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.0, "MAX": 3.0, "LABEL": "String Lights: Chase Speed"},

    {"NAME": "showKandils", "TYPE": "bool", "DEFAULT": true, "LABEL": "Side Kandils: On"},
    {"NAME": "kandilSize", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.5, "MAX": 1.6, "LABEL": "Side Kandils: Size"},
    {"NAME": "kandilHeight", "TYPE": "float", "DEFAULT": 16.0, "MIN": 8.0, "MAX": 26.0, "LABEL": "Side Kandils: Hang Depth (in)"},
    {"NAME": "kandilGlow", "TYPE": "float", "DEFAULT": 0.85, "MIN": 0.0, "MAX": 1.0, "LABEL": "Side Kandils: Glow"},
    {"NAME": "showCascades", "TYPE": "bool", "DEFAULT": true, "LABEL": "Side Light Cascades: On"},
    {"NAME": "cascadeLength", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.4, "MAX": 1.4, "LABEL": "Side Light Cascades: Length"},
    {"NAME": "sideInset", "TYPE": "float", "DEFAULT": 4.6, "MIN": 2.0, "MAX": 10.0, "LABEL": "Side Elements: Distance From Edge (in)"},

    {"NAME": "galleryX", "TYPE": "float", "DEFAULT": 0.0, "MIN": -8.0, "MAX": 8.0, "LABEL": "Align: Shift Strands Left / Right (in)"},
    {"NAME": "galleryY", "TYPE": "float", "DEFAULT": 0.0, "MIN": -6.0, "MAX": 6.0, "LABEL": "Align: Shift Strands Up / Down (in)"},
    {"NAME": "galleryScale", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.7, "MAX": 1.3, "LABEL": "Align: Strand Spacing Scale"},
    {"NAME": "sofaLine", "TYPE": "float", "DEFAULT": 0.19, "MIN": 0.0, "MAX": 0.5, "LABEL": "Sofa Line (from bottom)"},
    {"NAME": "featherTop", "TYPE": "float", "DEFAULT": 0.12, "MIN": 0.0, "MAX": 0.4, "LABEL": "Fade Top Edge"},
    {"NAME": "featherSides", "TYPE": "float", "DEFAULT": 0.10, "MIN": 0.0, "MAX": 0.4, "LABEL": "Fade Side Edges"},
    {"NAME": "audioDrive", "TYPE": "float", "DEFAULT": 0.4, "MIN": 0.0, "MAX": 1.0, "LABEL": "Audio Drive"}
  ]
}*/

#ifdef GL_ES
precision highp float;
#endif

const vec2 CANVAS = vec2(64.0, 42.0);
const vec2 GAL = vec2(8.0, 3.0);

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

// Fabric akash kandil: a hexagonal body of satin panels split by gold trim, brocade bands with pearl
// beads, striped ribbon tassels and a twisted hanging cord. Local units: body half-width 1.
// Returns colour and coverage.
vec4 akashKandil(vec2 u, vec2 c, float sc, float apexY, float swing, float t, float id, vec3 sideC, vec3 tbC, vec3 tasA, vec3 tasB, float lightLvl, float glowAmt, float pxu) {
  float hh = 0.85;
  float wt = 0.55;
  float bandH = 0.18;
  vec2 piv = c + vec2(0.0, apexY * sc);
  vec2 p = u - piv;
  float cs = cos(swing);
  float sn = sin(swing);
  p = vec2(cs * p.x + sn * p.y, -sn * p.x + cs * p.y);
  vec2 q = p / sc + vec2(0.0, apexY);
  float pxl = pxu / sc;
  vec3 col = vec3(0.0);
  float cov = 0.0;
  if (abs(q.x) > 1.4 || q.y < -hh - bandH - 2.1 || q.y > apexY + 0.2) {
    return vec4(0.0);
  }

  // hanging cord: two twisted strands meeting above
  for (int k = 0; k < 2; k++) {
    float sx = float(k) * 2.0 - 1.0;
    float th = 0.0;
    float dc = segDist(q, vec2(sx * wt * 0.85, hh + bandH), vec2(0.0, apexY), th);
    float cord = lineMask(dc, 0.035, pxl * 1.5);
    float twist = step(0.5, fract(th * 18.0 + sx * 0.25));
    vec3 cc = mix(vec3(0.85, 0.15, 0.2), vec3(0.95, 0.75, 0.2), twist);
    if (id > 0.5 && id < 1.5) cc = mix(vec3(0.95, 0.70, 0.15), vec3(0.98, 0.85, 0.30), twist);
    if (id > 1.5) cc = mix(vec3(0.20, 0.55, 0.85), vec3(0.85, 0.20, 0.30), twist);
    col = mix(col, cc * lightLvl, cord);
    cov = max(cov, cord);
  }

  // hexagonal body
  float ay = abs(q.y);
  float halfX = 1.0 - (1.0 - wt) * ay / hh;
  float slant = sqrt(1.0 + ((1.0 - wt) / hh) * ((1.0 - wt) / hh));
  float dEdge = (halfX - abs(q.x)) / slant;
  float inBody = step(ay, hh) * smoothstep(-pxl, pxl, dEdge);
  float kx = wt / hh;
  float nrm = sqrt(1.0 + kx * kx);
  float dX1 = abs(q.x + q.y * kx) / nrm;
  float dX2 = abs(q.x - q.y * kx) / nrm;
  bool topBot = abs(q.x) < ay * kx;
  vec3 panel = topBot ? tbC : sideC;
  float inner = 0.55 + 0.75 * exp(-length(q * vec2(1.0, 1.2)) * 1.3) * glowAmt;
  float sheenDir = q.x < 0.0 ? 1.0 : -1.0;
  float sheen = 0.18 * sin(q.x * 3.0 * sheenDir + q.y * 4.0 + 1.0) + 0.12 * smoothstep(0.3, 0.0, abs(q.x * 0.7 - q.y * sheenDir * 0.5 - 0.2));
  vec3 bodyCol = panel * (inner + sheen) * (0.55 * lightLvl + 0.65 * glowAmt);
  float trim = max(lineMask(dEdge, 0.07, pxl * 1.5), max(lineMask(dX1, 0.045, pxl * 1.5), lineMask(dX2, 0.045, pxl * 1.5)));
  float beadT = 0.8 + 0.2 * sin((q.x + q.y) * 70.0);
  vec3 gold = vec3(1.0, 0.78, 0.30) * beadT;
  bodyCol = mix(bodyCol, gold * (0.6 * lightLvl + 0.5 * glowAmt), trim);
  col = mix(col, bodyCol, inBody);
  cov = max(cov, inBody);

  // brocade bands with a row of pearls next to the body
  for (int k = 0; k < 2; k++) {
    float sy = float(k) * 2.0 - 1.0;
    float y0 = sy * hh;
    float y1 = sy * (hh + bandH);
    float lo = min(y0, y1);
    float hi = max(y0, y1);
    float inBand = step(lo, q.y) * step(q.y, hi) * (1.0 - smoothstep(wt * 1.08 - pxl, wt * 1.08 + pxl, abs(q.x)));
    float brocade = 0.75 + 0.25 * step(0.5, fract(q.x * 9.0 + step(0.5, fract(q.y * 12.0)) * 0.5));
    vec3 bc = vec3(0.95, 0.72, 0.28) * brocade;
    float edgeLine = lineMask(min(abs(q.y - lo), abs(q.y - hi)), 0.015, pxl);
    bc = mix(bc, vec3(0.6, 0.35, 0.1), edgeLine * 0.6);
    float pearlY = y0 + sy * 0.045;
    float pi = floor(q.x / 0.085 + 0.5);
    vec2 pq = vec2(q.x - pi * 0.085, q.y - pearlY);
    float pearl = (1.0 - smoothstep(0.034 - pxl, 0.034 + pxl, length(pq))) * step(abs(q.x), wt * 1.05);
    vec3 pc = vec3(0.95, 0.93, 0.88) * (0.7 + 0.5 * smoothstep(0.03, -0.02, length(pq - vec2(-0.012, 0.012))));
    bc = mix(bc, pc, pearl);
    col = mix(col, bc * (0.55 * lightLvl + 0.45 * glowAmt + 0.2), max(inBand, pearl * step(abs(q.y), hh + 0.1)));
    cov = max(cov, max(inBand, pearl * step(abs(q.y), hh + 0.1)));
  }

  // ribbon tassels
  float topT = -hh - bandH;
  if (q.y < topT) {
    float depth = topT - q.y;
    float nR = 5.0;
    float rw = (2.0 * wt) / nR;
    float sway = sin(depth * 1.6 - t * 1.7 + id) * 0.06 * depth;
    float xs = q.x + sway + wt;
    float ri = floor(xs / rw);
    float fx = fract(xs / rw);
    float rlen = 1.75 * (0.82 + 0.18 * hash21(vec2(ri, id + 3.0)));
    float rag = 0.04 * sin(fx * 31.0 + ri);
    float inR = step(0.0, ri) * step(ri, nR - 1.0) * step(depth, rlen + rag) * (1.0 - smoothstep(0.88, 0.95, fx)) * smoothstep(0.02, 0.07, fx);
    float stripes = max(lineMask(abs(fx - 0.2), 0.035, 0.03), lineMask(abs(fx - 0.78), 0.035, 0.03));
    float motif = step(abs(fx - 0.5) * 2.0 + abs(fract(depth * 3.0) - 0.5) * 2.0, 0.42);
    vec3 rc = mix(tasA, tasB, stripes);
    rc = mix(rc, vec3(1.0, 0.8, 0.3), motif * 0.8);
    float twist = 0.7 + 0.3 * sin(depth * 2.2 + ri * 1.3 + t * 1.1);
    col = mix(col, rc * twist * (0.5 * lightLvl + 0.25 * glowAmt + 0.15), inR);
    cov = max(cov, inR);
  }
  return vec4(col, cov);
}

vec3 bulbColor(int mode, float i) {
  if (mode == 2) return mix(vec3(0.85, 0.92, 1.0), vec3(1.0, 1.0, 1.0), step(0.5, fract(i * 0.5)));
  if (mode == 0) return mix(vec3(1.0, 0.78, 0.40), vec3(1.0, 0.90, 0.65), step(0.5, fract(i * 0.5)));
  float k = mod(i, 5.0);
  if (k < 0.5) return vec3(1.0, 0.35, 0.30);
  if (k < 1.5) return vec3(1.0, 0.80, 0.30);
  if (k < 2.5) return vec3(0.40, 1.0, 0.45);
  if (k < 3.5) return vec3(0.45, 0.65, 1.0);
  return vec3(1.0, 0.45, 0.90);
}

vec3 glowTint(int mode) {
  if (mode == 1) return vec3(1.0, 0.70, 0.30);
  if (mode == 2) return vec3(1.0, 0.40, 0.45);
  return vec3(1.0, 0.55, 0.20);
}

// One swag of lights between two anchors (gallery inches, y down), sagging by sag inches.
vec3 swag(vec2 p, vec2 a, vec2 b, float sag, float tm, float chase, int mode, float bulbScale, float px) {
  vec3 col = vec3(0.0);
  if (p.x < min(a.x, b.x) - 1.0 || p.x > max(a.x, b.x) + 1.0) return col;
  float t = clamp((p.x - a.x) / (b.x - a.x), 0.0, 1.0);
  float yc = mix(a.y, b.y, t) + sag * 4.0 * t * (1.0 - t);
  col += vec3(0.05, 0.04, 0.03) * lineMask(abs(p.y - yc), 0.05, px);
  float spacing = 1.1;
  float bi = floor((p.x - a.x) / spacing + 0.5);
  float bx = a.x + bi * spacing;
  float bt = clamp((bx - a.x) / (b.x - a.x), 0.0, 1.0);
  float by = mix(a.y, b.y, bt) + sag * 4.0 * bt * (1.0 - bt) + 0.25;
  float d = length(p - vec2(bx, by));
  float idx = bi + floor(a.x);
  float wave = 0.55 + 0.45 * pow(0.5 + 0.5 * sin(idx * 0.6 - tm * 1.8 * chase), 3.0);
  float tw = 0.85 + 0.15 * sin(tm * 7.0 + idx * 3.1);
  vec3 bc = bulbColor(mode, idx);
  float inside = step(min(a.x, b.x), bx) * step(bx, max(a.x, b.x));
  col += bc * (lineMask(d, 0.16 * bulbScale, px) * 1.2 + exp(-d / 0.6) * 0.35) * mix(1.0, wave, step(0.001, chase)) * tw * inside;
  return col;
}

// Cascading strand of fairy lights hanging from the top, with light trickling downward.
vec3 cascade(vec2 w, float x0, float len, float tm, float id, int mode, float px) {
  vec3 col = vec3(0.0);
  if (abs(w.x - x0) > 1.5 || w.y > len + 1.0) return col;
  float sway = sin(tm * 0.6 + id) * 0.15 * (w.y / max(len, 1.0));
  col += vec3(0.05, 0.04, 0.03) * lineMask(abs(w.x - x0 - sway), 0.04, px) * step(w.y, len);
  float bi = floor(w.y / 1.0 + 0.5);
  float by = bi * 1.0;
  float bsway = sin(tm * 0.6 + id) * 0.15 * (by / max(len, 1.0));
  float d = length(w - vec2(x0 + bsway + (mod(bi, 2.0) - 0.5) * 0.18, by));
  float drip = pow(0.5 + 0.5 * sin(by * 0.55 - tm * 2.4 + id * 1.7), 6.0);
  float tw = 0.8 + 0.2 * sin(tm * 6.0 + bi * 2.3 + id);
  vec3 bc = bulbColor(mode, bi + id * 3.0);
  float on = step(1.0, by) * step(by, len);
  col += bc * (lineMask(d, 0.14, px) * 1.2 + exp(-d / 0.55) * 0.35) * (0.4 + 0.9 * drip) * tw * on;
  return col;
}

void main() {
  vec2 uv = gl_FragCoord.xy / RENDERSIZE.xy;
  vec2 w = vec2(uv.x, 1.0 - uv.y) * CANVAS;
  float px = max(CANVAS.x / RENDERSIZE.x, CANVAS.y / RENDERSIZE.y);

  float tm = TIME * speed;
  float master = masterBrightness;
  bool onGlow = showGlow;
  float gAmt = glowAmount;
  float gH = glowHeight;
  float gFl = glowFlicker;
  int gCol = int(glowColor);
  bool onEmbers = showEmbers;
  float eAmt = emberAmount;
  float eH = emberHeight;
  bool onTop = showTopLights;
  float topY = topLightsY;
  float tSag = topSag;
  bool onBottom = showBottomLights;
  float botY = bottomLightsY;
  float bSag = bottomSag;
  int lm = int(lightColors);
  float lAmt = lightsBrightness;
  float chase = chaseSpeed;
  bool onKandil = showKandils;
  float kSize = kandilSize;
  float kY = kandilHeight;
  float kGlow = kandilGlow;
  bool onCascade = showCascades;
  float cLen = cascadeLength;
  float inset = sideInset;
  vec2 gOff = vec2(galleryX, galleryY);
  float gScale = galleryScale;
  float sofaY = CANVAS.y * (1.0 - sofaLine);
  float fTop = featherTop;
  float fSides = featherSides;
  float bass = max(audioBass, 0.2 + 0.08 * sin(TIME * 0.7)) * audioDrive;

  vec3 col = vec3(0.0);

  // warm glow from hidden diyas behind the sofa
  if (onGlow) {
    float glow = 0.0;
    for (int i = 0; i < 14; i++) {
      float fi = float(i);
      float sx = (fi + 0.5) / 14.0 * CANVAS.x + (hash21(vec2(fi, 2.0)) - 0.5) * 3.0;
      float fl = 1.0 + gFl * 0.5 * sin(tm * (5.0 + 3.0 * hash21(vec2(fi, 5.0))) + fi * 2.3) * sin(tm * 3.1 + fi);
      vec2 d = (w - vec2(sx, sofaY + 1.0)) / vec2(7.0, 7.0 + 16.0 * gH);
      glow += exp(-length(d) * 1.6) * fl;
    }
    float rise = exp(-max(sofaY - w.y, 0.0) / (4.0 + 14.0 * gH));
    vec3 tint = glowTint(gCol);
    col += tint * glow * 0.26 * gAmt * (1.0 + bass * 0.3);
    col += tint * vec3(1.0, 0.85, 0.8) * rise * 0.26 * gAmt * (1.0 - gFl * 0.15 + gFl * 0.15 * vnoise(vec2(w.x * 0.3, tm * 1.5)));
  }

  // embers drifting up from behind the sofa
  if (onEmbers) {
    float nE = floor(eAmt * 40.0 + 0.5);
    for (int i = 0; i < 40; i++) {
      float fi = float(i);
      if (fi >= nE) break;
      float h0 = hash21(vec2(fi, 1.7));
      float rate = 0.08 + 0.06 * h0;
      float cyc = tm * rate + h0;
      float k = floor(cyc);
      float ph = fract(cyc);
      float hx = hash21(vec2(fi + 3.0, k));
      float hgt = 6.0 + 18.0 * eH * hash21(vec2(k, fi));
      vec2 ep = vec2(hx * CANVAS.x + sin(tm * 0.9 + fi * 2.0) * 0.8 * ph, sofaY - ph * hgt);
      float d = length(w - ep);
      float fade = smoothstep(0.0, 0.08, ph) * (1.0 - smoothstep(0.5, 1.0, ph));
      float flick = 0.6 + 0.4 * sin(tm * 11.0 + fi * 5.0);
      col += glowTint(gCol) * vec3(1.0, 1.1, 1.25) * (lineMask(d, 0.1, px) + exp(-d / 0.35) * 0.4) * fade * flick;
    }
  }

  // string lights above and below the frames, aligned to the gallery
  vec2 g = (w - GAL - gOff) / gScale;
  float pg = px / gScale;
  vec3 lights = vec3(0.0);
  float bScale = 1.0 / gScale;
  if (onTop) {
    float ty = -2.2 + topY / gScale;
    lights += swag(g, vec2(-12.0, ty), vec2(15.1, ty), 1.8 * tSag, tm, chase, lm, bScale, pg);
    lights += swag(g, vec2(15.1, ty), vec2(29.7, ty), 1.6 * tSag, tm, chase, lm, bScale, pg);
    lights += swag(g, vec2(29.7, ty), vec2(60.0, ty), 1.8 * tSag, tm, chase, lm, bScale, pg);
  }
  if (onBottom) {
    float by = 28.4 + botY / gScale;
    lights += swag(g, vec2(-12.0, by), vec2(11.1, by), 1.4 * bSag, tm, chase, lm, bScale, pg);
    lights += swag(g, vec2(11.1, by), vec2(20.1, by), 1.0 * bSag, tm, chase, lm, bScale, pg);
    lights += swag(g, vec2(20.1, by), vec2(33.1, by), 1.2 * bSag, tm, chase, lm, bScale, pg);
    lights += swag(g, vec2(33.1, by), vec2(60.0, by), 1.6 * bSag, tm, chase, lm, bScale, pg);
  }
  col += lights * lAmt * (1.0 + bass * 0.3);

  // sides: a hanging kandil on each side, between cascading strands of lights
  vec3 sides = vec3(0.0);
  for (int k = 0; k < 2; k++) {
    float sx = k == 0 ? 1.0 : -1.0;
    float xc = k == 0 ? inset : CANVAS.x - inset;
    float id = float(k);
    if (onCascade) {
      sides += cascade(w, xc - sx * 1.9 * kSize, 21.0 * cLen, tm, id * 4.0 + 1.0, lm, px) * lAmt;
      sides += cascade(w, xc + sx * 1.9 * kSize, 25.0 * cLen, tm, id * 4.0 + 2.0, lm, px) * lAmt;
      sides += cascade(w, xc + sx * 3.1 * kSize, 17.0 * cLen, tm, id * 4.0 + 3.0, lm, px) * lAmt;
    }
    if (onKandil) {
      vec3 sideC = k == 0 ? vec3(0.95, 0.12, 0.10) : vec3(0.10, 0.62, 0.66);
      vec3 tbC = k == 0 ? vec3(1.0, 0.85, 0.15) : vec3(1.0, 0.58, 0.10);
      vec3 tasA = k == 0 ? vec3(1.0, 0.72, 0.15) : vec3(1.0, 0.55, 0.12);
      vec3 tasB = k == 0 ? vec3(0.85, 0.15, 0.10) : vec3(0.50, 0.15, 0.55);
      float sc = 2.2 * kSize;
      vec2 pu = vec2(w.x, -w.y);
      float swingK = sin(tm * 0.7 + id * 2.0) * 0.04;
      vec4 kd = akashKandil(pu, vec2(xc, -kY), sc, (kY + 1.0) / sc, swingK, tm, id * 2.0, sideC, tbC, tasA, tasB, 0.55, kGlow, px);
      float kdist = length((w - vec2(xc, kY)) * vec2(1.0, 0.8));
      sides += mix(sideC, tbC, 0.4) * exp(-kdist / (3.0 * kSize)) * 0.35 * kGlow;
      sides = mix(sides, kd.rgb, kd.a);
    }
  }
  col += sides;

  // nothing on the sofa, and soft fades at the projection edges
  col *= 1.0 - smoothstep(sofaY - 0.4, sofaY + 0.4, w.y);
  col *= smoothstep(0.0, max(fSides * 0.5, 0.001), min(uv.x, 1.0 - uv.x));
  col *= smoothstep(0.0, max(fTop, 0.001), 1.0 - uv.y);
  col *= master;
  gl_FragColor = vec4(clamp(col, 0.0, 1.0), 1.0);
}
