/*{
  "DESCRIPTION": "5x7 portrait frame (8x10 mount, 6x8 or 5x7 opening). Seen from above, a spinning chakri skids around the whole frame, bouncing off the borders with a burst of sparks at every hit. Its route drifts so it never repeats. Fine jets and a dense spray of sparks fly off it the whole time, ricochet off the edges and light up a polished sandstone floor with a powder rangoli, and scattered petals. Projects a clean white band on the mat.",
  "CREDIT": "Created for Ghost Arcade",
  "ISFVSN": "2",
  "CATEGORIES": ["Generator", "Audio Reactive", "Projection Mapping"],
  "INPUTS": [
    {"NAME": "colorMode", "TYPE": "long", "DEFAULT": 1, "VALUES": [0, 1, 2, 3], "LABELS": ["Golden", "Colour Changing", "Emerald", "Rainbow"], "LABEL": "Spark Colours"},
    {"NAME": "speed", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.25, "MAX": 3.0, "LABEL": "Speed"},
    {"NAME": "chakriSize", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.6, "MAX": 2.2, "LABEL": "Chakri Size"},
    {"NAME": "moveSpeed", "TYPE": "float", "DEFAULT": 0.7, "MIN": 0.0, "MAX": 1.5, "LABEL": "Travel Speed"},
    {"NAME": "spinSpeed", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.4, "MAX": 2.0, "LABEL": "Spin Speed"},
    {"NAME": "sparkSpread", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.6, "MAX": 1.5, "LABEL": "Spark Spread"},
    {"NAME": "sparkDensity", "TYPE": "float", "DEFAULT": 0.7, "MIN": 0.0, "MAX": 1.0, "LABEL": "Spark Density"},
    {"NAME": "bounceBurst", "TYPE": "float", "DEFAULT": 0.7, "MIN": 0.0, "MAX": 1.0, "LABEL": "Border Hit Burst"},
    {"NAME": "smokeAmount", "TYPE": "float", "DEFAULT": 0.3, "MIN": 0.0, "MAX": 1.0, "LABEL": "Smoke"},
    {"NAME": "bgBrightness", "TYPE": "float", "DEFAULT": 0.6, "MIN": 0.0, "MAX": 1.0, "LABEL": "Background Brightness"},
    {"NAME": "openingSize", "TYPE": "long", "DEFAULT": 0, "VALUES": [0, 1], "LABELS": ["6x8 opening", "5x7 opening"], "LABEL": "Opening Size"},
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
const float WHEEL_BASE = 0.036;

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

float fbm3(vec2 p) {
  float v = 0.0;
  float a = 0.5;
  for (int i = 0; i < 3; i++) {
    v += a * vnoise(p);
    p = p * 2.03 + vec2(7.1, 3.7);
    a *= 0.5;
  }
  return v;
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

// Triangle wave in -1..1 with period 1: the shape of a bounce back and forth.
float tri(float s) {
  return abs(fract(s) * 2.0 - 1.0) * 2.0 - 1.0;
}

// Folds a free position back inside the box, as if it bounced off the walls.
vec2 foldBox(vec2 p, vec2 bnd) {
  return vec2(bnd.x * tri(0.75 + p.x / (4.0 * bnd.x)), bnd.y * tri(0.75 + p.y / (4.0 * bnd.y)));
}

vec3 phaseColor(float i) {
  float k = mod(i, 5.0);
  if (k < 0.5) return vec3(0.35, 1.0, 0.40);
  if (k < 1.5) return vec3(1.0, 0.25, 0.30);
  if (k < 2.5) return vec3(1.0, 0.78, 0.25);
  if (k < 3.5) return vec3(0.40, 0.60, 1.0);
  return vec3(1.0, 0.35, 0.95);
}

vec3 baseColor(int pal, float t, float ang) {
  if (pal == 0) return vec3(1.0, 0.74, 0.28);
  if (pal == 2) return vec3(0.40, 1.0, 0.45);
  if (pal == 3) {
    float hsel = fract(ang / TAU) * 5.0;
    return mix(phaseColor(floor(hsel)), phaseColor(floor(hsel) + 1.0), fract(hsel));
  }
  float ph = t / 3.5;
  float idx = mod(floor(ph), 5.0);
  float fr = smoothstep(0.75, 1.0, fract(ph));
  return mix(phaseColor(idx), phaseColor(idx + 1.0), fr);
}

vec3 sparkColor(vec3 base, float heat) {
  vec3 c = mix(vec3(1.0, 0.97, 0.90), base, smoothstep(0.05, 0.45, heat));
  return mix(c, base * vec3(0.9, 0.55, 0.4), smoothstep(0.6, 1.0, heat));
}

// The chakri's free path: a steady glide whose heading slowly wanders, so the route never repeats.
vec2 freePath(float t, vec2 vel) {
  return vec2(vel.x * t + 0.35 * sin(t * 0.071 + 1.3) + 0.2 * sin(t * 0.0313 + 0.4),
              vel.y * t + 0.30 * sin(t * 0.053 + 2.1) + 0.2 * sin(t * 0.0271 + 4.0));
}

vec2 freeVel(float t, vec2 vel) {
  return vec2(vel.x + 0.35 * 0.071 * cos(t * 0.071 + 1.3) + 0.2 * 0.0313 * cos(t * 0.0313 + 0.4),
              vel.y + 0.30 * 0.053 * cos(t * 0.053 + 2.1) + 0.2 * 0.0271 * cos(t * 0.0271 + 4.0));
}

// Where the chakri is: the free path folded into the box, which makes it bounce off the borders.
vec2 chakriPos(float t, vec2 bnd, vec2 vel) {
  return foldBox(freePath(t, vel), bnd);
}

// Sparks thrown off a border where the chakri hit it, tau seconds ago.
vec3 wallBurst(vec2 u, vec2 hitP, vec2 normal, float tau, float seed, vec3 base, float amt, float px) {
  vec3 col = vec3(0.0);
  if (tau > 1.1 || amt < 0.001) return col;
  if (length(u - hitP) > 0.5) return col;
  float flash = exp(-tau * 9.0);
  float dh = length(u - hitP);
  col += vec3(1.0, 0.95, 0.85) * exp(-dh * 22.0) * flash * 2.2 * amt;
  col += base * exp(-dh * 7.0) * flash * 0.9 * amt * (1.0 - smoothstep(0.35, 0.5, dh));
  vec2 alongW = vec2(-normal.y, normal.x);
  float wallLine = exp(-abs(dot(u - hitP, normal)) * 120.0) * exp(-abs(dot(u - hitP, alongW)) * 9.0);
  col += mix(vec3(1.0), base, 0.4) * wallLine * exp(-tau * 4.0) * 1.2 * amt;
  vec2 tang = vec2(-normal.y, normal.x);
  for (int i = 0; i < 26; i++) {
    float fi = float(i);
    float h1 = hash21(vec2(fi, seed));
    float h2 = hash21(vec2(seed, fi + 7.0));
    float ang = (h1 - 0.5) * 2.6;
    vec2 dir = normal * cos(ang) + tang * sin(ang);
    float v = 0.45 + 0.7 * h2;
    float k = 2.6;
    float s1 = (1.0 - exp(-k * tau)) / k;
    float s0 = (1.0 - exp(-k * max(tau - 0.05, 0.0))) / k;
    vec2 p1 = hitP + dir * v * s1;
    vec2 p0 = hitP + dir * v * s0;
    float hh = 0.0;
    float dd = segDist(u, p1, p0, hh);
    float m = lineMask(dd, max(0.0013, px * 0.9), px * 1.3) * (1.0 - 0.6 * hh);
    float hg = 0.004 / (length(u - p1) + 0.004) * 0.3 * (1.0 - smoothstep(0.0, 0.03, length(u - p1)));
    float life = 1.0 - smoothstep(0.4 + 0.5 * h2, 1.1, tau);
    col += sparkColor(base, tau * 1.0) * (m * 1.6 + hg) * life * amt;
  }
  return col;
}

// The floor seen from above: polished sandstone tiles, a powder rangoli in the centre
// and marigold petals scattered around.
vec3 floorAlbedo(vec2 u, float asp, float px) {
  float n1 = vnoise(u * 9.0);
  float n2 = vnoise(u * 34.0 + 3.0);
  vec2 tq = u * 4.0 + 0.5;
  vec2 tid = floor(tq);
  vec2 tg = abs(fract(tq) - 0.5);
  float grout = max(lineMask(0.5 - tg.x, 0.0, 0.025), lineMask(0.5 - tg.y, 0.0, 0.025));
  float vein = smoothstep(0.47, 0.5, abs(fract(vnoise(u * 5.0 + tid * 3.1) * 3.0 + u.x * 2.0) - 0.5)) * 0.25;
  vec3 stone = mix(vec3(0.62, 0.44, 0.30), vec3(0.72, 0.55, 0.38), hash21(tid)) * (0.82 + 0.18 * n1 + 0.08 * n2);
  stone = mix(stone, vec3(0.85, 0.72, 0.55), vein);
  vec3 alb = mix(stone, vec3(0.20, 0.13, 0.09), grout * 0.55);

  // powder rangoli
  float r = length(u);
  float an = atan(u.y, u.x);
  float grain = 0.8 + 0.3 * vnoise(u * 320.0) + 0.1 * (vnoise(u * 90.0) - 0.5);
  float pIn = 0.5 + 0.5 * cos(an * 8.0);
  float rIn = 0.035 + 0.07 * pow(pIn, 0.7);
  float pOut = 0.5 - 0.5 * cos(an * 12.0);
  float rOut = 0.13 + 0.12 * pow(pOut, 0.8);
  float rOutIn = 0.13 + 0.065 * pow(pOut, 0.8);
  vec3 rc = vec3(0.0);
  float inR = 0.0;
  if (r < 0.035) { rc = vec3(1.0, 0.80, 0.08); inR = 1.0; }
  else if (r < rIn) { rc = vec3(0.90, 0.10, 0.55); inR = 1.0; }
  else if (r < 0.11) { rc = vec3(0.80, 0.08, 0.10); inR = 1.0; }
  else if (r < 0.13) { rc = vec3(1.0, 0.48, 0.04); inR = 1.0; }
  else if (r < rOutIn) { rc = vec3(0.12, 0.32, 0.85); inR = 1.0; }
  else if (r < rOut) { rc = vec3(0.10, 0.60, 0.25); inR = 1.0; }
  float aa = px * 1.5;
  float outline = max(max(lineMask(abs(r - 0.035), 0.0025, aa), lineMask(abs(r - rIn), 0.0025, aa) * step(0.035, r)),
                  max(max(lineMask(abs(r - 0.11), 0.0022, aa), lineMask(abs(r - 0.13), 0.0022, aa)),
                      max(lineMask(abs(r - rOut), 0.0028, aa) * step(0.13, r), lineMask(abs(r - rOutIn), 0.002, aa) * step(0.13, r))));
  float nd = 36.0;
  float cellA = floor(an * nd / TAU);
  float arc = (fract(an * nd / TAU) - 0.5) * TAU * 0.285 / nd;
  float dotM = lineMask(length(vec2(r - 0.285, arc)), 0.0075, aa);
  vec3 dotC = mod(cellA, 2.0) < 0.5 ? vec3(0.97, 0.95, 0.88) : vec3(1.0, 0.78, 0.10);
  alb = mix(alb, rc * grain, inR);
  alb = mix(alb, vec3(0.97, 0.95, 0.90) * grain, outline);
  alb = mix(alb, dotC * grain, dotM);

  // scattered marigold and rose petals
  vec2 pg = u * vec2(38.0, 38.0);
  vec2 pid = floor(pg);
  float ph = hash21(pid + 11.0);
  vec2 po = vec2(hash21(pid + 3.0), hash21(pid + 6.0)) - 0.5;
  float pa = hash21(pid + 9.0) * 3.14;
  vec2 pq = fract(pg) - 0.5 - po * 0.4;
  pq = vec2(cos(pa) * pq.x + sin(pa) * pq.y, -sin(pa) * pq.x + cos(pa) * pq.y);
  float zone = smoothstep(0.30, 0.33, r) * (1.0 - smoothstep(0.42, 0.5, r));
  float petal = (1.0 - smoothstep(0.85, 1.0, length(pq * vec2(2.6, 4.2)))) * step(0.86, ph) * zone;
  vec3 pc = ph > 0.96 ? vec3(0.85, 0.06, 0.18) : (ph > 0.91 ? vec3(1.0, 0.78, 0.10) : vec3(1.0, 0.48, 0.04));
  alb = mix(alb, pc, petal);
  return alb;
}

vec3 chakriArt(vec2 u, float asp, float px, float tm, int pal, float csize, float move, float spin, float spread, float dens, float burst, float smokeAmt, float bgB, float bass, float high) {
  float wr = WHEEL_BASE * csize;
  vec2 bnd = vec2(asp * 0.5 - wr - 0.012, 0.5 - wr - 0.012);
  vec2 vel = vec2(0.161, 0.119) * move / 0.7;
  vec2 cen = chakriPos(tm, bnd, vel);
  float spinPh = fract(tm * spin * 1.6);
  float spinA = spinPh * TAU;
  float R = 0.20 * spread * csize * (1.0 + bass * 0.2);
  vec3 base = baseColor(pal, tm, spinA);
  float flick = 0.9 + 0.1 * sin(tm * 37.0) * sin(tm * 23.0) + 0.1 * bass;

  // background: the decorated floor, lit softly all over and brightly by the chakri
  vec2 halfB = vec2(asp * 0.5, 0.5);
  float dl = length(u - cen);
  float pool = exp(-dl * 6.0 / sqrt(csize));
  vec3 alb = floorAlbedo(u, asp, px);
  float vign = 1.0 - 0.35 * smoothstep(0.25, 0.75, length(u * vec2(0.9, 0.8)));
  vec3 ambient = vec3(1.0, 0.86, 0.72) * (0.06 + 0.26 * bgB) * vign;
  vec3 col = alb * (ambient + base * pool * 1.3 * flick);
  col += vec3(1.0, 0.9, 0.8) * exp(-dl * 22.0) * 0.06;

  // border hits
  if (move > 0.01) {
    vec2 fp = freePath(tm, vel);
    vec2 fv = freeVel(tm, vel);
    float sx = 0.75 + fp.x / (4.0 * bnd.x);
    float tauX = fract(2.0 * sx) / max(2.0 * fv.x / (4.0 * bnd.x), 0.0001);
    float sideX = fract(sx) < 0.5 ? 1.0 : -1.0;
    vec2 hitPX = vec2(sideX * halfB.x, chakriPos(tm - tauX, bnd, vel).y);
    float seedX = floor(2.0 * sx);
    col += wallBurst(u, hitPX, vec2(-sideX, 0.0), tauX, seedX + 1.3, base, burst, px);
    float sy = 0.75 + fp.y / (4.0 * bnd.y);
    float tauY = fract(2.0 * sy) / max(2.0 * fv.y / (4.0 * bnd.y), 0.0001);
    float sideY = fract(sy) < 0.5 ? 1.0 : -1.0;
    vec2 hitPY = vec2(chakriPos(tm - tauY, bnd, vel).x, sideY * halfB.y);
    float seedY = floor(2.0 * sy);
    col += wallBurst(u, hitPY, vec2(0.0, -sideY), tauY, seedY + 5.7, base, burst, px);
  }

  vec2 g = u - cen;
  float r = length(g);
  float th = atan(g.y, g.x);

  // three fine jets curling out of the nozzles, with fainter sub-streams beside them
  float jets = 0.0;
  for (int k = 0; k < 3; k++) {
    float off = float(k) * 0.07;
    float fj = fract(th / TAU * 3.0 + (r - wr) * 9.0 / csize - spinPh * 3.0 + off);
    float d = min(fj, 1.0 - fj) * TAU / 3.0 * max(r, 0.01);
    float w = max(0.0016 + 0.004 * (r - wr), px * 0.9);
    float strength = k == 0 ? 1.0 : 0.45;
    jets += lineMask(d, w, px * 1.5) * strength;
  }
  float jetFade = smoothstep(wr * 0.8, wr * 1.15, r) * exp(-(r - wr) * 22.0 / (spread * csize)) * (1.0 - smoothstep(wr + 0.06 * spread * csize, wr + 0.12 * spread * csize, r));
  float breakup = 0.55 + 0.45 * step(0.35, hash21(vec2(floor(r * 90.0 - tm * 30.0), floor((th / TAU + 0.5) * 60.0))));
  float heatJ = clamp((r - wr) / R, 0.0, 1.0);
  vec3 jetBase = pal == 3 ? baseColor(pal, tm, th - spinA) : base;
  col += sparkColor(jetBase, heatJ * 0.7) * jets * jetFade * breakup * 2.2 * flick;

  // dense spray of thin sparks flung out tangentially along the path; they ricochet off the borders
  float nP = floor(20.0 + 40.0 * dens);
  for (int i = 0; i < 60; i++) {
    float fi = float(i);
    if (fi >= nP) break;
    float h1 = hash21(vec2(fi, 3.3));
    float rate = 1.6 + 0.8 * h1;
    float em = tm * rate + h1;
    float ce = floor(em);
    float pe = fract(em);
    float h2 = hash21(vec2(fi + 2.0, ce));
    float h3 = hash21(vec2(ce + 4.0, fi));
    float age = pe / rate * 1.1;
    vec2 emitP = chakriPos(tm - age, bnd, vel);
    float aEm = fract(spinPh - age * spin * 1.6 + floor(h2 * 3.0) / 3.0 + (h3 - 0.5) * 0.08) * TAU;
    vec2 radialD = vec2(cos(aEm), sin(aEm));
    vec2 src = emitP + wr * radialD;
    vec2 vdir = normalize(vec2(sin(aEm), -cos(aEm)) + 0.5 * radialD);
    float v = (0.45 + 0.45 * h3) * spread * sqrt(csize);
    float kf = 2.4;
    float s1 = (1.0 - exp(-kf * age)) / kf;
    float s0 = (1.0 - exp(-kf * max(age - 0.12, 0.0))) / kf;
    vec2 p1 = foldBox(src + vdir * v * s1, halfB);
    vec2 p0 = foldBox(src + vdir * v * s0, halfB);
    if (length(u - p1) > 0.12 * csize) continue;
    float hh = 0.0;
    float dd = segDist(u, p1, p0, hh);
    float taper = 1.0 - hh;
    float m = lineMask(dd, max(0.0011 * (0.4 + 0.6 * taper), px * 0.7), px * 1.3) * taper * taper * step(length(p1 - p0), 0.1);
    float hl = length(u - p1);
    float headG = 0.0025 / (hl + 0.0025) * 0.3 * (1.0 - smoothstep(0.0, 0.02, hl));
    float fade = 1.0 - smoothstep(0.55, 1.0, pe);
    vec3 pb = pal == 3 ? baseColor(pal, tm, aEm) : base;
    col += mix(sparkColor(pb, 0.15 + 0.85 * pe), pb * 0.8, hh) * (m * 1.6 + headG * 1.2) * fade;
  }

  // glitter crackle around the wheel
  vec2 gq = vec2(r * 180.0 - tm * 30.0, (th / TAU + 0.5) * 300.0);
  float gh = hash21(floor(gq) + 9.0);
  float gl = step(1.0 - 0.05 * (0.4 + dens) * (1.0 + high), gh) * lineMask(length(fract(gq) - 0.5), 0.16, 0.2);
  col += sparkColor(base, 0.3) * gl * (1.0 - smoothstep(R * 0.4, R, r)) * smoothstep(wr, wr * 1.5, r) * 1.2;

  // the wheel: a white-hot spinning disc with a fiery rim and bright nozzles
  float rimRing = exp(-abs(r - wr * 0.85) * 220.0 / csize);
  float disc = 1.0 - smoothstep(wr * 0.95 - px, wr * 0.95 + px, r);
  float swirl = 0.75 + 0.25 * sin((th - spinA) * 6.0 + r * 300.0 / csize);
  vec3 discCol = mix(base * 0.9, vec3(1.0, 0.95, 0.85), exp(-r * 70.0 / csize)) * swirl;
  col = mix(col, discCol * flick, disc * 0.9);
  col += mix(vec3(1.0), base, 0.35) * rimRing * 0.9 * flick;
  for (int k = 0; k < 3; k++) {
    float na = spinA + float(k) * TAU / 3.0;
    vec2 noz = wr * vec2(cos(na), sin(na));
    col += vec3(1.0, 0.97, 0.9) * exp(-length(g - noz) * 160.0 / csize) * 1.6 * flick;
  }
  col += vec3(1.0, 0.97, 0.9) * exp(-r * 55.0 / csize) * 0.8;
  col += base * exp(-r * 14.0 / csize) * 0.35 * flick;

  // a little smoke trailing along its path
  float w = fract(tm * 0.05);
  vec2 sp = u * vec2(4.0, 4.0);
  float n1 = fbm3(sp + vec2(-w * 5.0, 0.0));
  float n2 = fbm3(sp + vec2(-fract(w + 0.5) * 5.0 + 3.7, 5.1));
  float sn = mix(n1, n2, abs(2.0 * w - 1.0));
  float nearPath = exp(-length(u - cen) * 6.0) + 0.5 * exp(-length(u - chakriPos(tm - 0.8, bnd, vel)) * 6.0) + 0.3 * exp(-length(u - chakriPos(tm - 1.6, bnd, vel)) * 6.0);
  float smk = smoothstep(0.4, 0.85, sn) * min(nearPath, 1.2) * smokeAmt;
  col += (vec3(0.06, 0.055, 0.06) + base * 0.25) * smk;
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
  vec2 pin = (uv - 0.5) * vec2(8.0, 10.0);
  float px = max(8.0 / RENDERSIZE.x, 10.0 / RENDERSIZE.y);

  float tm = TIME * speed;
  int pal = int(colorMode);
  float csize = chakriSize;
  float move = moveSpeed;
  float spin = spinSpeed;
  float spread = sparkSpread;
  float dens = sparkDensity;
  float burst = bounceBurst;
  float smokeAmt = smokeAmount;
  float bgB = bgBrightness;
  int opening = int(openingSize);
  float drive = audioDrive;
  float mB = matBrightness;
  float mW = matWarmth;
  float oTrim = outerTrim;
  float iTrim = innerTrim;
  bool cal = calibrate;
  float ex = exposure;

  float bass = max(audioBass, 0.2 + 0.08 * sin(TIME * 0.7)) * drive;
  float high = max(audioHigh, 0.12 + 0.05 * sin(TIME * 1.3)) * drive;

  vec2 ho = vec2(4.0, 5.0) - vec2(oTrim);
  vec2 hi = (opening == 1 ? vec2(2.5, 3.5) : vec2(3.0, 4.0)) + vec2(iTrim);
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
      col = chakriArt(u, asp, pxu, tm, pal, csize, move, spin, spread, dens, burst, smokeAmt, bgB, bass, high) * ex;
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
