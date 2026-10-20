/*{
  "DESCRIPTION": "8x10 landscape frame (12x10 mount). A row of clay diyas on a polished ledge under a marigold and mango-leaf toran, with glowing fabric akash kandils hanging on the wall behind. The diyas light one by one when the layer starts, then burn continuously: flames flicker and lean as gentle breezes pass along the row, the toran sways, petals drift down, and everything reflects in the ledge. Projects a clean white band on the 1-inch mat.",
  "CREDIT": "Created for Ghost Arcade",
  "ISFVSN": "2",
  "CATEGORIES": ["Generator", "Audio Reactive", "Projection Mapping"],
  "INPUTS": [
    {"NAME": "colorMode", "TYPE": "long", "DEFAULT": 0, "VALUES": [0, 1, 2], "LABELS": ["Painted Clay", "Plain Terracotta", "Brass"], "LABEL": "Diya Style"},
    {"NAME": "speed", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.25, "MAX": 4.0, "LABEL": "Speed"},
    {"NAME": "diyaCount", "TYPE": "long", "DEFAULT": 5, "VALUES": [3, 5, 7], "LABELS": ["Three", "Five", "Seven"], "LABEL": "Number of Diyas"},
    {"NAME": "flameSize", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.5, "MAX": 1.7, "LABEL": "Flame Size"},
    {"NAME": "breeze", "TYPE": "float", "DEFAULT": 0.5, "MIN": 0.0, "MAX": 1.0, "LABEL": "Breeze"},
    {"NAME": "petalAmount", "TYPE": "float", "DEFAULT": 0.6, "MIN": 0.0, "MAX": 1.0, "LABEL": "Falling Petals"},
    {"NAME": "reflection", "TYPE": "float", "DEFAULT": 0.6, "MIN": 0.0, "MAX": 1.0, "LABEL": "Reflection"},
    {"NAME": "lanternCount", "TYPE": "long", "DEFAULT": 3, "VALUES": [0, 1, 2, 3], "LABELS": ["None", "One", "Two", "Three"], "LABEL": "Hanging Kandils"},
    {"NAME": "lanternGlow", "TYPE": "float", "DEFAULT": 0.7, "MIN": 0.0, "MAX": 1.0, "LABEL": "Kandil Glow"},
    {"NAME": "bgBrightness", "TYPE": "float", "DEFAULT": 0.5, "MIN": 0.0, "MAX": 1.0, "LABEL": "Background Brightness"},
    {"NAME": "lightUp", "TYPE": "bool", "DEFAULT": true, "LABEL": "Light Diyas One by One at Start"},
    {"NAME": "audioDrive", "TYPE": "float", "DEFAULT": 0.5, "MIN": 0.0, "MAX": 1.0, "LABEL": "Audio Drive"},
    {"NAME": "matBrightness", "TYPE": "float", "DEFAULT": 0.85, "MIN": 0.0, "MAX": 1.0, "LABEL": "Mat Brightness"},
    {"NAME": "matWarmth", "TYPE": "float", "DEFAULT": 0.25, "MIN": 0.0, "MAX": 1.0, "LABEL": "Mat Warmth"},
    {"NAME": "outerTrim", "TYPE": "float", "DEFAULT": 0.0, "MIN": 0.0, "MAX": 0.5, "LABEL": "Mat Outer Trim (in)"},
    {"NAME": "innerTrim", "TYPE": "float", "DEFAULT": 0.3, "MIN": -0.3, "MAX": 0.3, "LABEL": "Mat Inner Edge (in)"},
    {"NAME": "calibrate", "TYPE": "bool", "DEFAULT": false, "LABEL": "Calibration Grid"},
    {"NAME": "exposure", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.2, "MAX": 2.0, "LABEL": "Exposure"}
  ]
}*/

#ifdef GL_ES
precision highp float;
#endif

const float GUST_PERIOD = 10.0;

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

float sq(float x) {
  return x * x;
}

float lineMask(float d, float w, float aa) {
  return 1.0 - smoothstep(w, w + aa, d);
}

