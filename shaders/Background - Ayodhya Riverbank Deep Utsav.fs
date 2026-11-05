/*{
  "DESCRIPTION": "Ayodhya Deep Utsav \u2014 wide Saryu riverbank, illuminated stone ghats and temples, fabric kandils, mango-leaf and marigold torans, hanging brass lamps, rising lanterns, fireworks and travelling waves of diya light reflected in moving water. Adapted from the existing mapper shader library.",
  "CREDIT": "Adapted from projection-mapper library shaders: Ayodhya Deepotsav, A4 Sky Lanterns and Landscape Diya Row. Original library credits retained in README.",
  "ISFVSN": "2",
  "CATEGORIES": [
    "Generator",
    "Audio Reactive",
    "Projection Mapping"
  ],
  "INPUTS": [
    {
      "NAME": "colorMode",
      "TYPE": "long",
      "DEFAULT": 0,
      "VALUES": [
        0,
        1,
        2
      ],
      "LABELS": [
        "Warm Gold",
        "Saffron",
        "Festive Colours"
      ],
      "LABEL": "Temple Lights"
    },
    {
      "NAME": "speed",
      "TYPE": "float",
      "DEFAULT": 1.0,
      "MIN": 0,
      "MAX": 3.0,
      "LABEL": "Speed"
    },
    {
      "NAME": "waveAmount",
      "TYPE": "float",
      "DEFAULT": 0.85,
      "MIN": 0.0,
      "MAX": 1.0,
      "LABEL": "Diya Shimmer Waves"
    },
    {
      "NAME": "floatCount",
      "TYPE": "float",
      "DEFAULT": 0.7,
      "MIN": 0.0,
      "MAX": 1.0,
      "LABEL": "Floating Diyas"
    },
    {
      "NAME": "boatCount",
      "TYPE": "long",
      "DEFAULT": 2,
      "VALUES": [
        0,
        1,
        2
      ],
      "LABELS": [
        "No Boats",
        "One Boat",
        "Two Boats"
      ],
      "LABEL": "Boats"
    },
    {
      "NAME": "fireworkAmount",
      "TYPE": "float",
      "DEFAULT": 0.8,
      "MIN": 0.0,
      "MAX": 1.0,
      "LABEL": "Distant Fireworks"
    },
    {
      "NAME": "reflection",
      "TYPE": "float",
      "DEFAULT": 0.88,
      "MIN": 0.0,
      "MAX": 1.0,
      "LABEL": "River Reflection"
    },
    {
      "NAME": "bgBrightness",
      "TYPE": "float",
      "DEFAULT": 0.7,
      "MIN": 0.0,
      "MAX": 1.0,
      "LABEL": "Sky and Stone Brightness"
    },
    {
      "NAME": "lightUp",
      "TYPE": "bool",
      "DEFAULT": false,
      "LABEL": "Light the Ghat Row by Row at Start"
    },
    {
      "NAME": "audioDrive",
      "TYPE": "float",
      "DEFAULT": 0,
      "MIN": 0.0,
      "MAX": 1.0,
      "LABEL": "Audio Drive"
    },
    {
      "NAME": "exposure",
      "TYPE": "float",
      "DEFAULT": 1.0,
      "MIN": 0.2,
      "MAX": 2.0,
      "LABEL": "Exposure"
    },
    {
      "NAME": "wallAspect",
      "LABEL": "Wall width / height",
      "TYPE": "float",
      "DEFAULT": 1.7777777777777777,
      "MIN": 1,
      "MAX": 2.4
    },
    {
      "NAME": "lanternDensity",
      "LABEL": "Rising lantern density",
      "TYPE": "float",
      "DEFAULT": 0.65,
      "MIN": 0,
      "MAX": 1
    },
    {
      "NAME": "windAmount",
      "LABEL": "Wind strength",
      "TYPE": "float",
      "DEFAULT": 0.45,
      "MIN": 0,
      "MAX": 1
    },
    {
      "NAME": "decorLight",
      "LABEL": "Kandil and toran brightness",
      "TYPE": "float",
      "DEFAULT": 1,
      "MIN": 0,
      "MAX": 1.8
    }
  ]
}*/
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

