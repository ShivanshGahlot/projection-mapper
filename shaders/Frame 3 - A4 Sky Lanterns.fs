/*{
  "DESCRIPTION": "A4 portrait frame (10.3x13.7 mount). Paper sky lanterns rise continuously in three depths over a festive rooftop terrace with string lights, diyas and a swaying akash kandil. Near lanterns are soft and close, far ones shrink as they drift away. Projects a clean white band on the 1-inch mat.",
  "CREDIT": "Created for Ghost Arcade",
  "ISFVSN": "2",
  "CATEGORIES": ["Generator", "Audio Reactive", "Projection Mapping"],
  "INPUTS": [
    {"NAME": "colorMode", "TYPE": "long", "DEFAULT": 0, "VALUES": [0, 1, 2, 3], "LABELS": ["Classic Saffron", "Marigold Mix", "Rose and Gold", "Festive Mix"], "LABEL": "Lantern Colours"},
    {"NAME": "speed", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.25, "MAX": 4.0, "LABEL": "Speed"},
    {"NAME": "lanternDensity", "TYPE": "float", "DEFAULT": 0.7, "MIN": 0.0, "MAX": 1.0, "LABEL": "Number of Lanterns"},
    {"NAME": "lanternSize", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.7, "MAX": 1.4, "LABEL": "Lantern Size"},
    {"NAME": "windAmount", "TYPE": "float", "DEFAULT": 0.4, "MIN": -1.0, "MAX": 1.0, "LABEL": "Wind (left / right)"},
    {"NAME": "bokehAmount", "TYPE": "float", "DEFAULT": 0.5, "MIN": 0.0, "MAX": 1.0, "LABEL": "City Bokeh"},
    {"NAME": "terraceMode", "TYPE": "long", "DEFAULT": 0, "VALUES": [0, 1], "LABELS": ["Festive Terrace", "Sky Only"], "LABEL": "Foreground"},
    {"NAME": "audioDrive", "TYPE": "float", "DEFAULT": 0.4, "MIN": 0.0, "MAX": 1.0, "LABEL": "Audio Drive"},
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

float hash21(vec2 p) {
  p = fract(p * vec2(123.34, 456.21));
  p += dot(p, p + 45.32);
  return fract(p.x * p.y);
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
    float y = layer > 1.5 ? mix(-0.66, 0.66, ph) : mix(-0.46, 0.62, ph);
    float drift = windAmt * (ph - 0.5) * 0.35;
    float sway = sin(tm * 0.55 + fi * 1.7 + layer) * (0.006 + 0.01 * layer);
    float x = (hx * 2.0 - 1.0) * (asp * 0.5 - 0.03) + drift + sway;
    float sc = s0 * sizeMul * (1.0 - 0.4 * ph) * (0.85 + 0.3 * h0);
    float tilt = sin(tm * 0.8 + fi * 1.3) * 0.07 - windAmt * 0.08;
    float fade = smoothstep(0.0, 0.05, ph) * (1.0 - smoothstep(0.85, 1.0, ph));
    vec2 c = vec2(x, y);
    vec2 dp = u - c;
    if (dot(dp, dp) > (sc * 5.0) * (sc * 5.0)) continue;
    float flick = (0.86 + 0.14 * sin(TIME * 7.0 + fi * 2.3) * sin(TIME * 4.3 + fi)) * (1.0 + bass * 0.3);
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

// Akash kandil: an eight-pointed paper star lantern hanging from a pole, with tassels.
vec4 kandil(vec2 u, vec2 pivot, float len, float tm, vec3 paper, float px, out vec3 glow) {
  float ang = sin(tm * 0.9) * 0.07 + sin(tm * 0.37) * 0.03;
  vec2 dirv = vec2(sin(ang), -cos(ang));
  vec2 c = pivot + dirv * len;
  float h = 0.0;
  float ds = segDist(u, pivot, c - dirv * 0.03, h);
  float str = lineMask(ds, max(0.0012, px * 0.8), px);
  vec2 q = u - c;
  float cs = cos(ang);
  float sn = sin(ang);
  q = vec2(cs * q.x + sn * q.y, -sn * q.x + cs * q.y);
  float r = length(q);
  float a = atan(q.y, q.x);
  float rr = 0.05;
  float edge = rr * (0.58 + 0.42 * pow(abs(cos(a * 4.0)), 2.0));
  float d = r - edge;
  float body = 1.0 - smoothstep(-px, px, d);
  float lum = 0.55 + 0.9 * exp(-r / rr * 2.5);
  vec3 col = paper * lum;
  col = mix(col, vec3(1.0, 0.85, 0.35), lineMask(abs(r - edge * 0.55), 0.0018, px) * 0.8);
  col = mix(col, vec3(1.0, 0.9, 0.6), lineMask(abs(d), 0.0016, px) * 0.6);
  float tas = 0.0;
  for (int t = 0; t < 3; t++) {
    float tx = (float(t) - 1.0) * 0.016;
    float sway = sin(tm * 2.1 + float(t)) * 0.004;
    float th = 0.0;
    float dt = segDist(q, vec2(tx, -rr * 0.55), vec2(tx + sway, -rr * 0.55 - 0.05), th);
    tas = max(tas, lineMask(dt, max(0.0016, px), px) * (1.0 - th * 0.5));
  }
  vec3 tasCol = vec3(1.0, 0.75, 0.25) * 0.8;
  glow = paper * exp(-max(d, 0.0) * 18.0) * 0.35 + paper * exp(-r * 9.0) * 0.08;
  vec3 outc = col * body;
  outc = mix(outc, tasCol, tas * (1.0 - body));
  outc = mix(outc, vec3(0.02), str * (1.0 - body));
  return vec4(outc, max(body, max(tas, str)));
}

vec3 nightSky(vec2 u, float tm, float px) {
  float hgt = clamp(u.y + 0.5, 0.0, 1.0);
  vec3 col = mix(vec3(0.055, 0.028, 0.025), vec3(0.006, 0.007, 0.028), smoothstep(0.15, 0.75, hgt));
  vec2 sq = u * 45.0;
  float sh = hash21(floor(sq) + 21.0);
  float tw = 0.55 + 0.45 * sin(tm * 1.5 + sh * 50.0);
  float star = step(0.965, sh) * lineMask(length(fract(sq) - 0.5), 0.05, max(px * 65.0, 0.06));
  col += vec3(0.9, 0.92, 1.0) * star * tw * 0.35 * smoothstep(0.35, 0.7, hgt);
  return col;
}

vec3 cityBokeh(vec2 u, float tm, float amount) {
  vec3 col = vec3(0.0);
  vec2 g = u * vec2(10.0, 12.0) + vec2(0.0, 0.3);
  vec2 id = floor(g);
  vec2 f = fract(g) - 0.5;
  float h = hash21(id + 31.0);
  vec2 off = vec2(hash21(id + 2.0), hash21(id + 5.0)) - 0.5;
  float rad = 0.25 + 0.2 * hash21(id + 9.0);
  float dd = length(f - off * 0.4);
  float disc = 1.0 - smoothstep(rad * 0.6, rad, dd);
  float band = smoothstep(-0.42, -0.30, u.y) * (1.0 - smoothstep(-0.18, -0.02, u.y));
  float on = step(0.62, h);
  vec3 bc = h > 0.85 ? vec3(1.0, 0.45, 0.6) : (h > 0.7 ? vec3(1.0, 0.8, 0.45) : vec3(1.0, 0.6, 0.25));
  float tw = 0.75 + 0.25 * sin(tm * 0.8 + h * 30.0);
  float soft = 1.0 - smoothstep(0.0, rad, dd);
  col += bc * (disc * 0.07 + soft * soft * 0.10) * on * band * tw * amount;
  return col;
}

// Distant rooftops, then the near festive terrace. Returns colour and coverage for each.
vec4 distantRoofs(vec2 u, float tm, float px) {
  float bw = 0.045;
  float cell = floor(u.x / bw);
  float top = -0.5 + 0.13 + 0.06 * hash21(vec2(cell, 3.3));
  float cov = 1.0 - smoothstep(top - px, top + px, u.y);
  vec2 wg = vec2(u.x / 0.012, (u.y + 0.5) / 0.018);
  vec2 wid = floor(wg);
  vec2 wf = fract(wg) - 0.5;
  float lit = step(0.7, hash21(wid + 5.0));
  float win = (1.0 - smoothstep(0.2, 0.3, abs(wf.x))) * (1.0 - smoothstep(0.22, 0.32, abs(wf.y)));
  vec3 col = vec3(1.0, 0.66, 0.30) * win * lit * step(u.y, top - 0.012) * 0.32;
  col += vec3(0.008, 0.006, 0.01);
  return vec4(col * cov, cov);
}

vec4 terrace(vec2 u, float asp, float tm, float px, int pal, out vec3 glow) {
  glow = vec3(0.0);
  float base = -0.5;
  float yt = base + 0.10;
  float cov = 1.0 - smoothstep(yt - px, yt + px, u.y);
  float bal = abs(fract(u.x / 0.022) - 0.5);
  float inRail = step(base + 0.035, u.y) * step(u.y, yt - 0.016);
  float pillar = step(abs(fract(u.x / 0.2 + 0.5) - 0.5) * 0.2, 0.012);
  float hole = inRail * step(0.2, bal) * (1.0 - pillar);
  cov *= 1.0 - hole;
  vec3 col = vec3(0.0);

  // diyas along the parapet
  float dc = floor(u.x / 0.07);
  float dxc = (dc + 0.5) * 0.07;
  vec2 dq = u - vec2(dxc, yt + 0.006);
  float flick = 0.85 + 0.15 * sin(tm * 9.0 + dc * 2.1);
  float flame = lineMask(length(dq * vec2(1.0, 0.6)), 0.0035, px) * flick;
  float cup = lineMask(length(vec2(dq.x, min(dq.y + 0.004, 0.0)) * vec2(1.0, 2.0)), 0.006, px) * step(dq.y, -0.002);
  col += vec3(1.0, 0.75, 0.35) * flame;
  col = mix(col, vec3(0.25, 0.08, 0.03), cup);
  glow += vec3(1.0, 0.55, 0.2) * exp(-length(dq) * 45.0) * 0.25 * flick;
  cov = max(cov, max(flame, cup));

  // poles and the string of lights between them
  float lx = -asp * 0.45;
  float rx = asp * 0.36;
  float poleTop = base + 0.34;
  float poles = (lineMask(abs(u.x - lx), 0.004, px) + lineMask(abs(u.x - rx), 0.004, px)) * step(u.y, poleTop) * step(yt - 0.01, u.y);
  float armH = 0.0;
  float arm = lineMask(segDist(u, vec2(rx, poleTop - 0.01), vec2(rx - 0.07, poleTop - 0.01), armH), 0.003, px);
  cov = max(cov, max(min(poles, 1.0), arm));
  col *= 1.0 - max(min(poles, 1.0), arm);

  float anchorY = base + 0.29;
  float span = (rx - lx) * 0.5;
  float midX = (rx + lx) * 0.5;
  float xn = (u.x - midX) / span;
  float sagY = anchorY - 0.07 * (1.0 - xn * xn);
  float inSpan = step(abs(xn), 1.0);
  float wire = lineMask(abs(u.y - sagY), max(0.001, px * 0.7), px) * inSpan;
  col = mix(col, vec3(0.05), wire);
  cov = max(cov, wire * 0.8);
  float bi = floor(u.x / 0.024);
  float bx = (bi + 0.5) * 0.024;
  float bxn = (bx - midX) / span;
  float by = anchorY - 0.07 * (1.0 - bxn * bxn) - 0.006;
  float bd = length(u - vec2(bx, by));
  float pick = mod(bi, 4.0);
  vec3 bc = pick < 0.5 ? vec3(1.0, 0.4, 0.1) : (pick < 1.5 ? vec3(1.0, 0.85, 0.3) : (pick < 2.5 ? vec3(1.0, 0.3, 0.5) : vec3(0.5, 1.0, 0.5)));
  float chase = 0.4 + 0.6 * (0.5 + 0.5 * sin(bi * 1.1 - tm * 2.5));
  float bulb = lineMask(bd, max(0.0035, px * 1.2), px * 1.2) * step(abs(bxn), 1.0);
  col += bc * bulb * chase;
  cov = max(cov, bulb);
  glow += bc * 0.004 / (bd + 0.004) * 0.18 * chase * step(abs(bxn), 1.0);

  // hanging akash kandil
  vec3 kg = vec3(0.0);
  vec4 kd = kandil(u, vec2(rx - 0.065, poleTop - 0.01), 0.075, tm, lanternPaper(pal, 0.1), px, kg);
  col = mix(col, kd.rgb, kd.a);
  cov = max(cov, kd.a);
  glow += kg;
  return vec4(col, cov);
}

vec3 lanternArt(vec2 u, float asp, float px, float tm, int pal, float density, float sizeMul, float windAmt, float bokeh, int fg, float bass) {
  vec3 col = nightSky(u, tm, px);
  vec3 g0 = vec3(0.0);
  vec4 far = lanternLayer(u, asp, px, tm, pal, density, sizeMul, windAmt, 0.0, bass, g0);
  col = mix(col, far.rgb, far.a) + g0;
  col += cityBokeh(u, tm, bokeh);
  if (fg == 0) {
    vec4 dr = distantRoofs(u, tm, px);
    col = mix(col, dr.rgb, dr.a);
  }
  vec3 g1 = vec3(0.0);
  vec4 mid = lanternLayer(u, asp, px, tm, pal, density, sizeMul, windAmt, 1.0, bass, g1);
  col = mix(col, mid.rgb, mid.a) + g1;
  if (fg == 0) {
    vec3 tg = vec3(0.0);
    vec4 tr = terrace(u, asp, tm, px, pal, tg);
    col = mix(col, tr.rgb, tr.a) + tg;
  }
  vec3 g2 = vec3(0.0);
  vec4 near = lanternLayer(u, asp, px, tm, pal, density, sizeMul, windAmt, 2.0, bass, g2);
  col = mix(col, near.rgb, near.a) + g2;
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
  vec2 pin = (uv - 0.5) * vec2(10.3, 13.7);
  float px = max(10.3 / RENDERSIZE.x, 13.7 / RENDERSIZE.y);

  // Read every control here in main and pass it down.
  float tm = TIME * speed;
  int pal = int(colorMode);
  float density = lanternDensity;
  float sizeMul = lanternSize;
  float windAmt = windAmount;
  float bokeh = bokehAmount;
  int fg = int(terraceMode);
  float drive = audioDrive;
  float mB = matBrightness;
  float mW = matWarmth;
  float oTrim = outerTrim;
  float iTrim = innerTrim;
  bool cal = calibrate;
  float ex = exposure;

  float bass = max(audioBass, 0.2 + 0.08 * sin(TIME * 0.7)) * drive;

  vec2 ho = vec2(5.15, 6.85) - vec2(oTrim);
  vec2 hi = vec2(4.15, 5.85) + vec2(iTrim);
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
      col = lanternArt(u, asp, pxu, tm, pal, density, sizeMul, windAmt, bokeh, fg, bass) * ex;
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
