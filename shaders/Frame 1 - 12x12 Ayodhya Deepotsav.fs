/*{
  "DESCRIPTION": "12x12 frame (14x14 mount). Ayodhya Deepotsav: a riverside ghat at night with rows of diyas on every step, temples and chhatris outlined in lights, distant fireworks, and the whole scene reflected in the rippling Saryu, where floating diyas and lamp-lit boats drift by. The ghat lights up row by row when the layer starts, then glows continuously with waves of shimmer. Projects a clean white band on the mat.",
  "CREDIT": "Created for Ghost Arcade",
  "ISFVSN": "2",
  "CATEGORIES": ["Generator", "Audio Reactive", "Projection Mapping"],
  "INPUTS": [
    {"NAME": "colorMode", "TYPE": "long", "DEFAULT": 0, "VALUES": [0, 1, 2], "LABELS": ["Warm Gold", "Saffron", "Festive Colours"], "LABEL": "Temple Lights"},
    {"NAME": "speed", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.25, "MAX": 3.0, "LABEL": "Speed"},
    {"NAME": "waveAmount", "TYPE": "float", "DEFAULT": 0.6, "MIN": 0.0, "MAX": 1.0, "LABEL": "Diya Shimmer Waves"},
    {"NAME": "floatCount", "TYPE": "float", "DEFAULT": 0.7, "MIN": 0.0, "MAX": 1.0, "LABEL": "Floating Diyas"},
    {"NAME": "boatCount", "TYPE": "long", "DEFAULT": 2, "VALUES": [0, 1, 2], "LABELS": ["No Boats", "One Boat", "Two Boats"], "LABEL": "Boats"},
    {"NAME": "fireworkAmount", "TYPE": "float", "DEFAULT": 0.5, "MIN": 0.0, "MAX": 1.0, "LABEL": "Distant Fireworks"},
    {"NAME": "reflection", "TYPE": "float", "DEFAULT": 0.7, "MIN": 0.0, "MAX": 1.0, "LABEL": "River Reflection"},
    {"NAME": "bgBrightness", "TYPE": "float", "DEFAULT": 0.5, "MIN": 0.0, "MAX": 1.0, "LABEL": "Sky and Stone Brightness"},
    {"NAME": "lightUp", "TYPE": "bool", "DEFAULT": true, "LABEL": "Light the Ghat Row by Row at Start"},
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

const float TAU = 6.28318530718;
const float PI = 3.14159265359;
const float WL = -0.14;
const float GHAT_TOP = 0.10;
const float NSTEPS = 9.0;

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

vec3 multiColor(float i) {
  float k = mod(i, 5.0);
  if (k < 0.5) return vec3(1.0, 0.35, 0.30);
  if (k < 1.5) return vec3(1.0, 0.80, 0.25);
  if (k < 2.5) return vec3(0.40, 1.0, 0.45);
  if (k < 3.5) return vec3(0.45, 0.65, 1.0);
  return vec3(1.0, 0.45, 0.90);
}

vec3 templeLight(int pal, float idx) {
  if (pal == 1) return vec3(1.0, 0.58, 0.18);
  if (pal == 2) return multiColor(idx);
  return vec3(1.0, 0.84, 0.52);
}

// A dotted string of lights following an edge. e = distance to the edge, along = position along it.
float edgeLights(float e, float along, float spacing, float px) {
  float onEdge = lineMask(abs(e), 0.0025, px);
  float dotsA = lineMask(abs(fract(along / spacing) - 0.5) * spacing, 0.0018, px);
  return onEdge * dotsA;
}

// Temples, chhatris and the palace wall above the ghat. Returns colour (lights included) and coverage.
vec4 temples(vec2 p, float tm, int pal, float bgB, float px) {
  vec3 col = vec3(0.0);
  float cov = 0.0;
  float lights = 0.0;
  vec3 lightCol = vec3(0.0);
  vec3 stoneLit = vec3(0.10, 0.065, 0.05) * (0.4 + 0.9 * bgB);
  float warmUp = exp(-max(p.y - GHAT_TOP, 0.0) * 14.0);
  vec3 stone = stoneLit + vec3(0.30, 0.16, 0.08) * warmUp * 0.6;

  // palace wall with arched windows along the top of the ghat
  float wallTop = GHAT_TOP + 0.055;
  if (p.y > GHAT_TOP - 0.002 && p.y < wallTop) {
    cov = 1.0;
    col = stone;
    float ax = fract(p.x / 0.05) - 0.5;
    float ay = p.y - (GHAT_TOP + 0.008);
    float arch = step(abs(ax), 0.17) * step(0.0, ay) * step(ay, 0.026 + 0.012 * sqrt(max(1.0 - (ax / 0.17) * (ax / 0.17), 0.0)));
    col = mix(col, vec3(1.0, 0.62, 0.28) * (0.55 + 0.15 * sin(tm * 3.0 + floor(p.x / 0.05))), arch);
    float l = edgeLights(p.y - wallTop + 0.003, p.x, 0.012, px);
    lights = max(lights, l);
    lightCol = templeLight(pal, floor(p.x / 0.012));
  }

  // shikhara temples: one large in the centre and two smaller ones
  for (int k = 0; k < 3; k++) {
    float fk = float(k);
    float x0 = (fk - 1.0) * 0.30;
    float big = k == 1 ? 1.0 : 0.0;
    float base = wallTop;
    float hgt = mix(0.15, 0.24, big);
    float wid = mix(0.045, 0.07, big);
    float yn = (p.y - base) / hgt;
    if (yn > -0.02 && yn < 1.15) {
      float w = wid * pow(max(1.0 - yn, 0.0), 0.7) * (1.0 + 0.06 * sin(yn * 42.0));
      float dx = abs(p.x - x0);
      float body = step(0.0, yn) * step(yn, 1.0) * (1.0 - smoothstep(w - px, w + px, dx));
      float porch = step(0.0, yn) * step(yn, 0.18) * step(dx, wid * 1.35);
      float topY = base + hgt;
      float amalaka = lineMask(length((p - vec2(x0, topY + 0.004)) * vec2(1.0, 2.2)), wid * 0.18, px);
      float kalash = lineMask(length(p - vec2(x0, topY + 0.017)), wid * 0.08 + 0.002, px);
      float pole = lineMask(abs(p.x - x0), 0.0012, px) * step(topY, p.y) * step(p.y, topY + 0.05);
      float fy = p.y - (topY + 0.042);
      float fxl = (p.x - x0) / 0.03;
      float wave = sin(fxl * 6.0 - tm * 3.0) * 0.003 * fxl;
      float flag = step(0.0, fxl) * step(fxl, 1.0) * step(abs(fy - wave), 0.009 * (1.0 - fxl));
      float shape = max(max(body, porch), max(max(amalaka, kalash), pole));
      col = mix(col, stone * (0.8 + 0.4 * (1.0 - yn)), shape);
      col = mix(col, vec3(1.0, 0.45, 0.08) * 0.8, flag);
      cov = max(cov, max(shape, flag));
      float le = edgeLights(w - dx, p.y, 0.009, px) * step(0.0, yn) * step(yn, 1.0);
      float tier = 0.0;
      for (int t = 1; t < 4; t++) {
        float ty = float(t) * 0.25;
        tier = max(tier, edgeLights(yn - ty, p.x, 0.009, px * 4.0) * step(dx, w));
      }
      float lp = max(le, max(tier, edgeLights(yn - 0.18, p.x, 0.009, px * 4.0) * step(dx, wid * 1.35)));
      if (lp > lights) {
        lights = lp;
        lightCol = templeLight(pal, floor(p.y / 0.009) + fk * 2.0);
      }
    }
  }

  // chhatris: domed pavilions between the temples
  for (int k = 0; k < 4; k++) {
    float fk = float(k);
    float x0 = (fk - 1.5) * 0.30;
    float rr = 0.032;
    float base = wallTop;
    vec2 q = p - vec2(x0, base);
    float pillars = step(0.0, q.y) * step(q.y, 0.035) * step(abs(q.x), rr) * (1.0 - step(abs(fract(q.x / (rr * 0.66) + 0.5) - 0.5), 0.3) * step(0.006, q.y) * step(q.y, 0.03));
    float dome = (1.0 - smoothstep(rr - px, rr + px, length(vec2(q.x, (q.y - 0.035) * 1.15)))) * step(0.035, q.y);
    float finial = lineMask(length(q - vec2(0.0, 0.035 + rr / 1.15 + 0.006)), 0.004, px);
    float shape = max(max(pillars, dome), finial);
    col = mix(col, stone * 0.9, shape);
    cov = max(cov, shape);
    float an = atan(q.y - 0.035, q.x);
    float dl = edgeLights(rr - length(vec2(q.x, (q.y - 0.035) * 1.15)), an * rr, 0.009, px) * step(0.035, q.y);
    float inner = step(abs(q.x), rr * 0.95) * step(0.006, q.y) * step(q.y, 0.03) * (1.0 - pillars);
    col += vec3(1.0, 0.6, 0.25) * inner * 0.35;
    if (dl > lights) {
      lights = dl;
      lightCol = templeLight(pal, floor(an * 6.0) + fk);
    }
  }

  float twinkle = 0.85 + 0.15 * sin(tm * 2.0 + p.x * 40.0 + p.y * 30.0);
  col = mix(col, lightCol * 1.3 * twinkle, lights);
  cov = max(cov, lights);
  return vec4(col, cov);
}

// Step layout with perspective: steps near the water look taller, distant ones shorter.
float stepY(float r) {
  return WL + pow(clamp(r / NSTEPS, 0.0, 1.0), 0.625) * (GHAT_TOP - WL);
}

// The ghat: stone steps with a row of diyas on each step, closer rows larger.
vec3 ghat(vec2 p, float tm, float tReal, float waveAmt, bool doLight, float bgB, float bass, float px) {
  float sN = clamp((p.y - WL) / (GHAT_TOP - WL), 0.0, 1.0);
  float wv = pow(sN, 1.6) * NSTEPS;
  float row = floor(wv);
  float fy = fract(wv);
  float tex = 0.85 + 0.15 * vnoise(p * vec2(40.0, 120.0)) + 0.08 * (vnoise(p * 9.0) - 0.5);
  vec3 tread = vec3(0.50, 0.35, 0.22) * tex;
  vec3 riser = vec3(0.26, 0.16, 0.10) * tex;
  vec3 alb = mix(riser, tread, smoothstep(0.36, 0.44, fy));
  float glow = 0.0;
  vec3 col = vec3(0.0);
  for (int j = -1; j <= 1; j++) {
    float r = row + float(j);
    if (r < 0.0 || r > NSTEPS - 1.0) continue;
    float y0 = stepY(r);
    float y1 = stepY(r + 1.0);
    float hR = y1 - y0;
    float scl = hR / ((GHAT_TOP - WL) / NSTEPS);
    float yd = y0 + 0.4 * hR;
    float sx = 0.017 * scl;
    float off = mod(r, 2.0) * 0.5 * sx;
    float rowLit = doLight ? smoothstep(1.0 + r * 0.7 + (p.x + 0.5) * 0.8, 1.3 + r * 0.7 + (p.x + 0.5) * 0.8, tReal) : 1.0;
    glow += rowLit * exp(-abs(p.y - (yd + 0.006 * scl)) * 70.0 / scl) * scl;
    float xi = floor((p.x - off) / sx + 0.5);
    for (int k = -1; k <= 1; k++) {
      float xk = xi + float(k);
      float hh0 = hash21(vec2(xk + 7.0, r * 3.0));
      float dxp = xk * sx + off + (hh0 - 0.5) * 0.3 * sx;
      float lit = doLight ? smoothstep(1.0 + r * 0.7 + (dxp + 0.5) * 0.8, 1.3 + r * 0.7 + (dxp + 0.5) * 0.8, tReal) : 1.0;
      float hh = hash21(vec2(xk, r));
      float flick = 0.8 + 0.2 * sin(tm * (9.0 + 4.0 * hh) + hh * 30.0) * sin(tm * 5.7 + hh * 11.0);
      float wave = waveAmt * pow(0.5 + 0.5 * sin(dxp * 7.0 - tm * 1.3 + r * 0.6), 6.0);
      float b = lit * (flick + 1.4 * wave) * (1.0 + bass * 0.3) * step(0.04, hh0);
      float fs = 0.8 + 0.4 * hash21(vec2(xk, r + 9.0));
      vec2 q = (p - vec2(dxp, yd + (hh - 0.5) * 0.0012 * scl)) / scl;
      float cup = (1.0 - smoothstep(0.0, px * 1.5 / scl, length(vec2(q.x / 0.0055, min(q.y, 0.0) / 0.0028)) - 1.0)) * step(q.y, 0.0005);
      alb = mix(alb, vec3(0.45, 0.17, 0.07), cup);
      vec2 fq = q - vec2(0.0, 0.0045);
      float fd = length(vec2(fq.x / (0.0022 * fs), fq.y / (0.0048 * fs)));
      float flame = (1.0 - smoothstep(0.6, 1.1, fd)) * b;
      col += mix(vec3(1.0, 0.55, 0.15), vec3(1.0, 0.95, 0.80), 1.0 - smoothstep(0.0, 0.7, fd)) * flame * 1.4;
      col += vec3(1.0, 0.65, 0.30) * exp(-length(fq) / 0.006) * (0.35 + 0.5 * wave) * b;
    }
  }
  vec3 amb = vec3(0.05, 0.035, 0.03) * (0.4 + 1.0 * bgB);
  col += alb * (amb + vec3(1.0, 0.62, 0.30) * glow * 0.7);
  return col;
}

// Deep stambh: tall lamp towers standing on the ghat, with tiers of small lamps.
vec4 lampTowers(vec2 p, float tm, float tReal, bool doLight, float px) {
  vec3 col = vec3(0.0);
  float cov = 0.0;
  for (int k = 0; k < 2; k++) {
    float x0 = k == 0 ? -0.15 : 0.15;
    float yb = WL + 0.02;
    float yt = GHAT_TOP - 0.01;
    float yn = (p.y - yb) / (yt - yb);
    float w = mix(0.012, 0.006, clamp(yn, 0.0, 1.0));
    float dx = abs(p.x - x0);
    float body = step(0.0, yn) * step(yn, 1.0) * (1.0 - smoothstep(w - px, w + px, dx));
    float lit = doLight ? smoothstep(6.5, 7.5, tReal) : 1.0;
    col = mix(col, vec3(0.28, 0.17, 0.10) * (0.7 + 0.6 * yn), body);
    cov = max(cov, body);
    float tierN = 12.0;
    float ti = floor(yn * tierN + 0.5);
    float ty = yb + ti / tierN * (yt - yb);
    float tw = mix(0.012, 0.006, ti / tierN) + 0.007;
    for (int sgn = 0; sgn < 2; sgn++) {
      float sx = sgn == 0 ? -1.0 : 1.0;
      vec2 lp = vec2(x0 + sx * tw, ty + 0.004);
      float fl = 0.85 + 0.15 * sin(tm * 9.0 + ti * 1.7 + sx * 2.0);
      float d = length((p - lp) * vec2(1.0, 0.65));
      col += vec3(1.0, 0.85, 0.5) * lineMask(d, 0.0024, px) * 1.3 * fl * lit * step(0.0, ti) * step(ti, tierN);
      col += vec3(1.0, 0.6, 0.25) * exp(-d / 0.006) * 0.3 * fl * lit * step(0.0, ti) * step(ti, tierN);
    }
    vec2 topF = vec2(x0, yt + 0.012);
    float fd = length(vec2((p.x - topF.x) / 0.004, (p.y - topF.y) / 0.009));
    col += mix(vec3(1.0, 0.55, 0.15), vec3(1.0, 0.95, 0.8), 1.0 - smoothstep(0.0, 0.7, fd)) * (1.0 - smoothstep(0.6, 1.1, fd)) * 1.5 * lit;
    col += vec3(1.0, 0.65, 0.3) * exp(-length(p - topF) / 0.015) * 0.4 * lit;
    cov = max(cov, (1.0 - smoothstep(0.6, 1.1, fd)) * lit);
  }
  return vec4(col, cov);
}

vec3 nightSky(vec2 p, float tm, float bgB, bool detail, float px) {
  float hgt = clamp((p.y - GHAT_TOP) / 0.4, 0.0, 1.0);
  vec3 col = mix(vec3(0.10, 0.045, 0.035), vec3(0.008, 0.010, 0.035), smoothstep(0.0, 0.8, hgt)) * (0.5 + 0.9 * bgB);
  if (detail) {
    vec2 sq = p * 70.0;
    float sh = hash21(floor(sq) + 21.0);
    float star = step(0.975, sh) * lineMask(length(fract(sq) - 0.5), 0.06, 0.1);
    col += vec3(0.9, 0.92, 1.0) * star * (0.5 + 0.5 * sin(tm * 1.5 + sh * 50.0)) * 0.4 * smoothstep(0.3, 0.7, hgt);
  }
  return col;
}

// Small, distant fireworks above the temples, firing continuously in staggered lanes.
vec3 distantFireworks(vec2 p, float tm, int pal, float amt, float px) {
  vec3 col = vec3(0.0);
  if (amt < 0.01) return col;
  float lanes = 1.0 + floor(amt * 3.0 + 0.5);
  for (int i = 0; i < 4; i++) {
    float fi = float(i);
    if (fi >= lanes) break;
    float period = 4.5;
    float lt = tm + fi / lanes * period;
    float k = floor(lt / period);
    float age = fract(lt / period) * period;
    float hA = hash21(vec2(fi, k + 1.3));
    float hB = hash21(vec2(fi + 4.0, k + 2.9));
    float hC = hash21(vec2(fi + 8.0, k + 5.1));
    vec2 c = vec2((hA - 0.5) * 0.85, 0.30 + hB * 0.14);
    float rise = 0.9;
    if (age < rise) {
      float e = age / rise;
      vec2 rp = vec2(c.x, mix(GHAT_TOP + 0.1, c.y, e * (2.0 - e)));
      col += vec3(1.0, 0.8, 0.5) * 0.0025 / (length(p - rp) + 0.0025) * 0.3 * (1.0 - smoothstep(0.0, 0.02, length(p - rp)));
      continue;
    }
    float b = age - rise;
    float reach = 0.075 * (0.7 + 0.5 * hC) * (1.0 - exp(-3.2 * b));
    vec2 d = p - c + vec2(0.0, 0.01 * b * b);
    if (length(d) > reach + 0.03) continue;
    float a = atan(d.y, d.x);
    float n = 26.0;
    float sector = floor((a / TAU + 0.5) * n);
    float sa = (sector + 0.5) / n * TAU - PI;
    vec2 head = vec2(cos(sa), sin(sa)) * reach * (0.85 + 0.15 * hash21(vec2(sector, k)));
    float dh = length(d - head);
    vec3 fc = multiColor(floor(hC * 5.0));
    if (pal == 0) fc = mix(fc, vec3(1.0, 0.8, 0.45), 0.5);
    float life = 1.0 - smoothstep(1.6, 3.4, b);
    col += mix(fc, vec3(1.0), 0.3) * (lineMask(dh, max(0.0018, px), px) + 0.0015 / (dh + 0.0015) * 0.15 * (1.0 - smoothstep(0.0, 0.02, dh))) * life;
  }
  return col * (0.5 + 0.5 * amt);
}

// Everything above the water line.
vec3 aboveWater(vec2 p, float tm, float tReal, int pal, float waveAmt, float fwAmt, float bgB, bool doLight, bool detail, float bass, float px) {
  vec3 col = nightSky(p, tm, bgB, detail, px);
  col += distantFireworks(p, tm, pal, fwAmt, px);
  if (p.y < GHAT_TOP + 0.002) {
    col = ghat(p, tm, tReal, waveAmt, doLight, bgB, bass, px);
  }
  vec4 tw = lampTowers(p, tm, tReal, doLight, px);
  col = mix(col, tw.rgb, tw.a) + tw.rgb * (1.0 - tw.a) * 0.6;
  vec4 tp = temples(p, tm, pal, bgB, px);
  col = mix(col, tp.rgb, tp.a);
  return col;
}

vec4 boat(vec2 p, vec2 c, float sc, float tm, float id, float px, out vec3 glowOut) {
  glowOut = vec3(0.0);
  vec2 q = (p - c) / sc;
  float L = 0.08;
  float hgt = 0.016;
  float topY = 0.004 * (q.x / L) * (q.x / L) * 3.0;
  float yn = clamp((q.y + hgt) / hgt, 0.0, 1.0);
  float halfW = L * (0.72 + 0.28 * yn);
  float hull = step(-hgt, q.y) * step(q.y, topY) * (1.0 - smoothstep(halfW - px / sc, halfW + px / sc, abs(q.x)));
  vec3 col = vec3(0.12, 0.07, 0.04) + vec3(0.25, 0.13, 0.05) * yn;
  float rim = lineMask(abs(q.y - topY), 0.0015, px / sc) * step(abs(q.x), L);
  col = mix(col, vec3(0.55, 0.30, 0.12), rim);
  float pole = lineMask(abs(q.x + 0.045), 0.0012, px / sc) * step(topY, q.y) * step(q.y, 0.045);
  vec2 lan = q - vec2(-0.045, 0.040);
  float lantern = lineMask(length(lan * vec2(1.0, 0.8)), 0.0055, px / sc);
  float fl = 0.85 + 0.15 * sin(tm * 8.0 + id * 3.0);
  col = mix(col, vec3(0.08, 0.05, 0.03), pole * (1.0 - hull));
  col = mix(col, vec3(1.0, 0.75, 0.35) * 1.3 * fl, lantern);
  float cov = max(max(hull, pole), lantern);
  float dl = 0.0;
  for (int i = 0; i < 4; i++) {
    float dx = -0.01 + float(i) * 0.02;
    vec2 dq = q - vec2(dx, topY + 0.004);
    float flame = lineMask(length(dq * vec2(1.0, 0.6)), 0.0025, px / sc);
    col = mix(col, vec3(1.0, 0.85, 0.5) * 1.3, flame);
    cov = max(cov, flame);
    dl += exp(-length(dq) * 120.0);
  }
  glowOut = vec3(1.0, 0.65, 0.3) * (exp(-length(lan) * 70.0) * 0.5 * fl + dl * 0.2);
  return vec4(col, cov);
}

vec3 river(vec2 p, float tm, float tReal, int pal, float waveAmt, float fwAmt, float bgB, bool doLight, float refl, float floatAmt, int boats, float bass, float px) {
  float depth = WL - p.y;
  vec3 col = mix(vec3(0.03, 0.025, 0.04), vec3(0.008, 0.010, 0.025), smoothstep(0.0, 0.3, depth)) * (0.5 + 0.8 * bgB);
  float flowT = tm * 0.35;
  float n1 = vnoise(vec2(p.x * 8.0 + 3.0, p.y * 55.0 + flowT * 4.0));
  float n2 = vnoise(vec2(p.x * 20.0 - 7.0, p.y * 120.0 - flowT * 7.0));
  float rip = ((n1 - 0.5) * 0.009 + (n2 - 0.5) * 0.003) * (0.35 + depth * 3.0);
  vec2 rp = vec2(p.x + rip, WL + depth * 0.85);
  float stretch = 0.006 + depth * 0.03;
  vec3 r1 = aboveWater(rp, tm, tReal, pal, waveAmt, fwAmt, bgB, doLight, false, bass, px);
  vec3 r2 = aboveWater(rp + vec2(rip * 0.4, stretch), tm, tReal, pal, waveAmt, fwAmt, bgB, doLight, false, bass, px);
  vec3 r3 = aboveWater(rp + vec2(-rip * 0.3, -stretch * 0.8), tm, tReal, pal, waveAmt, fwAmt, bgB, doLight, false, bass, px);
  float glint = 0.8 + 0.2 * smoothstep(0.5, 0.8, n2);
  vec3 rc = (max(max(r1, r2), r3) * 0.55 + (r1 + r2 + r3) * 0.15) * glint;
  float fade = refl * (0.75 - 0.45 * smoothstep(0.0, 0.35, depth));
  col += rc * fade;
  col += vec3(1.0, 0.75, 0.45) * lineMask(abs(p.y - WL), 0.0015, px) * 0.25;

  // floating diyas drifting with the current
  float nF = floor(floatAmt * 24.0 + 0.5);
  for (int i = 0; i < 24; i++) {
    float fi = float(i);
    if (fi >= nF) break;
    float h1 = hash21(vec2(fi, 2.2));
    float h2 = hash21(vec2(fi, 5.5));
    float h3 = hash21(vec2(fi, 8.8));
    float y = mix(WL - 0.025, -0.47, h1);
    float near = (WL - y) / (WL + 0.47);
    float sc = mix(0.8, 2.1, near);
    float v = (0.006 + 0.008 * h2) * (0.6 + 0.6 * near);
    float x = fract(h3 + v * tm / 1.16) * 1.16 - 0.58;
    y += 0.0015 * sin(tm * 1.4 + fi * 2.0);
    vec2 q = p - vec2(x, y);
    if (abs(q.x) > 0.06 * sc || abs(q.y) > 0.08 * sc) continue;
    float leaf = 1.0 - smoothstep(1.0 - px * 80.0, 1.0 + px * 80.0, length(vec2(q.x / (0.012 * sc), q.y / (0.004 * sc))));
    float rimDots = lineMask(abs(length(vec2(q.x / (0.012 * sc), q.y / (0.004 * sc))) - 0.85), 0.12, 0.1) * step(0.5, fract(atan(q.y, q.x) / TAU * 10.0));
    col = mix(col, vec3(0.10, 0.18, 0.06), leaf);
    col = mix(col, vec3(1.0, 0.5, 0.06) * 0.8, rimDots * leaf);
    float fl = 0.85 + 0.15 * sin(tm * 10.0 + fi * 3.0) * sin(tm * 6.0 + fi);
    vec2 fq = q - vec2(0.0, 0.006 * sc);
    float fd = length(vec2(fq.x / (0.0025 * sc), fq.y / (0.0055 * sc)));
    col += mix(vec3(1.0, 0.55, 0.15), vec3(1.0, 0.95, 0.8), 1.0 - smoothstep(0.0, 0.7, fd)) * (1.0 - smoothstep(0.6, 1.1, fd)) * fl * 1.4;
    col += vec3(1.0, 0.65, 0.3) * exp(-length(fq) / (0.008 * sc)) * 0.4 * fl;
    vec2 rq = q - vec2(0.0, -0.012 * sc);
    col += vec3(1.0, 0.6, 0.25) * exp(-length(vec2(rq.x / (0.004 * sc), rq.y / (0.02 * sc)))) * 0.25 * fl * refl;
  }

  // lamp-lit boats drifting slowly across
  for (int i = 0; i < 2; i++) {
    float fi = float(i);
    if (fi >= float(boats)) break;
    float y = fi < 0.5 ? -0.25 : -0.37;
    float sc = fi < 0.5 ? 1.0 : 1.35;
    float dir = fi < 0.5 ? 1.0 : -1.0;
    float x = (fract(0.3 + fi * 0.5 + dir * tm * 0.008) * 1.5 - 0.75);
    y += 0.002 * sin(tm * 0.9 + fi);
    vec3 bg = vec3(0.0);
    vec4 bt = boat(p, vec2(x, y), sc, tm, fi, px, bg);
    col = mix(col, bt.rgb, bt.a);
    col += bg;
    vec2 rq = p - vec2(x - 0.045 * sc, y - 0.02 * sc);
    col += vec3(1.0, 0.6, 0.25) * exp(-length(vec2(rq.x / (0.006 * sc), rq.y / (0.035 * sc)))) * 0.3 * refl;
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
  vec2 pin = (uv - 0.5) * vec2(14.0, 14.0);
  float px = max(14.0 / RENDERSIZE.x, 14.0 / RENDERSIZE.y);

  float tm = TIME * speed;
  float tReal = TIME;
  int pal = int(colorMode);
  float waveAmt = waveAmount;
  float floatAmt = floatCount;
  int boats = int(boatCount);
  float fwAmt = fireworkAmount;
  float refl = reflection;
  float bgB = bgBrightness;
  bool doLight = lightUp;
  float drive = audioDrive;
  float mB = matBrightness;
  float mW = matWarmth;
  float oTrim = outerTrim;
  float iTrim = innerTrim;
  bool cal = calibrate;
  float ex = exposure;

  float bass = max(audioBass, 0.2 + 0.08 * sin(TIME * 0.7)) * drive;

  vec2 ho = vec2(7.0) - vec2(oTrim);
  vec2 hi = vec2(6.0) + vec2(iTrim);
  float dOut = max(abs(pin.x) - ho.x, abs(pin.y) - ho.y);
  float dIn = max(abs(pin.x) - hi.x, abs(pin.y) - hi.y);

  vec3 col = vec3(0.0);
  if (dIn < px) {
    vec2 u = pin / (2.0 * hi.y);
    float pxu = px / (2.0 * hi.y);
    if (cal) {
      col = calibInner(u, pxu, 2.0 * hi.y);
    } else if (u.y >= WL) {
      col = aboveWater(u, tm, tReal, pal, waveAmt, fwAmt, bgB, doLight, true, bass, pxu) * ex;
    } else {
      col = river(u, tm, tReal, pal, waveAmt, fwAmt, bgB, doLight, refl, floatAmt, boats, bass, pxu) * ex;
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