// Firework rendering from the mapper's A3 Fireworks shader: physical trajectories,
// multi-segment spark trails, changing shells, warm willows and crackle.
float segDist(vec2 p,vec2 a,vec2 b,out float h);
vec3 shellColor(int pal, float h) {
  float i = floor(clamp(h, 0.0, 0.999) * 5.0);
  if (pal == 1) {
    if (i < 0.5) return vec3(1.00, 0.78, 0.35);
    if (i < 1.5) return vec3(0.92, 0.94, 1.00);
    if (i < 2.5) return vec3(1.00, 0.62, 0.20);
    if (i < 3.5) return vec3(1.00, 0.88, 0.60);
    return vec3(0.80, 0.88, 1.00);
  }
  if (pal == 2) {
    if (i < 0.5) return vec3(0.10, 0.85, 0.80);
    if (i < 1.5) return vec3(0.20, 0.38, 1.00);
    if (i < 2.5) return vec3(0.25, 0.90, 0.40);
    if (i < 3.5) return vec3(1.00, 0.80, 0.30);
    return vec3(0.62, 0.32, 1.00);
  }
  if (pal == 3) {
    if (i < 0.5) return vec3(1.00, 0.45, 0.60);
    if (i < 1.5) return vec3(1.00, 0.75, 0.50);
    if (i < 2.5) return vec3(0.95, 0.30, 0.45);
    if (i < 3.5) return vec3(1.00, 0.90, 0.70);
    return vec3(0.90, 0.50, 0.90);
  }
  if (i < 0.5) return vec3(1.00, 0.25, 0.30);
  if (i < 1.5) return vec3(0.30, 0.92, 0.42);
  if (i < 2.5) return vec3(0.32, 0.52, 1.00);
  if (i < 3.5) return vec3(1.00, 0.76, 0.22);
  return vec3(0.86, 0.36, 1.00);
}

vec2 starPos(vec2 c, vec2 dir, float rr, float tau, float grav, float ke) {
  return c + dir * rr * (1.0 - exp(-ke * tau)) - vec2(0.0, 0.5 * grav * tau * tau);
}

