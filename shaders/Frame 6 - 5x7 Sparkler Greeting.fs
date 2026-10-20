/*{
  "DESCRIPTION": "5x7 landscape frame (9x7 mount). A phuljhadi sparkler writes Happy Diwali in glowing light. The letters cool from white-hot to an ember glow and never disappear: when the greeting is complete the sparkler swirls, pauses for an adjustable time, then traces it again, re-igniting the letters. Projects a clean white band on the 1-inch mat.",
  "CREDIT": "Created for Ghost Arcade",
  "ISFVSN": "2",
  "CATEGORIES": ["Generator", "Audio Reactive", "Projection Mapping"],
  "INPUTS": [
    {"NAME": "colorMode", "TYPE": "long", "DEFAULT": 0, "VALUES": [0, 1, 2, 3], "LABELS": ["Golden", "Rose", "Ice", "Festive Colours"], "LABEL": "Light Colours"},
    {"NAME": "speed", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.25, "MAX": 3.0, "LABEL": "Writing Speed"},
    {"NAME": "pauseTime", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.0, "MAX": 8.0, "LABEL": "Pause After Writing (seconds)"},
    {"NAME": "lineWidth", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.5, "MAX": 2.0, "LABEL": "Letter Thickness"},
    {"NAME": "trailGlow", "TYPE": "float", "DEFAULT": 0.6, "MIN": 0.0, "MAX": 1.0, "LABEL": "Letter Glow"},
    {"NAME": "emberLevel", "TYPE": "float", "DEFAULT": 0.5, "MIN": 0.1, "MAX": 1.0, "LABEL": "Resting Glow"},
    {"NAME": "sparkSize", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.4, "MAX": 1.8, "LABEL": "Sparkler Size"},
    {"NAME": "rodThickness", "TYPE": "float", "DEFAULT": 1.5, "MIN": 0.5, "MAX": 2.5, "LABEL": "Sparkler Rod Thickness"},
    {"NAME": "bgBrightness", "TYPE": "float", "DEFAULT": 0.4, "MIN": 0.0, "MAX": 1.0, "LABEL": "Background Brightness"},
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
const float NCURVES = 36.0;
const float PREROLL = 1.0;
const float WRITE_TIME = 9.0;

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

vec2 bez(vec2 a, vec2 c, vec2 b, float t) {
  float s = 1.0 - t;
  return s * s * a + 2.0 * s * t * c + t * t * b;
}

// Hand-lettered strokes for "Happy Diwali" and an underline flourish, as quadratic curves.
void curveData(int i, out vec2 a, out vec2 c, out vec2 b) {
  if (i == 0) { a = vec2(-0.4874, 0.4000); c = vec2(-0.5105, 0.2350); b = vec2(-0.5039, 0.0700); }
  else if (i == 1) { a = vec2(-0.3323, 0.4000); c = vec2(-0.3488, 0.2350); b = vec2(-0.3323, 0.0700); }
  else if (i == 2) { a = vec2(-0.5203, 0.2251); c = vec2(-0.4214, 0.2548); b = vec2(-0.3059, 0.2350); }
  else if (i == 3) { a = vec2(-0.1540, 0.2086); c = vec2(-0.2464, 0.2878); b = vec2(-0.2596, 0.1525); }
  else if (i == 4) { a = vec2(-0.2596, 0.1525); c = vec2(-0.2662, 0.0601); b = vec2(-0.2068, 0.0766); }
  else if (i == 5) { a = vec2(-0.2068, 0.0766); c = vec2(-0.1573, 0.0898); b = vec2(-0.1507, 0.1756); }
  else if (i == 6) { a = vec2(-0.1442, 0.2515); c = vec2(-0.1606, 0.1360); b = vec2(-0.1210, 0.0700); }
  else if (i == 7) { a = vec2(-0.0880, 0.2515); c = vec2(-0.0814, 0.0700); b = vec2(-0.0946, -0.0686); }
  else if (i == 8) { a = vec2(-0.0880, 0.2020); c = vec2(-0.0385, 0.2746); b = vec2(0.0143, 0.2218); }
  else if (i == 9) { a = vec2(0.0143, 0.2218); c = vec2(0.0572, 0.1525); b = vec2(0.0077, 0.0931); }
  else if (i == 10) { a = vec2(0.0077, 0.0931); c = vec2(-0.0517, 0.0535); b = vec2(-0.0880, 0.1096); }
  else if (i == 11) { a = vec2(0.0770, 0.2515); c = vec2(0.0836, 0.0700); b = vec2(0.0704, -0.0686); }
  else if (i == 12) { a = vec2(0.0770, 0.2020); c = vec2(0.1265, 0.2746); b = vec2(0.1793, 0.2218); }
  else if (i == 13) { a = vec2(0.1793, 0.2218); c = vec2(0.2222, 0.1525); b = vec2(0.1727, 0.0931); }
  else if (i == 14) { a = vec2(0.1727, 0.0931); c = vec2(0.1133, 0.0535); b = vec2(0.0770, 0.1096); }
  else if (i == 15) { a = vec2(0.2354, 0.2515); c = vec2(0.2321, 0.0964); b = vec2(0.2948, 0.0865); }
  else if (i == 16) { a = vec2(0.2948, 0.0865); c = vec2(0.3476, 0.0898); b = vec2(0.3542, 0.2515); }
  else if (i == 17) { a = vec2(0.3542, 0.2515); c = vec2(0.3575, -0.0290); b = vec2(0.2948, -0.0686); }
  else if (i == 18) { a = vec2(0.2948, -0.0686); c = vec2(0.2420, -0.0950); b = vec2(0.2354, -0.0290); }
  else if (i == 19) { a = vec2(-0.3805, 0.0000); c = vec2(-0.3706, -0.1650); b = vec2(-0.3805, -0.3300); }
  else if (i == 20) { a = vec2(-0.4003, -0.0066); c = vec2(-0.1693, 0.0198); b = vec2(-0.1759, -0.1650); }
  else if (i == 21) { a = vec2(-0.1759, -0.1650); c = vec2(-0.1825, -0.3432); b = vec2(-0.3805, -0.3300); }
  else if (i == 22) { a = vec2(-0.1033, -0.1485); c = vec2(-0.1099, -0.2475); b = vec2(-0.0934, -0.3300); }
  else if (i == 23) { a = vec2(-0.1000, -0.0792); c = vec2(-0.0967, -0.0726); b = vec2(-0.0918, -0.0792); }
  else if (i == 24) { a = vec2(-0.0406, -0.1485); c = vec2(-0.0340, -0.3036); b = vec2(-0.0010, -0.3300); }
  else if (i == 25) { a = vec2(-0.0010, -0.3300); c = vec2(0.0254, -0.2640); b = vec2(0.0419, -0.1914); }
  else if (i == 26) { a = vec2(0.0419, -0.1914); c = vec2(0.0551, -0.2640); b = vec2(0.0815, -0.3300); }
  else if (i == 27) { a = vec2(0.0815, -0.3300); c = vec2(0.1244, -0.2970); b = vec2(0.1244, -0.1485); }
  else if (i == 28) { a = vec2(0.2762, -0.1914); c = vec2(0.1838, -0.1122); b = vec2(0.1706, -0.2475); }
  else if (i == 29) { a = vec2(0.1706, -0.2475); c = vec2(0.1640, -0.3399); b = vec2(0.2234, -0.3234); }
  else if (i == 30) { a = vec2(0.2234, -0.3234); c = vec2(0.2729, -0.3102); b = vec2(0.2795, -0.2244); }
  else if (i == 31) { a = vec2(0.2861, -0.1485); c = vec2(0.2696, -0.2640); b = vec2(0.3092, -0.3300); }
  else if (i == 32) { a = vec2(0.3653, 0.0165); c = vec2(0.3455, -0.1815); b = vec2(0.3752, -0.3300); }
  else if (i == 33) { a = vec2(0.4379, -0.1485); c = vec2(0.4313, -0.2475); b = vec2(0.4478, -0.3300); }
  else if (i == 34) { a = vec2(0.4412, -0.0792); c = vec2(0.4445, -0.0726); b = vec2(0.4494, -0.0792); }
  else if (i == 35) { a = vec2(-0.4405, -0.4000); c = vec2(0.0159, -0.5000); b = vec2(0.6006, -0.3600); }
  else { a = vec2(0.0); c = vec2(0.0); b = vec2(0.0); }
}

// Each stroke gets an equal slot, except the jump to the second line and the flourish,
// which get extra time so the sparkler glides there instead of jumping.
void curveTiming(float fi, out float st, out float du, out float trav) {
  float unit = WRITE_TIME / (NCURVES + 2.5);
  st = PREROLL + (fi + step(19.5, fi) * 1.0) * unit;
  du = unit;
  trav = 0.25;
  if (fi > 18.5 && fi < 19.5) { du = unit * 2.0; trav = 0.55; }
  if (fi > 34.5) { du = unit * 2.5; trav = 0.5; }
}

vec3 festiveHue(float i) {
  float k = mod(i, 5.0);
  if (k < 0.5) return vec3(1.0, 0.35, 0.30);
  if (k < 1.5) return vec3(1.0, 0.75, 0.20);
  if (k < 2.5) return vec3(0.35, 0.95, 0.45);
  if (k < 3.5) return vec3(0.40, 0.65, 1.0);
  return vec3(0.95, 0.40, 0.95);
}

// Colour of a written stroke as it cools: white-hot, then the palette tone, then a deep ember.
vec3 trailColor(int pal, float age, float idx) {
  vec3 warm = vec3(1.0, 0.70, 0.24);
  vec3 deep = vec3(1.0, 0.38, 0.08);
  if (pal == 1) { warm = vec3(1.0, 0.50, 0.68); deep = vec3(0.90, 0.16, 0.45); }
  if (pal == 2) { warm = vec3(0.62, 0.82, 1.0); deep = vec3(0.45, 0.42, 1.0); }
  if (pal == 3) { warm = festiveHue(idx); deep = warm * vec3(0.9, 0.6, 0.6); }
  vec3 c = mix(vec3(1.0, 0.97, 0.90), warm, smoothstep(0.0, 1.2, age));
  return mix(c, deep, smoothstep(2.5, 7.0, age));
}

vec2 restPos(float ct) {
  vec2 a = vec2(0.0);
  vec2 c = vec2(0.0);
  vec2 b = vec2(0.0);
  curveData(int(NCURVES) - 1, a, c, b);
  return b + vec2(-0.06, 0.08) + vec2(0.05 * sin(ct * 2.4), 0.03 * sin(ct * 4.8));
}

// Where the sparkler tip is at time ct within the cycle.
vec2 sparklerTip(float ct, float cycleLen) {
  vec2 a = vec2(0.0);
  vec2 c = vec2(0.0);
  vec2 b = vec2(0.0);
  if (ct < PREROLL) {
    curveData(0, a, c, b);
    vec2 from = restPos(cycleLen);
    float e = smoothstep(0.0, 1.0, ct / PREROLL);
    return mix(from, a, e) + vec2(0.0, sin(e * PI) * 0.06);
  }
  if (ct < PREROLL + WRITE_TIME) {
    float idx = 0.0;
    float st = 0.0;
    float du = 0.0;
    float trav = 0.25;
    for (int i = 0; i < 36; i++) {
      float s0 = 0.0;
      float d0 = 0.0;
      float t0 = 0.0;
      curveTiming(float(i), s0, d0, t0);
      if (ct >= s0) { idx = float(i); st = s0; du = d0; trav = t0; }
    }
    float f = clamp((ct - st) / du, 0.0, 1.0);
    curveData(int(idx), a, c, b);
    if (f < trav) {
      vec2 pa = vec2(0.0);
      vec2 pc = vec2(0.0);
      vec2 pb = a;
      if (idx > 0.5) curveData(int(idx) - 1, pa, pc, pb);
      float e = smoothstep(0.0, 1.0, f / trav);
      float lift = length(a - pb) * 0.25;
      return mix(pb, a, e) + vec2(0.0, sin(e * PI) * lift);
    }
    return bez(a, c, b, (f - trav) / (1.0 - trav));
  }
  vec2 endP = restPos(ct);
  curveData(int(NCURVES) - 1, a, c, b);
  return mix(b, endP, smoothstep(PREROLL + WRITE_TIME, PREROLL + WRITE_TIME + 0.6, ct));
}

vec3 sparklerArt(vec2 u, float asp, float px, float tm, int pal, float pauseS, float lwMul, float glowAmt, float ember, float sSize, float rodW, float bgB, float bass, float high) {
  float cycleLen = PREROLL + WRITE_TIME + max(pauseS, 0.0) + 0.05;
  float cyc = tm / cycleLen;
  float kc = floor(cyc);
  float ct = fract(cyc) * cycleLen;
  float fadeF = 1.0 - (1.0 - ember) * smoothstep(cycleLen - min(0.9, 0.3 + pauseS * 0.4), cycleLen - 0.02, ct);
  float resid = kc > 0.5 ? (0.6 * ember) : 0.0;
  vec2 tip = sparklerTip(ct, cycleLen);

  // background: dark night with soft, out-of-focus festival lights
  vec3 col = mix(vec3(0.03, 0.018, 0.02), vec3(0.008, 0.008, 0.022), smoothstep(-0.5, 0.5, u.y)) * (0.5 + bgB);
  vec2 g = u * vec2(7.0, 6.0);
  vec2 gid = floor(g);
  float bh = hash21(gid + 3.0);
  vec2 bo = vec2(hash21(gid + 7.0), hash21(gid + 9.0)) - 0.5;
  float bd = length(fract(g) - 0.5 - bo * 0.4);
  vec3 bcol = bh > 0.8 ? vec3(1.0, 0.45, 0.6) : (bh > 0.6 ? vec3(1.0, 0.75, 0.35) : vec3(1.0, 0.55, 0.2));
  col += bcol * (1.0 - smoothstep(0.15, 0.3, bd)) * step(0.55, bh) * 0.10 * bgB * (0.8 + 0.2 * sin(tm * 0.9 + bh * 20.0));
  col += vec3(1.0, 0.7, 0.35) * exp(-length(u - tip) * 5.0) * 0.18;

  // letters
  float lw = max(0.006 * lwMul, px * 1.2);
  float aa = px * 1.5;
  vec3 core = vec3(0.0);
  vec3 halo = vec3(0.0);
  float nearTrail = 1.0;
  for (int i = 0; i < 36; i++) {
    float fi = float(i);
    vec2 a = vec2(0.0);
    vec2 c = vec2(0.0);
    vec2 b = vec2(0.0);
    curveData(i, a, c, b);
    vec2 lo = min(min(a, b), c) - vec2(0.06);
    vec2 hi = max(max(a, b), c) + vec2(0.06);
    if (u.x < lo.x || u.y < lo.y || u.x > hi.x || u.y > hi.y) continue;
    float st = 0.0;
    float du = 0.0;
    float trav = 0.0;
    curveTiming(fi, st, du, trav);
    float startT = st + trav * du;
    float drawDur = (1.0 - trav) * du;
    float r = clamp((ct - startT) / drawDur, 0.0, 1.0);
    vec2 prev = a;
    for (int j = 1; j <= 8; j++) {
      float t1 = float(j) / 8.0;
      vec2 cur = bez(a, c, b, t1);
      float h = 0.0;
      float d = segDist(u, prev, cur, h);
      float tp = t1 - 0.125 + h * 0.125;
      float written = step(tp, r) * step(0.0001, r);
      float age = max(ct - (startT + tp * drawDur), 0.0);
      float inten = written > 0.5 ? (0.6 + 0.4 * exp(-age / 3.0)) * fadeF : resid;
      inten = max(inten, resid);
      vec3 tc = trailColor(pal, written > 0.5 ? age : 6.0, fi);
      float m = lineMask(d, lw, aa);
      core = max(core, tc * m * inten);
      halo += tc * exp(-d / (0.006 + 0.03 * glowAmt)) * inten * 0.06 * (1.0 - smoothstep(0.03, 0.058, d));
      if (inten > 0.01) nearTrail = min(nearTrail, d);
      prev = cur;
    }
  }
  col += halo * (0.2 + 1.2 * glowAmt);
  col = max(col, core * 1.25);

  // glitter along the letters
  vec2 sg = u * 260.0;
  float sh = hash21(floor(sg) + vec2(mod(floor(tm * 12.0), 997.0), 5.0));
  float glit = step(0.93, sh) * lineMask(length(fract(sg) - 0.5), 0.2, 0.2) * (1.0 - smoothstep(lw, lw * 3.5, nearTrail));
  col += vec3(1.0, 0.95, 0.85) * glit * (0.6 + high) * 0.8;

  // sparkler rod: a glowing burning end, a thick textured coating, then the bare metal handle
  vec2 sdir = normalize(vec2(0.38, -1.0));
  vec2 sperp = vec2(-sdir.y, sdir.x);
  float hs = 0.0;
  float rodLen = 0.9;
  float ds = segDist(u, tip, tip + sdir * rodLen, hs);
  float along = hs * rodLen;
  float coatLen = 0.26;
  float coatW = max(0.0085 * rodW, px * 1.5);
  float wireW = max(0.0038 * rodW, px * 1.2);
  float wdth = mix(coatW, wireW, smoothstep(coatLen - 0.01, coatLen + 0.01, along));
  float rod = lineMask(ds, wdth, px * 1.2);
  float across = dot(u - tip, sperp) / max(wdth, 0.0001);
  float roundShade = 0.55 + 0.45 * sqrt(max(1.0 - across * across, 0.0));
  float grit = 0.75 + 0.35 * vnoise(vec2(along * 260.0, across * 3.0));
  vec3 coat = vec3(0.42, 0.38, 0.34) * grit;
  vec3 metal = vec3(0.62, 0.62, 0.66) * (0.7 + 0.5 * smoothstep(0.2, 0.7, -across));
  vec3 rodCol = mix(coat, metal, smoothstep(coatLen - 0.01, coatLen + 0.01, along)) * roundShade;
  rodCol *= 0.55 + 0.9 * exp(-along * 7.0);
  vec3 burning = mix(vec3(1.0, 0.95, 0.75), vec3(1.0, 0.35, 0.06), smoothstep(0.0, 0.03, along));
  rodCol = mix(burning, rodCol, smoothstep(0.015, 0.045, along));
  col = mix(col, rodCol, rod);
  col += vec3(1.0, 0.55, 0.2) * lineMask(ds, wdth * 2.5, px * 3.0) * (1.0 - rod) * exp(-along * 18.0) * 0.5;

  // the sparks: branching rays that flicker many times a second
  vec2 d = u - tip;
  float rr = length(d);
  float ang = atan(d.y, d.x);
  float slot = mod(floor(tm * 15.0), 997.0);
  float n = 64.0;
  float sector = floor((ang / TAU + 0.5) * n);
  float sa = (sector + 0.5) / n * TAU - PI;
  float hh = hash21(vec2(sector, slot));
  float rayLen = (0.03 + 0.11 * hh * hh) * sSize * (1.0 + bass * 0.4);
  vec2 dir = vec2(cos(sa), sin(sa));
  float alongR = dot(d, dir);
  float perp = abs(dot(d, vec2(-dir.y, dir.x)));
  float rw = max(0.0012, px * 0.8);
  float ray = lineMask(perp, rw, px * 1.2) * step(0.0, alongR) * (1.0 - smoothstep(rayLen * 0.55, rayLen, alongR));
  float fk = step(0.45, hash21(vec2(slot, sector + 3.0)));
  vec2 forkO = dir * rayLen * 0.6;
  float fsgn = hash21(vec2(sector, slot + 7.0)) < 0.5 ? -1.0 : 1.0;
  vec2 fdir = vec2(cos(sa + 0.5 * fsgn), sin(sa + 0.5 * fsgn));
  float fh = 0.0;
  float fd = segDist(u, tip + forkO, tip + forkO + fdir * rayLen * 0.4, fh);
  float fork = lineMask(fd, rw, px * 1.2) * fk * (1.0 - fh);
  float star = lineMask(length(d - dir * rayLen), max(0.003, px * 1.2), px * 1.5) * step(0.6, hash21(vec2(slot + 2.0, sector)));
  vec3 sc = mix(vec3(1.0, 0.95, 0.8), trailColor(pal, 0.8, sector), 0.25);
  float fall = 1.0 - clamp(alongR / max(rayLen, 0.001), 0.0, 1.0);
  col += sc * (ray * (0.5 + 0.8 * fall) + fork * 0.7 + star * 0.9);
  col += vec3(1.0, 0.97, 0.88) * 0.012 * sSize / (rr + 0.012 * sSize) * 0.9;

  // falling embers below the tip
  for (int e = 0; e < 10; e++) {
    float fe = float(e);
    float h0 = hash21(vec2(fe, 4.4));
    float ph = fract(tm * (0.9 + 0.5 * h0) + h0);
    float ce = mod(floor(tm * (0.9 + 0.5 * h0) + h0), 97.0);
    float hx = hash21(vec2(fe, ce));
    vec2 epos = tip + vec2((hx - 0.5) * 0.08 * ph, -0.25 * ph * ph - 0.02 * ph);
    float em = lineMask(length(u - epos), max(0.0022, px), px) * (1.0 - ph);
    col += vec3(1.0, 0.6, 0.2) * em;
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
  vec2 pin = (uv - 0.5) * vec2(9.0, 7.0);
  float px = max(9.0 / RENDERSIZE.x, 7.0 / RENDERSIZE.y);

  float tm = TIME * speed;
  int pal = int(colorMode);
  float pauseS = pauseTime;
  float lwMul = lineWidth;
  float glowAmt = trailGlow;
  float ember = emberLevel;
  float sSize = sparkSize;
  float rodW = rodThickness;
  float bgB = bgBrightness;
  float drive = audioDrive;
  float mB = matBrightness;
  float mW = matWarmth;
  float oTrim = outerTrim;
  float iTrim = innerTrim;
  bool cal = calibrate;
  float ex = exposure;

  float bass = max(audioBass, 0.2 + 0.08 * sin(TIME * 0.7)) * drive;
  float high = max(audioHigh, 0.12 + 0.05 * sin(TIME * 1.3)) * drive;

  vec2 ho = vec2(4.5, 3.5) - vec2(oTrim);
  vec2 hi = vec2(3.5, 2.5) + vec2(iTrim);
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
      col = sparklerArt(u, asp, pxu, tm, pal, pauseS, lwMul, glowAmt, ember, sSize, rodW, bgB, bass, high) * ex;
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
