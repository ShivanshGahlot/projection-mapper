/*{
  "DESCRIPTION": "Background behind the gallery: paper sky lanterns rise from behind the sofa in three depths, slipping behind the frames and reappearing in the gaps, while rockets climb from behind the sofa and burst across the wall. Continuous, never empty. Edges fade softly and nothing is projected on the sofa.",
  "CREDIT": "Created for Ghost Arcade",
  "ISFVSN": "2",
  "CATEGORIES": ["Generator", "Projection Mapping"],
  "INPUTS": [
    {"NAME": "speed", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.25, "MAX": 3.0, "LABEL": "Speed"},
    {"NAME": "brightness", "TYPE": "float", "DEFAULT": 0.85, "MIN": 0.1, "MAX": 1.0, "LABEL": "Brightness"},
    {"NAME": "lanternDensity", "TYPE": "float", "DEFAULT": 0.7, "MIN": 0.0, "MAX": 1.0, "LABEL": "Number of Lanterns"},
    {"NAME": "rocketDensity", "TYPE": "float", "DEFAULT": 0.6, "MIN": 0.0, "MAX": 1.0, "LABEL": "Number of Rockets"},
    {"NAME": "burstSize", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.6, "MAX": 1.6, "LABEL": "Burst Size"},
    {"NAME": "colorMode", "TYPE": "long", "DEFAULT": 0, "VALUES": [0, 1, 2, 3], "LABELS": ["Festive Mix", "Gold and Silver", "Peacock", "Rose Gold"], "LABEL": "Firework Colours"},
    {"NAME": "lanternColors", "TYPE": "long", "DEFAULT": 0, "VALUES": [0, 1, 2, 3], "LABELS": ["Classic Saffron", "Marigold Mix", "Rose and Gold", "Festive Mix"], "LABEL": "Lantern Colours"},
    {"NAME": "windAmount", "TYPE": "float", "DEFAULT": 0.3, "MIN": -1.0, "MAX": 1.0, "LABEL": "Wind (left / right)"},
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

const float UNIT = 16.0;

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
  float wid = max(0.0019, px * 1.5);
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


// Lanterns rising from behind the sofa. layer 0 = far, 1 = middle, 2 = near. Units of 16 inches, y up.
vec4 wallLanterns(vec2 p, float wUnits, float ySofa, float yTop, float px, float tm, int pal, float density, float windAmt, float layer, out vec3 glowOut) {
  glowOut = vec3(0.0);
  vec3 acc = vec3(0.0);
  float cov = 0.0;
  float n = 18.0;
  float s0 = 0.07;
  float rate = 1.0 / 46.0;
  float blur = 0.0;
  float bright = 0.6;
  if (layer > 0.5 && layer < 1.5) { n = 10.0; s0 = 0.11; rate = 1.0 / 34.0; bright = 0.9; }
  if (layer > 1.5) { n = 4.0; s0 = 0.17; rate = 1.0 / 24.0; blur = 1.0; bright = 1.0; }
  float activeN = max(floor(n * (0.25 + 0.75 * density) + 0.5), 1.0);
  for (int i = 0; i < 18; i++) {
    float fi = float(i);
    if (fi >= activeN) break;
    float h0 = hash21(vec2(fi, layer + 3.1));
    float r = rate * (0.88 + 0.24 * h0);
    float cyc = tm * r + (fi + 0.5 * h0) / activeN;
    float k = floor(cyc);
    float ph = fract(cyc);
    float hx = hash21(vec2(fi + 7.0, k + layer * 11.0));
    float hc = hash21(vec2(fi + 13.0, k + layer * 5.0));
    float y = mix(ySofa - 0.15, yTop + 0.1, ph);
    float x = hx * wUnits + windAmt * (ph - 0.5) * 0.6 + sin(tm * 0.5 + fi * 1.7 + layer) * (0.01 + 0.015 * layer);
    float sc = s0 * (1.0 - 0.35 * ph) * (0.85 + 0.3 * h0);
    float tilt = sin(tm * 0.8 + fi * 1.3) * 0.07 - windAmt * 0.08;
    float fade = smoothstep(0.0, 0.04, ph) * (1.0 - smoothstep(0.85, 1.0, ph));
    vec2 c = vec2(x, y);
    vec2 dp = p - c;
    if (dot(dp, dp) > (sc * 5.0) * (sc * 5.0)) continue;
    float flick = 0.86 + 0.14 * sin(TIME * 7.0 + fi * 2.3) * sin(TIME * 4.3 + fi);
    vec3 paper = lanternPaper(pal, hc);
    vec3 g = vec3(0.0);
    vec4 lt = lantern(p, c, sc, tilt, flick, paper, blur, px, g);
    float a = lt.a * fade;
    acc = mix(acc, lt.rgb * bright * (1.0 - 0.3 * ph), a);
    cov = max(cov, a);
    glowOut += g * fade * bright;
  }
  return vec4(acc, cov);
}

void main() {
  vec2 uv = gl_FragCoord.xy / RENDERSIZE.xy;
  vec2 w = vec2(uv.x, 1.0 - uv.y) * CANVAS;
  float pxIn = max(CANVAS.x / RENDERSIZE.x, CANVAS.y / RENDERSIZE.y);
  float tm = TIME * speed;
  float bright = brightness;
  float lDens = lanternDensity;
  float rDens = rocketDensity;
  float bsize = burstSize;
  int pal = int(colorMode);
  int lpal = int(lanternColors);
  float windAmt = windAmount;
  float sofaY = CANVAS.y * (1.0 - sofaLine);

  vec2 p = vec2(w.x, CANVAS.y - w.y) / UNIT;
  float px = pxIn / UNIT;
  float wUnits = CANVAS.x / UNIT;
  float ySofa = (CANVAS.y - sofaY) / UNIT;
  float yTop = CANVAS.y / UNIT;

  vec3 col = vec3(0.0);
  col += vec3(1.0, 0.5, 0.18) * exp(-max(p.y - ySofa, 0.0) * 6.0) * 0.10;

  vec3 g0 = vec3(0.0);
  vec4 far = wallLanterns(p, wUnits, ySofa, yTop, px, tm, lpal, lDens, windAmt, 0.0, g0);
  col = mix(col, far.rgb, far.a) + g0 * 0.6;

  // rockets climbing from behind the sofa and bursting across the wall, in staggered lanes
  float lanePeriod = 6.0;
  float laneCount = 2.0 + floor(rDens * 6.0 + 0.5);
  vec3 fw = vec3(0.0);
  for (int i = 0; i < 8; i++) {
    float fi = float(i);
    if (fi >= laneCount) break;
    float lt0 = tm + fi / laneCount * lanePeriod;
    float k = floor(lt0 / lanePeriod);
    float age = fract(lt0 / lanePeriod) * lanePeriod - 0.3 * hash21(vec2(fi + 40.0, k));
    float hA = hash21(vec2(fi, k + 1.3));
    float hB = hash21(vec2(fi + 4.0, k + 2.9));
    float hC = hash21(vec2(fi + 8.0, k + 5.1));
    float hD = hash21(vec2(fi + 12.0, k + 7.3));
    float hE = hash21(vec2(fi + 16.0, k + 9.7));
    int type = 5;
    if (hC < 0.22) type = 0;
    else if (hC < 0.40) type = 1;
    else if (hC < 0.58) type = 2;
    else if (hC < 0.72) type = 3;
    else if (hC < 0.84) type = 4;
    float L = 3.2;
    if (type == 2) L = 4.4;
    if (type == 4) L = 3.6;
    float rise = 1.3 + 0.4 * hB;
    if (age < 0.0 || age > rise + L) continue;
    float zone = hash21(vec2(fi + 21.0, k + 3.3));
    float bx = zone < 0.4 ? (0.06 + 0.16 * hD) : (zone < 0.8 ? (0.78 + 0.16 * hD) : (0.2 + 0.6 * hD));
    float by = zone < 0.8 ? (ySofa + 0.7 + hE * (yTop - ySofa - 1.1)) : (yTop - 0.25 - 0.25 * hE);
    vec2 c = vec2(bx * wUnits, by);
    vec2 start = vec2(c.x + (hB - 0.5) * 0.2, ySofa - 0.05);
    if (age < rise) {
      fw += rocketTrail(p, start, c, age / rise, px, tm, hA) * 1.2;
      continue;
    }
    float b = age - rise;
    float rmax = (0.5 + 0.3 * hB) * bsize;
    vec3 c1 = shellColor(pal, hE);
    vec3 c2 = shellColor(pal, fract(hE + 0.4));
    if (type == 2 || type == 4) c1 = vec3(1.0, 0.68, 0.28);
    fw += shellLight(p, c, b, L, type, rmax, c1, c2, hA * 7.0 + k, px, tm);
    col += c1 * exp(-length(p - c) * 2.2) * exp(-b * 1.2) * 0.18;
  }
  col += fw;

  vec3 g1 = vec3(0.0);
  vec4 mid = wallLanterns(p, wUnits, ySofa, yTop, px, tm, lpal, lDens, windAmt, 1.0, g1);
  col = mix(col, mid.rgb, mid.a) + g1 * 0.7;
  vec3 g2 = vec3(0.0);
  vec4 near = wallLanterns(p, wUnits, ySofa, yTop, px, tm, lpal, lDens, windAmt, 2.0, g2);
  col = mix(col, near.rgb, near.a) + g2 * 0.8;

  col *= bright * wallMask(uv, w, sofaY, featherTop, featherSides);
  gl_FragColor = vec4(clamp(col, 0.0, 1.0), 1.0);
}