// One firework shell after it has burst. b = seconds since the burst, L = its lifetime.
// Types: 0 peony, 1 colour-changing chrysanthemum, 2 gold willow, 3 ring, 4 palm, 5 crackle.
vec3 shellLight(vec2 p, vec2 c, float b, float L, int type, float rmax, vec3 c1, vec3 c2, float seed, float px, float tm) {
  vec3 col = vec3(0.0);
  float grav = 0.022;
  float trailLen = 0.32;
  float nStars = 56.0;
  float layers = 2.0;
  float wid = max(0.0007, px * 0.75);
  float ke = 3.2;
  if (type == 1) trailLen = 0.5;
  if (type == 2) { ke = 1.8; grav = 0.034; trailLen = 1.8; nStars = 30.0; layers = 1.0; }
  if (type == 3) { nStars = 48.0; layers = 1.0; }
  if (type == 4) { ke = 1.3; grav = 0.03; trailLen = 1.3; nStars = 9.0; layers = 1.0; wid *= 2.2; }
  float life = 1.0 - smoothstep(0.55 * L, L, b);
  if (type == 2) life = 1.0 - smoothstep(0.35 * L, L, b);
  float drop = 0.5 * grav * b * b;
  vec2 d = p - c + vec2(0.0, drop);
  float rNow = rmax * (1.0 - exp(-ke * b));
  if (length(d) > rNow * 1.25 + drop + 0.05) return col;

  float rotA = seed * 2.1;
  float tilt = 0.3 + 0.4 * fract(seed * 3.7);
  float rc = cos(rotA);
  float rs = sin(rotA);
  vec2 dn = d;
  if (type == 3) {
    dn = vec2(rc * d.x + rs * d.y, -rs * d.x + rc * d.y);
    dn.y /= tilt;
  }
  float a = atan(dn.y, dn.x);
  vec3 headCol = c1;
  if (type == 1) headCol = mix(c1, c2, smoothstep(0.4 * L, 0.5 * L, b));
  float twEnd = smoothstep(0.5 * L, 0.85 * L, b);
  float slot = mod(floor(tm * 18.0), 997.0);

  for (int ly = 0; ly < 2; ly++) {
    float fl = float(ly);
    if (fl >= layers) break;
    float rf = ly == 0 ? 1.0 : 0.6;
    float n = ly == 0 ? nStars : floor(nStars * 0.6);
    float off = fl * 0.5;
    float base = floor((a / TAU + 0.5) * n + off);
    for (int j = -1; j <= 1; j++) {
      if (type != 2 && j != 0) continue;
      float sector = base + float(j);
      float sj = hash21(vec2(sector + fl * 31.0, seed));
      float sa = (sector + 0.5 - off) / n * TAU - PI + (sj - 0.5) * 0.06;
      vec2 dir = vec2(cos(sa), sin(sa));
      if (type == 3) {
        vec2 rd = vec2(dir.x, dir.y * tilt);
        dir = vec2(rc * rd.x - rs * rd.y, rs * rd.x + rc * rd.y);
      }
      float spread = 0.88 + 0.12 * sj;
      if (type == 0 || type == 1 || type == 5) spread = mix(0.5, 1.0, sqrt(sj));
      if (type == 3) spread = 0.95 + 0.05 * sj;
      float rr = rmax * rf * spread;
      vec2 hp = starPos(c, dir, rr, b, grav, ke);
      float hd = length(p - hp);
      vec2 prev = hp;
      float trail = 0.0;
      for (int s = 1; s <= 4; s++) {
        float tau = max(b - trailLen * float(s) / 4.0, 0.0);
        vec2 cur = starPos(c, dir, rr, tau, grav, ke);
        float h = 0.0;
        float dd = segDist(p, prev, cur, h);
        float along = (float(s) - 1.0 + h) / 4.0;
        trail = max(trail, lineMask(dd, wid * (1.0 - 0.5 * along), px * 1.5) * (1.0 - along));
        prev = cur;
      }
      float tw = mix(1.0, step(0.45, hash21(vec2(sector + fl * 17.0, slot + seed))), twEnd * 0.8);
      float core = lineMask(hd, wid * 1.7, px * 1.5);
      float glow = wid * 2.5 / (hd + wid * 2.5) * 0.3 * (1.0 - smoothstep(0.0, 0.03, hd));
      vec3 hc = mix(headCol, vec3(1.0), 0.45 * core);
      vec3 tc = headCol * 0.75;
      if (type == 2) {
        float gl = step(0.55, hash21(floor(p * 260.0) + mod(floor(tm * 24.0), 997.0)));
        tc = vec3(1.0, 0.62, 0.22) * (0.35 + 0.9 * gl);
      }
      float dim = ly == 0 ? 1.0 : 0.75;
      col += (hc * (core + glow) * tw + tc * trail) * life * dim;
    }
  }

  if (type == 5 && b > 0.4 * L) {
    vec2 g = d * 70.0;
    vec2 gid = floor(g);
    float ph = hash21(gid + fract(seed) * 13.0);
    float popT = L * (0.5 + 0.45 * ph);
    float pop = 1.0 - smoothstep(0.0, 0.08, abs(b - popT));
    float inR = 1.0 - smoothstep(rNow * 0.8, rNow * 1.1, length(d));
    float m = lineMask(length(fract(g) - 0.5), 0.12, 0.2);
    col += vec3(1.0, 0.97, 0.9) * pop * m * inR * step(0.6, hash21(gid + 3.7 + fract(seed))) * 1.4;
  }
  return col;
}

