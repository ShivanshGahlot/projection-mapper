/*{
  "DESCRIPTION": "8x10 portrait frame (10x12 mount). Anar flower-pot fireworks in a festive courtyard, firing continuously: fountains of sparks that surge and breathe, arc, cool and bounce on the ground, with glowing embers and rising smoke. Projects a clean white band on the 1-inch mat.",
  "CREDIT": "Created for Ghost Arcade",
  "ISFVSN": "2",
  "CATEGORIES": ["Generator", "Audio Reactive", "Projection Mapping"],
  "INPUTS": [
    {"NAME": "colorMode", "TYPE": "long", "DEFAULT": 0, "VALUES": [0, 1, 2, 3], "LABELS": ["Classic Gold", "Silver Sparkle", "Colour Anar", "Emerald"], "LABEL": "Spark Colours"},
    {"NAME": "speed", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.25, "MAX": 4.0, "LABEL": "Speed"},
    {"NAME": "potCount", "TYPE": "long", "DEFAULT": 3, "VALUES": [1, 2, 3], "LABELS": ["One Pot", "Two Pots", "Three Pots"], "LABEL": "Pots"},
    {"NAME": "fountainHeight", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.6, "MAX": 1.3, "LABEL": "Fountain Height"},
    {"NAME": "sparkDensity", "TYPE": "float", "DEFAULT": 0.7, "MIN": 0.0, "MAX": 1.0, "LABEL": "Spark Density"},
    {"NAME": "smokeAmount", "TYPE": "float", "DEFAULT": 0.5, "MIN": 0.0, "MAX": 1.0, "LABEL": "Smoke"},
    {"NAME": "bgBrightness", "TYPE": "float", "DEFAULT": 0.6, "MIN": 0.0, "MAX": 1.0, "LABEL": "Background Brightness"},
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

const float GRAV = 2.0;

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

// Spark colour as it cools: hot white, then the palette's tone, then ember.
vec3 sparkColor(int pal, float age, float h) {
  vec3 hot = vec3(1.0, 0.97, 0.88);
  vec3 midC = vec3(1.0, 0.74, 0.30);
  vec3 endC = vec3(1.0, 0.36, 0.08);
  if (pal == 1) { midC = vec3(0.80, 0.88, 1.0); endC = vec3(0.45, 0.60, 1.0); }
  if (pal == 2) {
    float i = floor(h * 4.0);
    endC = i < 0.5 ? vec3(0.30, 1.0, 0.40) : (i < 1.5 ? vec3(1.0, 0.22, 0.32) : (i < 2.5 ? vec3(1.0, 0.30, 0.90) : vec3(0.35, 0.55, 1.0)));
    midC = mix(vec3(1.0, 0.8, 0.4), endC, 0.5);
  }
  if (pal == 3) { midC = vec3(0.65, 1.0, 0.35); endC = vec3(0.15, 0.85, 0.30); }
  vec3 c = mix(hot, midC, smoothstep(0.0, 0.18, age));
  return mix(c, endC, smoothstep(0.35, 0.8, age));
}

// The pots fire continuously; their output breathes with slow surges instead of burning out.
float potIntensity(float lt) {
  return 0.9 + 0.1 * sin(lt * 0.7);
}

void potLayout(int i, int cnt, out vec2 base, out float scl, out float offs) {
  base = vec2(0.0, -0.40);
  scl = 1.15;
  offs = 0.0;
  if (cnt == 2) {
    base = i == 0 ? vec2(-0.15, -0.38) : vec2(0.15, -0.41);
    scl = 1.05;
    offs = float(i) * 0.5;
  }
  if (cnt == 3) {
    if (i == 0) { base = vec2(0.0, -0.31); scl = 0.8; }
    if (i == 1) { base = vec2(-0.21, -0.41); scl = 1.0; }
    if (i == 2) { base = vec2(0.21, -0.39); scl = 0.95; }
    offs = float(i) / 3.0;
  }
}

vec3 fountain(vec2 u, vec2 base, float scl, float lt, float k, float pid, float px, int pal, float hMul, float nPart, float dens, float bass, float high, float tm) {
  vec3 col = vec3(0.0);
  float surge = 1.0 + 0.12 * sin(lt * 5.3 + pid) + 0.08 * sin(lt * 11.7 + pid * 2.0);
  float iNow = potIntensity(lt);
  float ival = iNow;
  vec2 tip = base + vec2(0.0, 0.07 * scl);
  float groundY = base.y;
  float hh = 0.50 * scl * hMul * (0.92 + 0.16 * bass);
  float v0 = sqrt(2.0 * GRAV * hh);

  float reach = v0 * 0.75 + 0.06;
  if (abs(u.x - tip.x) > reach || u.y < groundY - 0.03 || u.y > tip.y + hh * 1.15 + 0.03) return col;
  vec2 d = u - tip;

  // luminous jet core
  float coreW = 0.012 + 0.09 * max(d.y, 0.0);
  float jet = exp(-abs(d.x) / coreW) * smoothstep(-0.012, 0.01, d.y) * (1.0 - smoothstep(hh * 0.35, hh * 1.0, d.y));
  vec3 jetCol = mix(vec3(1.0, 0.95, 0.85), sparkColor(pal, 0.3, pid * 0.31 + 0.1), 0.6);
  col += jetCol * jet * (0.5 + 0.6 * dens) * ival * surge;
  col += vec3(1.0, 0.95, 0.85) * exp(-length(d) * 70.0) * ival;

  // dense crackle of tiny sparks inside the spray
  float envW = 0.02 + 0.55 * clamp(d.y, 0.0, hh) + 0.25 * max(hh * 0.5 - d.y, 0.0) * step(d.y, 0.0);
  float envMask = (1.0 - smoothstep(0.6, 1.0, abs(d.x) / envW)) * step(-0.01, d.y) * (1.0 - smoothstep(hh * 0.85, hh * 1.1, d.y));
  vec2 g = d * 150.0;
  vec2 gid = floor(g);
  float slot = mod(floor(tm * 25.0), 997.0);
  float sh = hash21(gid + vec2(slot, pid * 13.0));
  float prob = envMask * ival * (0.04 + 0.30 * dens + 0.25 * high) * (1.0 - 0.6 * clamp(d.y / hh, 0.0, 1.0));
  float dotM = lineMask(length(fract(g) - 0.5), 0.14, 0.2) * step(1.0 - prob, sh);
  col += sparkColor(pal, 0.25 + 0.5 * hash21(gid + 3.0), sh) * dotM * 1.2;

  // tracked sparks: arcs, cooling colour, streaks, one bounce on the ground
  for (int j = 0; j < 32; j++) {
    float fj = float(j);
    if (fj >= nPart) break;
    float h1 = hash21(vec2(fj, pid + 0.5));
    float rate = 0.75 + 0.3 * h1;
    float em = lt * rate + h1;
    float cj = mod(floor(em), 997.0);
    float pe = fract(em);
    float h2 = hash21(vec2(fj + 3.0, cj + pid * 7.0 + k));
    float h3 = hash21(vec2(cj + 1.7, fj + pid * 3.0 + k));
    float emitT = lt - pe / rate;
    float ie = potIntensity(emitT);
    if (h3 > ie * 1.05) continue;
    float tt = pe / rate;
    float ang = (h2 - 0.5) * 1.05;
    float v = v0 * (0.68 + 0.32 * fract(h2 * 7.13)) * sqrt(max(ie, 0.05));
    vec2 vel0 = vec2(sin(ang), cos(ang)) * v;
    vec2 pos = tip + vel0 * tt - vec2(0.0, 0.5 * GRAV * tt * tt);
    vec2 vel = vel0 - vec2(0.0, GRAV * tt);
    float bounced = 0.0;
    if (pos.y < groundY) {
      float th = (vel0.y + sqrt(vel0.y * vel0.y + 2.0 * GRAV * (tip.y - groundY))) / GRAV;
      vec2 hitP = vec2(tip.x + vel0.x * th, groundY);
      float vyHit = vel0.y - GRAV * th;
      vec2 bv = vec2(vel0.x * 0.55, -vyHit * 0.3);
      float t2 = tt - th;
      pos = hitP + bv * t2 - vec2(0.0, 0.5 * GRAV * t2 * t2);
      vel = bv - vec2(0.0, GRAV * t2);
      if (pos.y < groundY) {
        pos.y = groundY;
        vel = vec2(bv.x * 0.3, 0.0);
      }
      bounced = 1.0;
    }
    float fade = 1.0 - smoothstep(0.65, 1.0, pe);
    vec2 back = pos - vel * 0.022;
    float hs = 0.0;
    float ds = segDist(u, pos, back, hs);
    float w = max(0.0016, px * 1.1);
    float streak = lineMask(ds, w, px * 1.4) * (1.0 - 0.7 * hs);
    float hl = length(u - pos);
    float headGlow = 0.0025 / (hl + 0.0025) * 0.18 * (1.0 - smoothstep(0.0, 0.025, hl));
    vec3 sc = sparkColor(pal, pe, h2);
    col += sc * (streak * 1.3 + headGlow) * fade * (1.0 - 0.4 * bounced);
  }

  // embers glowing on the ground around the pot
  float exE = (u.x - tip.x) * 90.0;
  float eid = floor(exE);
  float eh = hash21(vec2(eid, k + pid * 5.0 + mod(floor(lt * 2.0), 997.0)));
  vec2 eq = vec2(fract(exE) - 0.5, (u.y - groundY - 0.002) * 90.0 * 2.0);
  float ember = lineMask(length(eq), 0.12, 0.15) * step(0.8, eh) * step(abs(u.x - tip.x), 0.16);
  float eFlick = 0.6 + 0.4 * sin(tm * 6.0 + eid * 3.0);
  col += vec3(1.0, 0.35, 0.08) * ember * eFlick;
  return col;
}

vec4 potShape(vec2 u, vec2 base, float scl, float lt, float k, float pid, float px) {
  float ph = 0.07 * scl;
  vec2 q = u - base;
  float yn = q.y / ph;
  float hw = mix(0.042, 0.011, clamp(yn, 0.0, 1.0)) * scl;
  float inside = step(0.0, yn) * step(yn, 1.0) * (1.0 - smoothstep(hw - px, hw + px, abs(q.x)));
  float hc = fract(pid / 3.0 + 0.1);
  vec3 cA = hc < 0.33 ? vec3(1.00, 0.16, 0.20) : (hc < 0.66 ? vec3(0.18, 0.78, 0.32) : vec3(0.28, 0.40, 1.00));
  vec3 cB = vec3(1.00, 0.84, 0.28);
  float band = mod(floor(yn * 4.0), 2.0);
  vec3 col = band < 0.5 ? cA : cB;
  float dia = abs(fract(q.x / (0.012 * scl)) - 0.5) + abs(fract(yn * 6.0) - 0.5);
  col = mix(col, band < 0.5 ? cB : cA, step(dia, 0.22) * 0.8);
  float xn = clamp(q.x / max(hw, 0.0001), -1.0, 1.0);
  float cyl = sqrt(max(1.0 - xn * xn, 0.0));
  float iNow = potIntensity(lt);
  col *= (0.62 + 0.45 * cyl) * (0.95 + 0.5 * iNow * smoothstep(0.3, 1.0, yn));
  float rim = smoothstep(0.75, 1.0, abs(xn));
  col = mix(col, vec3(1.0, 0.85, 0.55), rim * 0.25 * smoothstep(0.2, 1.0, yn));
  return vec4(col, inside);
}

// Courtyard: a warm plaster wall with a painted border and a lamp-lit niche, a tiled floor,
// a marigold strand and glowing garland lights. Fountain light falls on wall and floor.
vec3 courtyard(vec2 u, float tm, float px, float bgB, vec3 potLight) {
  float horizon = -0.26;
  float n1 = vnoise(u * 30.0);
  float n2 = vnoise(u * 6.0 + 4.0);
  float tex = 0.85 + 0.15 * n1 + 0.12 * (n2 - 0.5);
  vec3 col = vec3(0.0);
  if (u.y > horizon) {
    vec3 plaster = vec3(0.46, 0.28, 0.17) * tex;
    float ambient = 0.22 + 0.08 * smoothstep(0.5, -0.2, u.y);
    col = plaster * ambient;
    // painted dado border just above the floor
    float dado = step(u.y, horizon + 0.055);
    vec3 dadoCol = vec3(0.50, 0.10, 0.07) * tex * ambient * 1.2;
    float zig = abs(fract(u.x / 0.03) - 0.5) * 0.03;
    float zigLine = lineMask(abs(u.y - (horizon + 0.03) - zig * 0.6), 0.0018, px);
    col = mix(col, dadoCol, dado);
    col = mix(col, vec3(0.9, 0.65, 0.2) * ambient * 2.2, zigLine * dado);
    col = mix(col, vec3(0.9, 0.65, 0.2) * ambient * 2.0, lineMask(abs(u.y - (horizon + 0.055)), 0.0015, px));
    // arched niche with a small lamp
    vec2 nq = vec2(abs(u.x) - 0.29, u.y - 0.06);
    float nw = 0.045;
    float rectN = step(abs(nq.x), nw) * step(-0.07, nq.y) * step(nq.y, 0.03);
    float archN = 1.0 - smoothstep(nw - px, nw + px, length(nq - vec2(0.0, 0.03)));
    float niche = max(rectN, archN * step(0.03, nq.y));
    float lampGlow = exp(-length(nq - vec2(0.0, -0.05)) * 18.0);
    col = mix(col, plaster * ambient * 0.35 + vec3(1.0, 0.6, 0.25) * lampGlow * 0.35, niche);
    float flick = 0.85 + 0.15 * sin(tm * 8.0) * sin(tm * 5.3);
    col += vec3(1.0, 0.8, 0.45) * lineMask(length((nq - vec2(0.0, -0.058)) * vec2(1.0, 0.6)), 0.006, px) * niche * flick;
    col += plaster * potLight * 0.55;
  } else {
    float dz = horizon - u.y + 0.03;
    float zf = 0.09 / dz;
    vec2 tq = vec2(u.x * zf * 9.0, zf * 3.0);
    vec2 tg = abs(fract(tq) - 0.5);
    float grout = max(lineMask(0.5 - tg.x, 0.0, 0.04 * zf), lineMask(0.5 - tg.y, 0.0, 0.04 * zf));
    vec3 stone = vec3(0.34, 0.24, 0.17) * tex * (0.9 + 0.1 * hash21(floor(tq)));
    col = stone * (0.10 + 0.06 * smoothstep(-0.5, horizon, u.y));
    col *= 1.0 - grout * 0.5;
    col += stone * potLight * 0.8;
  }
  col *= bgB * 1.6;

  // marigold strand along the top
  float my0 = 0.47;
  float mxn = u.x / 0.42;
  float mcy = my0 - 0.035 * (1.0 - mxn * mxn);
  float mi = floor(u.x / 0.022);
  float mbx = (mi + 0.5) * 0.022;
  float mbxn = mbx / 0.42;
  float mby = my0 - 0.035 * (1.0 - mbxn * mbxn);
  float md = length(u - vec2(mbx, mby));
  float bead = 1.0 - smoothstep(0.009, 0.011, md);
  vec3 mc = mod(mi, 2.0) < 0.5 ? vec3(1.0, 0.5, 0.05) : vec3(1.0, 0.75, 0.10);
  float petals = 0.75 + 0.25 * vnoise(u * 400.0);
  col = mix(col, mc * petals * (0.35 + 0.4 * bgB), bead);
  float leaf = lineMask(abs(u.y - mcy + 0.016), 0.003, px) * step(0.5, fract(u.x / 0.044 + 0.25)) * (1.0 - bead);
  col = mix(col, vec3(0.10, 0.35, 0.10) * (0.4 + 0.4 * bgB), leaf);

  // garland lights, softly out of focus
  for (int s = 0; s < 2; s++) {
    float fs = float(s);
    float y0 = 0.38 - fs * 0.09;
    float sag = 0.05;
    float bi = floor(u.x / 0.05 + fs * 0.5);
    float bx = (bi + 0.5 - fs * 0.5) * 0.05;
    float bxn = bx / 0.42;
    float by = y0 - sag * (1.0 - bxn * bxn) - 0.01;
    float dd = length(u - vec2(bx, by));
    float pick = mod(bi + fs, 3.0);
    vec3 bc = pick < 0.5 ? vec3(1.0, 0.55, 0.15) : (pick < 1.5 ? vec3(1.0, 0.8, 0.35) : vec3(1.0, 0.35, 0.45));
    float tw = 0.7 + 0.3 * sin(tm * 1.3 + bi * 1.7 + fs);
    float bright = 0.25 + 0.45 * bgB;
    col += bc * ((1.0 - smoothstep(0.006, 0.016, dd)) * bright + exp(-dd * 40.0) * bright * 0.4) * tw;
  }
  return col;
}

vec3 anarArt(vec2 u, float asp, float px, float tm, int pal, int cnt, float hMul, float density, float smokeAmt, float bgB, float bass, float high) {
  float nPart = floor(6.0 + 26.0 * density);

  // floor glow and smoke first, sparks over them, pots on top
  vec3 sparks = vec3(0.0);
  vec3 potLight = vec3(0.0);
  float smokeW = 0.0;
  vec3 smokeLight = vec3(0.0);
  for (int i = 0; i < 3; i++) {
    if (i >= cnt) break;
    vec2 base = vec2(0.0);
    float scl = 1.0;
    float offs = 0.0;
    potLayout(i, cnt, base, scl, offs);
    float pid = float(i);
    float k = float(i) * 3.0 + 1.0;
    float lt = tm + offs * 13.0;
    float iv = potIntensity(lt);
    vec2 lp = base + vec2(0.0, 0.07 * scl + 0.12 * scl * hMul);
    vec2 rel = (u - lp) * vec2(1.0, u.y < -0.26 ? 2.4 : 1.0);
    vec3 tint = mix(vec3(1.0, 0.62, 0.28), sparkColor(pal, 0.4, pid * 0.31 + 0.1), 0.7);
    potLight += tint * iv * exp(-length(rel) * 4.0) * scl;
    sparks += fountain(u, base, scl, lt, k, pid, px, pal, hMul, nPart, density, bass, high, tm);
    vec2 d = u - base - vec2(0.0, 0.07 * scl);
    float plume = exp(-abs(d.x) / (0.04 + 0.35 * max(d.y, 0.0))) * smoothstep(0.0, 0.12, d.y) * (1.0 - smoothstep(0.35, 0.9, d.y));
    smokeW += plume;
    smokeLight += tint * plume * iv;
  }

  vec3 col = courtyard(u, tm, px, bgB, potLight);

  // smoke drifts upward forever without a loop point (two offset layers cross-fade)
  float w = fract(tm * 0.05);
  vec2 sp = u * vec2(5.0, 3.0);
  float n1 = fbm3(sp + vec2(0.0, -w * 6.0));
  float n2 = fbm3(sp + vec2(3.7, -fract(w + 0.5) * 6.0 + 5.1));
  float blend = abs(2.0 * w - 1.0);
  float sn = mix(n1, n2, blend);
  float smk = smoothstep(0.3, 0.75, sn) * min(smokeW * 1.6, 1.5) * smokeAmt;
  col += (vec3(0.10, 0.095, 0.10) + smokeLight * 0.45) * smk;

  col += sparks;

  for (int i = 0; i < 3; i++) {
    if (i >= cnt) break;
    vec2 base = vec2(0.0);
    float scl = 1.0;
    float offs = 0.0;
    potLayout(i, cnt, base, scl, offs);
    float k = float(i) * 3.0 + 1.0;
    float lt = tm + offs * 13.0;
    vec4 pot = potShape(u, base, scl, lt, k, float(i), px);
    col = mix(col, pot.rgb, pot.a);
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
  vec2 pin = (uv - 0.5) * vec2(10.0, 12.0);
  float px = max(10.0 / RENDERSIZE.x, 12.0 / RENDERSIZE.y);

  // Read every control here in main and pass it down.
  float tm = TIME * speed;
  int pal = int(colorMode);
  int cnt = int(potCount);
  float hMul = fountainHeight;
  float density = sparkDensity;
  float smokeAmt = smokeAmount;
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

  vec2 ho = vec2(5.0, 6.0) - vec2(oTrim);
  vec2 hi = vec2(4.0, 5.0) + vec2(iTrim);
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
      col = anarArt(u, asp, pxu, tm, pal, cnt, hMul, density, smokeAmt, bgB, bass, high) * ex;
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