// A breeze that travels left to right along the row every GUST_PERIOD seconds.
float gustAt(float x, float tm) {
  float w = fract(tm / GUST_PERIOD) * GUST_PERIOD;
  float arrive = 3.0 + (x + 0.7) * 2.2;
  float g = exp(-sq((w - arrive) / 0.9));
  float w2 = w + GUST_PERIOD;
  g += exp(-sq((w2 - arrive) / 0.9));
  return g;
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

vec3 bowlColor(int style, float h) {
  if (style == 1) return vec3(0.62, 0.26, 0.10);
  if (style == 2) return vec3(0.85, 0.60, 0.22);
  return mix(vec3(0.72, 0.20, 0.10), vec3(0.62, 0.28, 0.08), h);
}

// One diya in local units (bowl half-width 0.11). Returns colour and coverage.
vec4 diya(vec2 q, float t, float id, float lit, float lean, float fScale, float bass, float pxl, int style) {
  vec3 col = vec3(0.0);
  float cov = 0.0;

  // bowl, with a pointed spout on the right
  float spout = max(q.x - 0.06, 0.0) * 0.55;
  float bd = max(length(vec2(q.x, (q.y + spout * 0.4) * 1.55)) - 0.11 - spout * 0.25, q.y - spout * 0.35);
  float bowl = 1.0 - smoothstep(-pxl, pxl, bd);
  float lip = length(vec2(q.x / 0.108, q.y / 0.017)) - 1.0;
  float lipM = 1.0 - smoothstep(-0.06, 0.06, lip);
  float h = hash21(vec2(id, 4.0));
  vec3 clay = bowlColor(style, h);
  float shade = 0.45 + 0.55 * smoothstep(-0.11, 0.07, q.x + 0.03);
  float topLight = 0.6 + 0.9 * exp(min(q.y, 0.0) * 22.0) * (0.35 + 0.65 * lit);
  vec3 bc = clay * shade * topLight;
  if (style == 0) {
    float band = lineMask(abs(q.y + 0.034), 0.009, 0.004);
    float dots = lineMask(length(vec2(fract(q.x * 28.0) - 0.5, (q.y + 0.034) * 28.0)), 0.22, 0.12);
    float tri = step(fract(q.x * 14.0 + 0.5), 0.5 - (q.y + 0.062) * 14.0) * step(-0.062, q.y) * step(q.y, -0.047);
    vec3 paintA = h > 0.5 ? vec3(1.0, 0.82, 0.25) : vec3(0.95, 0.95, 0.85);
    bc = mix(bc, paintA * topLight * 0.8, band * 0.85);
    bc = mix(bc, vec3(0.15, 0.55, 0.30) * topLight, dots * band);
    bc = mix(bc, vec3(1.0, 0.82, 0.25) * topLight * 0.7, tri * 0.7);
  }
  if (style == 2) {
    float spec = exp(-sq((q.x + 0.04) / 0.02)) * (1.0 - smoothstep(-0.05, 0.0, -q.y - 0.0));
    bc += vec3(1.0, 0.85, 0.5) * spec * 0.6;
  }
  col = bc;
  cov = bowl;
  vec3 oil = mix(vec3(0.25, 0.12, 0.04), vec3(1.0, 0.75, 0.32), 0.15 + 0.7 * lit);
  col = mix(col, oil * (0.7 + 0.3 * shade), lipM);
  cov = max(cov, lipM);

  // wick
  vec2 wq = q - vec2(0.045, 0.004);
  float wick = lineMask(length(vec2(wq.x - wq.y * 0.4, max(abs(wq.y) - 0.008, 0.0))), 0.0035, pxl);
  col = mix(col, mix(vec3(0.15, 0.1, 0.08), vec3(1.0, 0.6, 0.3), lit), wick);
  cov = max(cov, wick);

  // flame: blue root, white core, yellow body, orange rim, flickering tip that leans in the breeze
  vec2 base = vec2(0.05, 0.012);
  float wob = sin(t * 11.0 + id * 1.7) * sin(t * 7.3 + id * 3.1);
  float fh = 0.12 * fScale * (1.0 + bass * 0.35) * (1.0 + wob * 0.07) * (0.6 + 0.4 * lit);
  vec2 fq = q - base;
  float yn = clamp(fq.y / fh, 0.0, 1.2);
  float turb = (vnoise(vec2(fq.y * 40.0 - t * 9.0, id * 7.0)) - 0.5) * 0.012 * yn * yn;
  float leanS = lean / (1.0 + 0.6 * abs(lean));
  fq.x -= (leanS * 0.065 + wob * 0.004) * yn * yn + turb;
  float taper = 1.0 - clamp((fq.y - fh * 0.3) / (fh * 0.7), 0.0, 1.0) * 0.85;
  float wid = 0.026 * fScale * max(taper, 0.12);
  float fd = length(vec2(fq.x / wid, (fq.y - fh * 0.32) / (fh * 0.66))) - 1.0;
  float body = (1.0 - smoothstep(-0.12, 0.10, fd)) * lit;
  float core = (1.0 - smoothstep(-0.85, -0.35, fd)) * lit;
  float midL = (1.0 - smoothstep(-0.5, 0.0, fd)) * lit;
  float root = (1.0 - smoothstep(-0.02, 0.02, fq.y - fh * 0.12)) * (1.0 - smoothstep(-0.6, -0.1, fd)) * lit;
  vec3 fc = vec3(1.0, 0.42, 0.06);
  fc = mix(fc, vec3(1.0, 0.82, 0.30), midL);
  fc = mix(fc, vec3(1.0, 0.98, 0.90), core);
  fc = mix(fc, vec3(0.40, 0.55, 1.0), root * 0.45);
  col = mix(col, fc * (1.3 + wob * 0.1), body);
  cov = max(cov, body);
  return vec4(col, cov);
}

// Toran across the top: mango leaves and swaying marigold strings on a rope.
vec4 toran(vec2 u, float asp, float t, float swingAmt, float lightLvl, float pxu) {
  vec3 col = vec3(0.0);
  float cov = 0.0;
  float ropeY = 0.44;
  float xr = u.x / (asp * 0.5);
  float rope = lineMask(abs(u.y - (ropeY - 0.02 * (1.0 - xr * xr))), 0.0025, pxu);
  col = mix(col, vec3(0.55, 0.35, 0.15) * lightLvl, rope);
  cov = max(cov, rope);

  // mango leaves hanging from the rope
  float lsp = 0.085;
  float li = floor(u.x / lsp);
  for (int j = -1; j <= 1; j++) {
    float idx = li + float(j);
    float lx = (idx + 0.5) * lsp;
    float lxr = lx / (asp * 0.5);
    float ly = ropeY - 0.02 * (1.0 - lxr * lxr);
    float ang = sin(t * 1.2 + idx * 0.9) * 0.05 * (0.4 + swingAmt * 3.0) + (hash21(vec2(idx, 2.0)) - 0.5) * 0.2;
    vec2 q = u - vec2(lx, ly);
    float cs = cos(ang);
    float sn = sin(ang);
    q = vec2(cs * q.x + sn * q.y, -sn * q.x + cs * q.y);
    float along = clamp(-q.y / 0.095, 0.0, 1.0);
    float halfW = 0.02 * sin(3.14159 * pow(along, 0.8)) + 0.0005;
    float leaf = step(-0.095, q.y) * step(q.y, 0.0) * (1.0 - smoothstep(halfW - pxu, halfW + pxu, abs(q.x)));
    float rib = lineMask(abs(q.x), 0.0012, pxu) * leaf;
    vec3 lc = mix(vec3(0.10, 0.42, 0.12), vec3(0.22, 0.58, 0.18), along) * (0.85 + 0.15 * sign(q.x));
    lc = mix(lc, vec3(0.55, 0.75, 0.30), rib * 0.6);
    col = mix(col, lc * lightLvl, leaf);
    cov = max(cov, leaf);
  }

  // strings of marigolds between every second pair of leaves
  float msp = 0.17;
  float mi = floor(u.x / msp + 0.5);
  for (int j = -1; j <= 1; j++) {
    float idx = mi + float(j);
    float mx = idx * msp;
    float mxr = mx / (asp * 0.5);
    float my = ropeY - 0.02 * (1.0 - mxr * mxr);
    float ang = sin(t * 0.9 + idx * 1.3) * 0.05 * (0.4 + swingAmt * 3.5);
    vec2 dir = vec2(sin(ang), -cos(ang));
    float nBalls = 4.0 + mod(idx, 2.0);
    for (int b = 0; b < 5; b++) {
      float fb = float(b);
      if (fb >= nBalls) break;
      vec2 c = vec2(mx, my) + dir * (0.026 + fb * 0.032);
      float d = length(u - c);
      float rr = 0.0165;
      float ang2 = atan(u.y - c.y, u.x - c.x);
      float ruffle = 0.0015 * sin(ang2 * 9.0 + fb * 2.0);
      float ball = 1.0 - smoothstep(rr + ruffle - pxu, rr + ruffle + pxu, d);
      float petalTex = 0.75 + 0.25 * vnoise((u - c) * 600.0 + fb * 9.0);
      float shadeB = 0.65 + 0.35 * smoothstep(rr, -rr, d - 0.004 + (u.x - c.x) * 0.3);
      vec3 mc = mod(fb + idx, 2.0) < 0.5 ? vec3(1.0, 0.50, 0.04) : vec3(1.0, 0.76, 0.08);
      col = mix(col, mc * petalTex * shadeB * lightLvl * 1.2, ball);
      cov = max(cov, ball);
    }
  }
  return vec4(col, cov);
}

vec3 diyaArt(vec2 u, float asp, float pxu, float tm, float tReal, int style, int cnt, float fScale, float breezeAmt, float petals, float refl, float bgB, bool doLight, int kCount, float kGlow, float bass) {
  float n = float(cnt);
  float spacing = 1.1 / n;
  float s = min(spacing * 0.45 / 0.11, 1.2);
  float baseY = -0.15;
  float surfY = baseY - 0.07 * s;

  // which diya column this pixel is nearest to, and the light falling on wall and ledge
  float fi = floor((u.x / spacing) + n * 0.5);
  float lightSum = 0.0;
  for (int i = 0; i < 7; i++) {
    float f = float(i);
    if (f >= n) break;
    float dx = (f - (n - 1.0) * 0.5) * spacing;
    float litI = doLight ? smoothstep(1.0 + f * 1.1, 1.4 + f * 1.1, tReal) : 1.0;
    vec2 lp = vec2(dx + 0.05 * s, baseY + 0.06 * s);
    vec2 rel = u - lp;
    rel.y *= u.y < surfY ? 2.5 : 1.0;
    lightSum += litI * exp(-length(rel) * 5.0);
  }
  float flickAll = 0.92 + 0.08 * sin(tm * 9.0) * sin(tm * 6.1);
  vec3 warm = vec3(1.0, 0.62, 0.28);

  // wall
  float tex = 0.85 + 0.15 * vnoise(u * 28.0) + 0.1 * (vnoise(u * 5.0) - 0.5);
  vec3 wallAlb = vec3(0.42, 0.24, 0.15) * tex;
  vec3 col = wallAlb * (0.10 + 0.25 * bgB) + wallAlb * warm * lightSum * 0.9 * flickAll;

  // light the kandils cast on the wall behind them
  for (int k = 0; k < 3; k++) {
    float fk = float(k);
    if (fk >= float(kCount)) break;
    float kx = 0.0;
    float id = 1.0;
    if (kCount == 2) { kx = fk < 0.5 ? -0.40 : 0.40; id = fk * 2.0; }
    if (kCount == 3) { kx = (fk - 1.0) * 0.40; id = fk; }
    vec3 sideC = vec3(0.95, 0.12, 0.10);
    vec3 tbC = vec3(1.0, 0.85, 0.15);
    vec3 tasA = vec3(1.0, 0.72, 0.15);
    vec3 tasB = vec3(0.85, 0.15, 0.10);
    if (id > 0.5 && id < 1.5) { sideC = vec3(0.10, 0.62, 0.66); tbC = vec3(1.0, 0.58, 0.10); tasA = vec3(1.0, 0.55, 0.12); tasB = vec3(0.50, 0.15, 0.55); }
    if (id > 1.5) { sideC = vec3(0.72, 0.88, 0.45); tbC = vec3(0.98, 0.15, 0.50); tasA = vec3(0.95, 0.12, 0.45); tasB = vec3(1.0, 0.72, 0.20); }
    vec2 kc = vec2(kx, 0.20);
    col += mix(sideC, tbC, 0.4) * kGlow * exp(-length((u - kc) * vec2(1.0, 0.8)) * 7.0) * 0.45;
  }

  // polished ledge with reflections
  if (u.y < surfY) {
    vec3 ledgeAlb = vec3(0.30, 0.18, 0.12) * (0.9 + 0.1 * vnoise(u * vec2(60.0, 8.0)));
    col = ledgeAlb * (0.05 + 0.12 * bgB) + ledgeAlb * warm * lightSum * 1.1 * flickAll;
    float edge = lineMask(abs(u.y - surfY), 0.0018, pxu);
    col += vec3(1.0, 0.75, 0.4) * edge * (0.15 + 0.4 * lightSum);
    vec2 ru = vec2(u.x + sin(u.y * 120.0 + tm * 2.0) * 0.0015 * (surfY - u.y) * 20.0, 2.0 * surfY - u.y);
    float rfade = refl * 0.85 * (1.0 - smoothstep(0.0, 0.22, surfY - u.y));
    float rfi = floor((ru.x / spacing) + n * 0.5);
    for (int j = -1; j <= 1; j++) {
      float f = rfi + float(j);
      if (f < 0.0 || f >= n) continue;
      float dx = (f - (n - 1.0) * 0.5) * spacing;
      float litI = doLight ? smoothstep(1.0 + f * 1.1, 1.4 + f * 1.1, tReal) : 1.0;
      float lean = breezeAmt * (gustAt(dx, tm) + 0.35 * sin(tm * 0.7 + f));
      vec4 dy = diya((ru - vec2(dx, baseY)) / s, tm, f, litI, lean, fScale, bass, pxu / s, style);
      col = mix(col, dy.rgb, dy.a * rfade);
    }
    // petals scattered on the ledge
    vec2 pg = u * vec2(30.0, 45.0);
    vec2 pid = floor(pg);
    vec2 pf = fract(pg) - 0.5;
    float ph = hash21(pid + 11.0);
    vec2 po = vec2(hash21(pid + 3.0), hash21(pid + 6.0)) - 0.5;
    float pa = hash21(pid + 9.0) * 3.14;
    vec2 pq = pf - po * 0.4;
    pq = vec2(cos(pa) * pq.x + sin(pa) * pq.y, -sin(pa) * pq.x + cos(pa) * pq.y);
    float petal = (1.0 - smoothstep(0.85, 1.0, length(pq * vec2(2.4, 3.8)))) * step(1.0 - 0.18 * petals, ph) * step(u.y, surfY - 0.02);
    vec3 pc = ph > 0.94 ? vec3(0.85, 0.08, 0.18) : (ph > 0.88 ? vec3(1.0, 0.75, 0.10) : vec3(1.0, 0.45, 0.05));
    col = mix(col, pc * (0.25 + 0.9 * lightSum + 0.2 * bgB), petal * 0.9);
  }

  // diyas
  for (int j = -1; j <= 1; j++) {
    float f = fi + float(j);
    if (f < 0.0 || f >= n) continue;
    float dx = (f - (n - 1.0) * 0.5) * spacing;
    float litI = doLight ? smoothstep(1.0 + f * 1.1, 1.4 + f * 1.1, tReal) : 1.0;
    float flare = doLight ? exp(-sq((tReal - (1.2 + f * 1.1)) / 0.15)) : 0.0;
    float lean = breezeAmt * (gustAt(dx, tm) + 0.35 * sin(tm * 0.7 + f));
    vec4 dy = diya((u - vec2(dx, baseY)) / s, tm, f, litI, lean, fScale, bass, pxu / s, style);
    col = mix(col, dy.rgb, dy.a);
    vec2 fpos = vec2(dx + 0.05 * s, baseY + 0.06 * s);
    float halo = exp(-length(u - fpos) * 30.0);
    col += vec3(1.0, 0.7, 0.32) * halo * 0.45 * litI * flickAll;
    col += vec3(1.0, 0.85, 0.6) * exp(-length(u - fpos) * 45.0) * flare * 1.5;
  }

  // falling petals drifting down from the toran
  float nP = floor(petals * 40.0 + 0.5);
  for (int i = 0; i < 40; i++) {
    float f = float(i);
    if (f >= nP) break;
    float h0 = hash21(vec2(f, 1.9));
    float rate = 0.07 + 0.04 * h0;
    float cyc = tm * rate + h0;
    float k = mod(floor(cyc), 97.0);
    float ph = fract(cyc);
    float hx = hash21(vec2(f + 5.0, k));
    float y = mix(0.42, surfY + 0.005, ph);
    float x = (hx - 0.5) * asp * 0.9 + sin(tm * 1.3 + f * 2.0) * 0.03 + breezeAmt * gustAt(0.0, tm) * 0.03;
    float spin = tm * (2.0 + 2.0 * h0) + f;
    vec2 q = u - vec2(x, y);
    float ca = cos(spin * 0.5);
    float sa = sin(spin * 0.5);
    q = vec2(ca * q.x + sa * q.y, -sa * q.x + ca * q.y);
    q.x /= max(abs(cos(spin)), 0.25);
    float pd = length(q * vec2(1.0, 1.6));
    float psz = 0.011 + 0.006 * hash21(vec2(f, 7.7));
    float petal = 1.0 - smoothstep(psz - pxu, psz + pxu, pd);
    float fade = smoothstep(0.0, 0.05, ph) * (1.0 - smoothstep(0.92, 1.0, ph));
    vec3 pc = h0 > 0.66 ? vec3(0.9, 0.1, 0.2) : (h0 > 0.33 ? vec3(1.0, 0.75, 0.1) : vec3(1.0, 0.45, 0.05));
    float vein = 0.85 + 0.15 * sin(q.y * 900.0);
    col = mix(col, pc * vein * (0.6 + 0.3 * bgB + 0.8 * lightSum), petal * fade);
  }

  // toran on top
  float swing = breezeAmt * (gustAt(u.x, tm) + 0.3);
  vec4 tr = toran(u, asp, tm, swing, 0.35 + 0.6 * bgB + 0.3 * lightSum, pxu);
  col = mix(col, tr.rgb, tr.a);

  // fabric kandils hanging from the toran rope, in front of the wall
  for (int k = 0; k < 3; k++) {
    float fk = float(k);
    if (fk >= float(kCount)) break;
    float kx = 0.0;
    float id = 1.0;
    if (kCount == 2) { kx = fk < 0.5 ? -0.40 : 0.40; id = fk * 2.0; }
    if (kCount == 3) { kx = (fk - 1.0) * 0.40; id = fk; }
    vec3 sideC = vec3(0.95, 0.12, 0.10);
    vec3 tbC = vec3(1.0, 0.85, 0.15);
    vec3 tasA = vec3(1.0, 0.72, 0.15);
    vec3 tasB = vec3(0.85, 0.15, 0.10);
    if (id > 0.5 && id < 1.5) { sideC = vec3(0.10, 0.62, 0.66); tbC = vec3(1.0, 0.58, 0.10); tasA = vec3(1.0, 0.55, 0.12); tasB = vec3(0.50, 0.15, 0.55); }
    if (id > 1.5) { sideC = vec3(0.72, 0.88, 0.45); tbC = vec3(0.98, 0.15, 0.50); tasA = vec3(0.95, 0.12, 0.45); tasB = vec3(1.0, 0.72, 0.20); }
    float swingK = sin(tm * 0.8 + fk * 2.1) * 0.035 + breezeAmt * gustAt(kx, tm) * 0.07;
    vec4 kd = akashKandil(u, vec2(kx, 0.20), 0.085, 2.7, swingK, tm, id, sideC, tbC, tasA, tasB, 0.35 + 0.5 * bgB + lightSum * 0.6, kGlow, pxu);
    col = mix(col, kd.rgb, kd.a);
  }
  return col;
}

vec3 calibInner(vec2 u, float pxu, float inchH) {
  vec3 col = vec3(0.02);
  vec2 g = u * inchH;
  vec2 gd = 0.5 - abs(fract(g) - 0.5);
  float gl = max(lineMask(gd.x, 0.0, pxu * inchH * 1.5), lineMask(gd.y, 0.0, pxu * inchH * 1.5));
  col += vec3(0.35) * gl;
  float crossM = max(lineMask(abs(u.x), pxu, pxu), lineMask(abs(u.y), pxu, pxu));
  col = mix(col, vec3(1.0), crossM);
  return col;
}

void main() {
  vec2 uv = gl_FragCoord.xy / RENDERSIZE.xy;
  vec2 pin = (uv - 0.5) * vec2(12.0, 10.0);
  float px = max(12.0 / RENDERSIZE.x, 10.0 / RENDERSIZE.y);

  float tm = TIME * speed;
  float tReal = TIME;
  int style = int(colorMode);
  int cnt = int(diyaCount);
  float fScale = flameSize;
  float breezeAmt = breeze;
  float petals = petalAmount;
  float refl = reflection;
  float bgB = bgBrightness;
  bool doLight = lightUp;
  int kCount = int(lanternCount);
  float kGlow = lanternGlow;
  float drive = audioDrive;
  float mB = matBrightness;
  float mW = matWarmth;
  float oTrim = outerTrim;
  float iTrim = innerTrim;
  bool cal = calibrate;
  float ex = exposure;

  float bass = max(audioBass, 0.2 + 0.08 * sin(TIME * 0.7)) * drive;

  vec2 ho = vec2(6.0, 5.0) - vec2(oTrim);
  vec2 hi = vec2(5.0, 4.0) + vec2(iTrim);
  float dOut = max(abs(pin.x) - ho.x, abs(pin.y) - ho.y);
  float dIn = max(abs(pin.x) - hi.x, abs(pin.y) - hi.y);

  vec3 col = vec3(0.0);
  if (dIn < px) {
    vec2 u = pin / (2.0 * hi.y);
    float asp = hi.x / hi.y;
    float pxu = px / (2.0 * hi.y);
    if (cal) {
      col = calibInner(u, pxu, 2.0 * hi.y);
    } else {
      col = diyaArt(u, asp, pxu, tm, tReal, style, cnt, fScale, breezeAmt, petals, refl, bgB, doLight, kCount, kGlow, bass) * ex;
    }
  }

  vec3 matCol = mix(vec3(1.0), vec3(1.0, 0.92, 0.80), mW) * mB;
  float inMat = smoothstep(-px, px, dIn);
  float inFrame = 1.0 - smoothstep(-px, px, dOut);
  col = mix(col, matCol, inMat * inFrame);
  col *= inFrame;

  if (cal) {
    col = mix(col, vec3(1.0, 0.1, 0.1), lineMask(abs(dOut), px * 1.5, px));
    col = mix(col, vec3(0.1, 1.0, 0.2), lineMask(abs(dIn), px * 1.5, px));
  }

  gl_FragColor = vec4(clamp(col, 0.0, 1.0), 1.0);
}