vec3 rocketTrail(vec2 p, vec2 start, vec2 c, float a01, float px, float tm, float seed) {
  float e = 1.0 - (1.0 - a01) * (1.0 - a01);
  vec2 wob = vec2(sin(a01 * 6.0 + seed * 10.0) * 0.006, 0.0);
  vec2 pos = mix(start, c, e) + wob;
  vec2 back = mix(start, c, max(e - 0.14, 0.0)) + wob * 0.5;
  float h = 0.0;
  float dd = segDist(p, pos, back, h);
  float spark = 0.5 + 0.5 * hash21(vec2(floor(h * 14.0), mod(floor(tm * 30.0), 997.0) + seed));
  float trail = lineMask(dd, max(0.0016, px), px * 1.5) * (1.0 - h) * spark;
  float head = 0.004 / (length(p - pos) + 0.004);
  return vec3(1.0, 0.8, 0.5) * (trail * 0.7 + head * 0.45);
}


vec3 distantFireworks(vec2 p,float tm,int pal,float amt,float px){
 vec3 col=vec3(0.);if(amt<.001)return col;
 float lanes=2.+floor(amt*3.);
 for(int i=0;i<5;i++){
  float fi=float(i);if(fi>=lanes)break;
  float tt=tm+fi*6.4/lanes;float cycle=mod(floor(tt/6.4),97.);float age=mod(tt,6.4);
  float hA=hash21(vec2(fi+1.,cycle+3.)),hB=hash21(vec2(fi+4.,cycle+8.));
  float x=(i==0?-.36:i==1?.34:i==2?-.14:i==3?.15:.01)+(hA-.5)*.10;
  vec2 center=vec2(x,.29+hB*.09);float rise=1.+hB*.25;
  if(age<rise){col+=rocketTrail(p,vec2(x,GHAT_TOP),center,age/rise,px,tm,hA)*.7;continue;}
  float b=age-rise;int type=int(mod(fi+cycle,5.));float L=type==2?4.6:3.3;
  if(b>L)continue;
  vec3 c1=shellColor(1,hB),c2=shellColor(1,fract(hB+.4));
  if(type==2||type==4)c1=vec3(1.,.65,.22);
  float radius=.100+.045*hA;
  col+=shellLight(p,center,b,L,type,radius,c1,c2,hA*7.+cycle,px,tm)*.8;
  float smoke=vnoise(p*vec2(16.,21.)+vec2(tm*.08,0.));
  col+=c1*exp(-length(p-center)*13.)*exp(-b*1.4)*.025*smoke;
 }
 return col*amt;
}

