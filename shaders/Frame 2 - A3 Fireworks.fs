/*{
  "DESCRIPTION": "A3 portrait frame (13.75x18.5 mount). A choreographed Diwali fireworks show over a rooftop skyline: rockets, peony, colour-changing chrysanthemum, gold willow, ring, palm and crackle shells, glowing smoke, running continuously at finale pace with no gaps or loop point. Projects a clean white band on the 1-inch mat.",
  "CREDIT": "Created for Ghost Arcade",
  "ISFVSN": "2",
  "CATEGORIES": ["Generator", "Audio Reactive", "Projection Mapping"],
  "INPUTS": [
    {"NAME": "colorMode", "TYPE": "long", "DEFAULT": 0, "VALUES": [0, 1, 2, 3], "LABELS": ["Festive Mix", "Gold and Silver", "Peacock", "Rose Gold"], "LABEL": "Firework Colours"},
    {"NAME": "speed", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.25, "MAX": 4.0, "LABEL": "Speed"},
    {"NAME": "showDensity", "TYPE": "float", "DEFAULT": 0.8, "MIN": 0.0, "MAX": 1.0, "LABEL": "Show Intensity"},
    {"NAME": "burstSize", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.6, "MAX": 1.4, "LABEL": "Burst Size"},
    {"NAME": "smokeAmount", "TYPE": "float", "DEFAULT": 0.5, "MIN": 0.0, "MAX": 1.0, "LABEL": "Smoke"},
    {"NAME": "skylineMode", "TYPE": "long", "DEFAULT": 0, "VALUES": [0, 1, 2], "LABELS": ["Rooftops and Temple", "Rooftops", "Sky Only"], "LABEL": "Skyline"},
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

vec3 nightSky(vec2 u, float tm, float px) {
  float hgt = clamp(u.y + 0.5, 0.0, 1.0);
  vec3 col = mix(vec3(0.040, 0.022, 0.020), vec3(0.004, 0.006, 0.022), smoothstep(0.1, 0.7, hgt));
  vec2 sq = u * 40.0;
  float sh = hash21(floor(sq) + 21.0);
  float tw = 0.6 + 0.4 * sin(tm * 1.7 + sh * 50.0);
  float star = step(0.965, sh) * lineMask(length(fract(sq) - 0.5), 0.05, max(px * 60.0, 0.06));
  col += vec3(0.9, 0.92, 1.0) * star * tw * 0.35 * smoothstep(0.25, 0.6, hgt);
  return col;
}

// Rooftop silhouettes with lit windows and festival lights. Returns colour and coverage.
vec4 skyline(vec2 u, float asp, int mode, float tm, float px) {
  if (mode == 2) return vec4(0.0);
  float bw = 0.055;
  float cell = floor(u.x / bw);
  float fx = fract(u.x / bw);
  float hb = hash21(vec2(cell, 3.1));
  float top = -0.5 + 0.07 + 0.07 * hb;
  float tank = step(0.6, hash21(vec2(cell, 7.7))) * step(abs(fx - 0.35), 0.16);
  float topT = top + tank * 0.018;
  float bldg = 1.0 - smoothstep(topT - px, topT + px, u.y);
  float antOn = step(0.72, hash21(vec2(cell, 9.2)));
  float ant = antOn * lineMask(abs(fx - 0.78) * bw, px, px) * step(u.y, top + 0.045);
  float cov = max(bldg, ant);
  vec3 col = vec3(0.0);

  // lit windows
  vec2 wg = vec2(u.x / 0.0138, (u.y + 0.5) / 0.021);
  vec2 wid = floor(wg);
  vec2 wf = fract(wg) - 0.5;
  float litW = step(0.62, hash21(wid + 5.0));
  float tv = step(0.9, hash21(wid + 9.0));
  float win = (1.0 - smoothstep(0.22, 0.30, abs(wf.x))) * (1.0 - smoothstep(0.24, 0.32, abs(wf.y)));
  float inside = step(u.y, topT - 0.014) * step(-0.49, u.y);
  vec3 wc = mix(vec3(1.0, 0.68, 0.32), vec3(0.55, 0.65, 1.0) * (0.7 + 0.3 * sin(tm * 3.0 + wid.x)), tv);
  col += wc * win * litW * inside * bldg * 0.55;

  // festival lights along some rooftops
  float lightsOn = step(0.45, hash21(vec2(cell, 4.4)));
  float bi = floor(u.x / 0.0105);
  float bx = (bi + 0.5) * 0.0105;
  float bd = length(vec2(u.x - bx, u.y - (top - 0.004)));
  float pick = mod(bi, 3.0);
  vec3 bc = pick < 0.5 ? vec3(1.0, 0.45, 0.1) : (pick < 1.5 ? vec3(1.0, 0.85, 0.3) : vec3(1.0, 0.25, 0.4));
  float onb = 0.45 + 0.55 * step(0.5, fract(bi * 0.37 - tm * 0.8));
  float bulb = lineMask(bd, max(0.0028, px * 1.2), px * 1.2) + 0.0025 / (bd + 0.0025) * 0.25;
  col += bc * bulb * onb * lightsOn * step(abs(u.x - bx), 0.0105);

  if (mode == 0) {
    // temple with a curved shikhara spire, kalash and flag
    float x0 = asp * 0.24;
    float baseY = -0.5 + 0.13;
    float spH = 0.16;
    float yn = clamp((u.y - baseY) / spH, 0.0, 1.0);
    float w = 0.052 * pow(1.0 - yn, 0.75);
    float spire = step(abs(u.x - x0), w) * step(baseY - 0.001, u.y) * step(u.y, baseY + spH);
    float plinth = step(abs(u.x - x0), 0.075) * step(u.y, baseY);
    float kalash = lineMask(length(u - vec2(x0, baseY + spH + 0.007)), 0.007, px);
    float pole = lineMask(abs(u.x - x0), px * 0.8, px) * step(baseY + spH, u.y) * step(u.y, baseY + spH + 0.05);
    float fy = u.y - (baseY + spH + 0.038);
    float flag = step(0.0, u.x - x0) * step(u.x - x0, 0.028) * step(abs(fy), 0.011 * (1.0 - (u.x - x0) / 0.028));
    float mandapa = lineMask(length(vec2(u.x - (x0 - 0.075), max(u.y - (baseY - 0.01), 0.0))), 0.035, px) * step(u.y, baseY + 0.03);
    float temple = max(max(max(spire, plinth), max(kalash, pole)), mandapa);
    cov = max(cov, temple);
    col *= 1.0 - temple;
    col += vec3(1.0, 0.45, 0.08) * flag * 0.6;
    // a row of diyas on the temple steps
    float dx = fract((u.x - x0) / 0.016) - 0.5;
    float dRow = step(abs(u.x - x0), 0.07) * lineMask(length(vec2(dx * 0.016, (u.y - baseY - 0.006) * 0.8)), 0.0028, px);
    float flick = 0.8 + 0.2 * sin(tm * 9.0 + floor((u.x - x0) / 0.016) * 2.0);
    col += vec3(1.0, 0.7, 0.3) * dRow * flick;
    col += vec3(1.0, 0.55, 0.2) * exp(-abs(u.y - baseY) * 60.0) * step(abs(u.x - x0), 0.08) * 0.12;
  }
  return vec4(col, cov);
}

vec3 fireworksArt(vec2 u, float asp, float px, float tm, int pal, float density, float bsize, float smokeAmt, int skyMode, float bass, float beat) {
  vec3 col = nightSky(u, tm, px);
  vec3 fw = vec3(0.0);
  vec3 sL = vec3(0.0);
  float flashSum = 0.0;

  // Up to ten staggered launch lanes run continuously at finale pace, so rockets keep climbing
  // while earlier bursts fade. There is no loop boundary: each lane simply fires again with new choices.
  float lanePeriod = 6.4;
  float laneCount = 4.0 + floor(density * 6.0 + 0.5);

  for (int i = 0; i < 10; i++) {
    float fi = float(i);
    if (fi >= laneCount) break;
    float lt0 = tm + fi / laneCount * lanePeriod;
    float k = mod(floor(lt0 / lanePeriod), 97.0);
    float lc = fract(lt0 / lanePeriod) * lanePeriod;
    float age = lc - 0.3 * hash21(vec2(fi + 40.0, k));
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
    if (type == 2) L = 4.6;
    if (type == 4) L = 3.6;
    float rise = 1.0 + 0.35 * hB;
    if (age < 0.0 || age > rise + L) continue;
    float rmaxEst = (0.12 + 0.10 * hB) * bsize;
    float xRange = max(asp * 0.5 - rmaxEst * 0.55, 0.05);
    float hX = hash21(vec2(fi * 7.0 + 3.0, k + 13.1));
    vec2 c = vec2((hX * 2.0 - 1.0) * xRange, -0.04 + hD * 0.36);
    vec2 start = vec2(c.x + (hB - 0.5) * 0.05, -0.42);
    if (age < rise) {
      fw += rocketTrail(u, start, c, age / rise, px, tm, hA);
      continue;
    }
    float b = age - rise;
    float rmax = (0.12 + 0.10 * hB) * bsize * (1.0 + bass * 0.25);
    if (type == 2) rmax *= 1.15;
    if (type == 3) rmax *= 0.9;
    vec3 c1 = shellColor(pal, hE);
    vec3 c2 = shellColor(pal, fract(hE + 0.4));
    if (type == 2 || type == 4) c1 = vec3(1.0, 0.68, 0.28);
    fw += shellLight(u, c, b, L, type, rmax, c1, c2, hA * 7.0 + k, px, tm);
    float fl = exp(-b * 7.0);
    flashSum += fl;
    sL += c1 * (exp(-b * 1.1) * 0.6 + fl * 1.5) * exp(-length(u - c) * 3.5);
  }

  // drifting smoke that builds up over the show and glows with the bursts
  float sm = smokeAmt * (0.8 + 0.2 * sin(tm * 0.05));
  vec2 drift = 4.0 * vec2(cos(tm * 0.02), sin(tm * 0.02)) + vec2(0.0, sin(tm * 0.05) * 0.3);
  float dens = fbm3(u * vec2(2.2, 3.0) + drift);
  dens = smoothstep(0.35, 0.85, dens) * smoothstep(-0.4, 0.1, u.y);
  col += (vec3(0.012, 0.012, 0.018) + sL * 0.35 + vec3(0.6, 0.5, 0.4) * flashSum * 0.02) * dens * sm;
  col += fw * (1.0 + beat * 0.25);
  col += vec3(0.02, 0.018, 0.025) * min(flashSum, 2.0) * 0.5;

  vec4 sk = skyline(u, asp, skyMode, tm, px);
  col = mix(col, sk.rgb, sk.a);
  return col;
}

vec3 calibInner(vec2 u, float pxu) {
  vec3 col = vec3(0.02);
  vec2 g = u * 16.5;
  vec2 gd = 0.5 - abs(fract(g) - 0.5);
  float gl = max(lineMask(gd.x, 0.0, pxu * 16.5 * 1.5), lineMask(gd.y, 0.0, pxu * 16.5 * 1.5));
  col += vec3(0.35) * gl;
  float crossM = max(lineMask(abs(u.x), pxu, pxu), lineMask(abs(u.y), pxu, pxu));
  col = mix(col, vec3(1.0), crossM);
  return col;
}

void main() {
  vec2 uv = gl_FragCoord.xy / RENDERSIZE.xy;
  vec2 pin = (uv - 0.5) * vec2(13.75, 18.5);
  float px = max(13.75 / RENDERSIZE.x, 18.5 / RENDERSIZE.y);

  // Read every control here in main and pass it down.
  float tm = TIME * speed;
  int pal = int(colorMode);
  float density = showDensity;
  float bsize = burstSize;
  float smokeAmt = smokeAmount;
  int skyMode = int(skylineMode);
  float drive = audioDrive;
  float mB = matBrightness;
  float mW = matWarmth;
  float oTrim = outerTrim;
  float iTrim = innerTrim;
  bool cal = calibrate;
  float ex = exposure;

  float bass = max(audioBass, 0.2 + 0.08 * sin(TIME * 0.7)) * drive;
  float beat = audioBeat * drive;

  vec2 ho = vec2(6.875, 9.25) - vec2(oTrim);
  vec2 hi = vec2(5.875, 8.25) + vec2(iTrim);
  float dOut = max(abs(pin.x) - ho.x, abs(pin.y) - ho.y);
  float dIn = max(abs(pin.x) - hi.x, abs(pin.y) - hi.y);

  vec3 col = vec3(0.0);
  if (dIn < px) {
    vec2 u = pin / (2.0 * hi.y);
    float asp = hi.x / hi.y;
    float pxu = px / (2.0 * hi.y);
    if (cal) {
      col = calibInner(u, pxu);
    } else {
      col = fireworksArt(u, asp, pxu, tm, pal, density, bsize, smokeAmt, skyMode, bass, beat) * ex;
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