// Everything above the water line.
vec3 baseAboveWater(vec2 p, float tm, float tReal, int pal, float waveAmt, float fwAmt, float bgB, bool doLight, bool detail, float bass, float px) {
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

vec3 lanternPaper(int pal, float h) {
  float i = floor(clamp(h, 0.0, 0.999) * 4.0);
  if (pal == 1) {
    if (i < 0.5) return vec3(1.00, 0.55, 0.08);
    if (i < 1.5) return vec3(1.00, 0.78, 0.15);
    if (i < 2.5) return vec3(0.95, 0.30, 0.08);
    return vec3(1.00, 0.66, 0.20);
  }
  if (pal == 2) {
    if (i < 0.5) return vec3(1.00, 0.45, 0.55);
    if (i < 1.5) return vec3(1.00, 0.75, 0.45);
    if (i < 2.5) return vec3(0.95, 0.40, 0.65);
    return vec3(1.00, 0.85, 0.60);
  }
  if (pal == 3) {
    if (i < 0.5) return vec3(1.00, 0.25, 0.20);
    if (i < 1.5) return vec3(1.00, 0.80, 0.20);
    if (i < 2.5) return vec3(0.95, 0.30, 0.70);
    return vec3(1.00, 0.55, 0.12);
  }
  if (i < 0.5) return vec3(1.00, 0.55, 0.15);
  if (i < 1.5) return vec3(1.00, 0.42, 0.10);
  if (i < 2.5) return vec3(1.00, 0.66, 0.25);
  return vec3(0.95, 0.36, 0.12);
}

// One paper sky lantern: a tapered cylinder lit from the burner inside. Returns colour and coverage.
vec4 lantern(vec2 p, vec2 c, float s, float tilt, float flick, vec3 paper, float blur, float px, out vec3 glow) {
  vec2 q = p - c;
  float cs = cos(tilt);
  float sn = sin(tilt);
  q = vec2(cs * q.x + sn * q.y, -sn * q.x + cs * q.y) / s;
  float yt = clamp((q.y + 1.0) * 0.5, 0.0, 1.0);
  float halfW = mix(0.50, 0.70, yt);
  vec2 dq = vec2(abs(q.x) - (halfW - 0.22), q.y - 0.78);
  float dTop = length(max(dq, 0.0)) + min(max(dq.x, dq.y), 0.0) - 0.22;
  float d = max(dTop, -(q.y + 1.0));
  float aa = px / s * 1.5 + blur * 0.12;
  float body = 1.0 - smoothstep(-aa, aa, d);
  float xn = clamp(q.x / halfW, -1.0, 1.0);
  float cyl = sqrt(max(1.0 - xn * xn, 0.0));
  float phi = asin(xn);
  float seam = lineMask(abs(fract(phi / 0.7854 + 0.5) - 0.5), 0.03, 0.05 + blur * 0.2);
  float fromFlame = exp(-(q.y + 0.8) * 1.1);
  float lum = (0.45 + 0.55 * cyl) * (0.55 + 0.75 * clamp(fromFlame, 0.0, 1.6));
  vec3 col = paper * lum;
  col = mix(col, vec3(1.0, 0.92, 0.75), clamp(fromFlame - 0.6, 0.0, 1.0) * 0.45 * cyl);
  col *= 1.0 - seam * 0.3 * (1.0 - blur * 0.7);
  float ring = lineMask(abs(q.y + 0.96), 0.03, 0.04 + blur * 0.2) * step(abs(q.x), halfW);
  col = mix(col, paper * 0.35, ring * 0.7);
  float fl = exp(-length((q - vec2(0.0, -0.86)) * vec2(3.5, 2.5)) * 2.5);
  col += vec3(1.0, 0.85, 0.55) * fl * 0.9;
  col *= flick;
  float halo = exp(-max(d, 0.0) * 2.2);
  glow = paper * halo * 0.22 * flick + vec3(1.0, 0.7, 0.35) * exp(-length(q - vec2(0.0, -0.5)) * 1.6) * 0.06 * flick;
  return vec4(col, body);
}

// A depth layer of lanterns. layer 0 = far, 1 = middle, 2 = near.
vec4 lanternLayer(vec2 u, float asp, float px, float tm, int pal, float density, float sizeMul, float windAmt, float layer, float bass, out vec3 glowOut) {
  glowOut = vec3(0.0);
  vec3 acc = vec3(0.0);
  float cov = 0.0;
  float n = 16.0;
  float s0 = 0.018;
  float rate = 1.0 / 40.0;
  float blur = 0.0;
  float bright = 0.6;
  if (layer > 0.5 && layer < 1.5) { n = 9.0; s0 = 0.036; rate = 1.0 / 28.0; bright = 0.85; }
  if (layer > 1.5) { n = 3.0; s0 = 0.068; rate = 1.0 / 17.0; blur = 1.0; bright = 1.0; }
  float activeN = max(floor(n * (0.3 + 0.7 * density) + 0.5), 1.0);
  for (int i = 0; i < 16; i++) {
    float fi = float(i);
    if (fi >= activeN) break;
    float h0 = hash21(vec2(fi, layer + 3.1));
    float r = rate * (0.88 + 0.24 * h0);
    float cyc = tm * r + (fi + 0.5 * h0) / activeN;
    float k = mod(floor(cyc), 97.0);
    float ph = fract(cyc);
    float hx = hash21(vec2(fi + 7.0, k + layer * 11.0));
    float hc = hash21(vec2(fi + 13.0, k + layer * 5.0));
    float y = layer > 1.5 ? mix(0.10, 0.70, ph) : mix(0.16, 0.62, ph);
    float drift = windAmt * (ph - 0.5) * 0.35;
    float sway = sin(tm * 0.55 + fi * 1.7 + layer) * (0.006 + 0.01 * layer);
    float x = (hx * 2.0 - 1.0) * (asp * 0.5 - 0.03) + drift + sway;
    float sc = s0 * sizeMul * (1.0 - 0.4 * ph) * (0.85 + 0.3 * h0);
    float tilt = sin(tm * 0.8 + fi * 1.3) * 0.07 - windAmt * 0.08;
    float fade = smoothstep(0.0, 0.05, ph) * (1.0 - smoothstep(0.85, 1.0, ph));
    vec2 c = vec2(x, y);
    vec2 dp = u - c;
    if (dot(dp, dp) > (sc * 5.0) * (sc * 5.0)) continue;
    float flick = (0.86 + 0.14 * sin(tm * 7.0 + fi * 2.3) * sin(tm * 4.3 + fi)) * (1.0 + bass * 0.3);
    vec3 paper = lanternPaper(pal, hc);
    vec3 g = vec3(0.0);
    vec4 lt = lantern(u, c, sc, tilt, flick, paper, blur, px, g);
    float a = lt.a * fade;
    acc = mix(acc, lt.rgb * bright * (1.0 - 0.35 * ph), a);
    cov = max(cov, a);
    glowOut += g * fade * bright;
  }
  return vec4(acc, cov);
}

const float GUST_PERIOD = 10.0;
float sq(float x){return x*x;}
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


// All additions participate in the same reflected scene as the ghat.
vec3 festivalDecor(vec3 col,vec2 p,float tm,float px){
 float asp=wallAspect;
 // Two local torans frame the view; their leaf ribs, marigold petals and ropes use library shading.
 for(int side=0;side<2;side++){
  float sg=side==0?-1.:1.;float cx=sg*(asp*.5-.255);
  vec2 local=p-vec2(cx,0.);
  if(abs(local.x)<.29&&p.y>.22){vec4 tr=toran(local,.58,tm,windAmount,decorLight,px);float fade=1.-smoothstep(.25,.29,abs(local.x));col=mix(col,tr.rgb,tr.a*fade);}
  float kx=sg*(asp*.5-.17);float sway=sin(tm*.88+float(side)*1.6)*.045*(.3+windAmount);
  float ksc=.060;vec2 kc=vec2(kx,.215);
  if(abs(p.x-kx)<.16&&p.y>-.03){
   vec3 sideC=side==0?vec3(.85,.10,.17):vec3(.12,.38,.75);
   vec4 kd=akashKandil(p,kc,ksc,2.65,sway,tm,float(side),sideC,vec3(1.,.55,.10),sideC,vec3(1.,.74,.22),decorLight,.85,px);
   col=mix(col,kd.rgb,kd.a);
   float hh=0.;float cord=segDist(p,vec2(kx,.52),vec2(kx,.215+2.65*ksc),hh);col=mix(col,vec3(.32,.20,.08),lineMask(cord,.0008,px));
   col+=vec3(1.,.35,.08)*exp(-length((p-kc)*vec2(1.,.8))/.065)*.08*decorLight;
  }
  // Brass hanging lamp: three fine chains meet the moving bowl, and the flame bends independently.
  float lx=sg*(asp*.5-.075);float ang=sin(tm*.74+float(side)*2.)*.04*windAmount;
  vec2 pivot=vec2(lx,.50);vec2 center=pivot+vec2(sin(ang),-cos(ang))*.40;
  if(abs(p.x-lx)<.10){
   float h=0.;for(int chain=0;chain<3;chain++){float off=(float(chain)-1.)*.013;float d=segDist(p,pivot,center+vec2(off,0.),h);col=mix(col,vec3(.60,.39,.13)*decorLight,lineMask(d,.00065,px));}
   vec2 q=(p-center)/.18;float cs=cos(ang),sn=sin(ang);q=vec2(cs*q.x+sn*q.y,-sn*q.x+cs*q.y);
   vec4 lamp=diya(q,tm,float(side)+25.,1.,sin(tm*1.3)*windAmount,1.1,0.,px/.18,2);col=mix(col,lamp.rgb*decorLight,lamp.a);
   col+=vec3(1.,.58,.20)*exp(-length(p-center-vec2(.009,.012))/.027)*.18*decorLight;
  }
 }
 return col;
}
vec3 aboveWater(vec2 p,float tm,float tReal,int pal,float waveAmt,float fwAmt,float bgB,bool doLight,bool detail,float bass,float px){
 vec3 col=baseAboveWater(p,tm,tReal,pal,waveAmt,fwAmt,bgB,doLight,detail,bass,px);
 // Lanterns are behind temple silhouettes. Keep the open upper sky as their emergence region.
 if(p.y>GHAT_TOP+.07){
  for(int layer=0;layer<3;layer++){
   vec3 glow=vec3(0.);vec4 sky=lanternLayer(p,wallAspect,px,tm,1,lanternDensity,.48,windAmount,float(layer),bass,glow);
   float fade=smoothstep(.17,.24,p.y);col=mix(col,sky.rgb,sky.a*fade)+glow*fade;
  }
  // Composite temple coverage once more so a lantern can pass behind a spire.
  vec4 tp=temples(p,tm,pal,bgB,px);col=mix(col,tp.rgb,tp.a);
 }
 return festivalDecor(col,p,tm,px);
}
vec3 river(vec2 p, float tm, float tReal, int pal, float waveAmt, float fwAmt, float bgB, bool doLight, float refl, float floatAmt, int boats, float bass, float px) {
  float depth = WL - p.y;
  vec3 col = mix(vec3(0.03, 0.025, 0.04), vec3(0.008, 0.010, 0.025), smoothstep(0.0, 0.3, depth)) * (0.5 + 0.8 * bgB);
  float flowT = tm * 0.35;
  float n1 = vnoise(vec2(p.x * 8.0 + 3.0, p.y * 55.0 + flowT * 4.0));
  float n2 = vnoise(vec2(p.x * 20.0 - 7.0, p.y * 120.0 - flowT * 7.0));
  float rip = ((n1 - 0.5) * 0.009 + (n2 - 0.5) * 0.003) * (0.35 + depth * 3.0);
  vec2 rp = vec2(p.x + rip, WL + depth * 1.6);
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
    float x = fract(h3 + v * tm / (wallAspect+.16)) * (wallAspect+.16) - (wallAspect+.16)*.5;
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
    float x = (fract(0.3 + fi * 0.5 + dir * tm * 0.008) * (wallAspect+.3) - (wallAspect+.3)*.5);
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


const vec2 CANVAS=vec2(1920.,1080.);
void main(){
 vec2 uv=gl_FragCoord.xy/RENDERSIZE;vec2 p=(uv-.5)*vec2(wallAspect,1.);
 float px=max(wallAspect/RENDERSIZE.x,1./RENDERSIZE.y);
 float tm=TIME*speed;float bass=audioBass*audioDrive;
 vec3 col;
 if(p.y>=WL)col=aboveWater(p,tm,tm,int(colorMode),waveAmount,fireworkAmount,bgBrightness,lightUp,true,bass,px);
 else col=river(p,tm,tm,int(colorMode),waveAmount,fireworkAmount,bgBrightness,lightUp,reflection,floatCount,int(boatCount),bass,px);
 // Gentle filmic shoulder, retaining the library's shaded materials and warm white flame cores.
 col=max(col*exposure,0.);col=col/(1.+col*.30);col=pow(clamp(col,0.,1.),vec3(.90));
 gl_FragColor=vec4(col,1.);
}
