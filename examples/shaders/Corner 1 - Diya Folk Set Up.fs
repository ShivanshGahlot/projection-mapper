/*{
  "DESCRIPTION": "Diya folk corner shot 1: the crew climbs, crosses the corner and lays the wire. Add twice (Left wall / Right wall).",
  "CREDIT": "Diwali corner scene",
  "ISFVSN": "2",
  "CATEGORIES": [
    "Generator",
    "Diwali Story"
  ],
  "INPUTS": [
    {
      "NAME": "wallSide",
      "TYPE": "long",
      "DEFAULT": 2,
      "VALUES": [
        0,
        1,
        2
      ],
      "LABELS": [
        "Left wall (ledge + switch)",
        "Right wall (ladder)",
        "Preview: both walls"
      ],
      "LABEL": "Wall side"
    },
    {
      "NAME": "showGrid",
      "TYPE": "bool",
      "DEFAULT": false,
      "LABEL": "Mapping guides (border, grid, circles, corner, ledge)"
    },
    {
      "NAME": "stretch",
      "TYPE": "float",
      "DEFAULT": 1.0,
      "MIN": 0.4,
      "MAX": 2.5,
      "LABEL": "Horizontal stretch (fix squashed/fat characters)"
    },
    {
      "NAME": "ledgeHeight",
      "TYPE": "float",
      "DEFAULT": 0.74,
      "MIN": 0.4,
      "MAX": 0.95,
      "LABEL": "Left wall: ledge height (0 bottom - 1 top)"
    },
    {
      "NAME": "floorHeight",
      "TYPE": "float",
      "DEFAULT": 0.05,
      "MIN": 0.0,
      "MAX": 0.45,
      "LABEL": "Right wall: ladder base height (lower = longer ladder)"
    },
    {
      "NAME": "ladderDist",
      "TYPE": "float",
      "DEFAULT": 0.13,
      "MIN": 0.07,
      "MAX": 0.6,
      "LABEL": "Right wall: ladder distance from corner"
    },
    {
      "NAME": "switchDist",
      "TYPE": "float",
      "DEFAULT": 0.72,
      "MIN": 0.45,
      "MAX": 1.6,
      "LABEL": "Left wall: where the wire ends (distance from corner)"
    },
    {
      "NAME": "charSize",
      "TYPE": "float",
      "DEFAULT": 1.15,
      "MIN": 0.6,
      "MAX": 1.6,
      "LABEL": "Character size"
    },
    {
      "NAME": "clockMode",
      "TYPE": "long",
      "DEFAULT": 0,
      "VALUES": [
        0,
        1,
        2
      ],
      "LABELS": [
        "Sync to wall clock (both layers in step)",
        "Layer TIME",
        "Hold at preview time"
      ],
      "LABEL": "Clock"
    },
    {
      "NAME": "startOffset",
      "TYPE": "float",
      "DEFAULT": 0.0,
      "MIN": 0.0,
      "MAX": 45.0,
      "LABEL": "Start offset (s)"
    },
    {
      "NAME": "previewTime",
      "TYPE": "float",
      "DEFAULT": 12.0,
      "MIN": 0.0,
      "MAX": 45.0,
      "LABEL": "Preview time (s)"
    },
    {
      "NAME": "brightness",
      "TYPE": "float",
      "DEFAULT": 1.0,
      "MIN": 0.0,
      "MAX": 2.0,
      "LABEL": "Brightness"
    }
  ]
}*/
const float PI = 3.14159265;
const float TAU = 6.28318531;
float gPx, aa, gT;
float hash11(float n) { return fract(sin(n * 127.1 + 311.7) * 43758.5453); }
float hash21(vec2 p) { p = fract(p * vec2(123.34, 456.21)); p += dot(p, p + 45.32); return fract(p.x * p.y); }
vec2 hash22(vec2 p) { return vec2(hash21(p), hash21(p + 17.31)); }
float vnoise(vec2 p) { vec2 i = floor(p), f = fract(p); f = f * f * (3.0 - 2.0 * f); return mix(mix(hash21(i), hash21(i + vec2(1, 0)), f.x), mix(hash21(i + vec2(0, 1)), hash21(i + vec2(1, 1)), f.x), f.y); }
float fbm(vec2 p) { float s = 0.0, a = 0.5; for (int i = 0; i < 4; i++) { s += a * vnoise(p); p = p * 2.03 + 7.1; a *= 0.5; } return s; }
float sat(float x) { return clamp(x, 0.0, 1.0); }
float lin(float t, float a, float b) { return sat((t - a) / (b - a)); }
float easeIO(float x) { x = sat(x); return x * x * (3.0 - 2.0 * x); }
mat2 rot(float a) { float c = cos(a), s = sin(a); return mat2(c, s, -s, c); }
float sdBox(vec2 p, vec2 c, vec2 h) { vec2 d = abs(p - c) - h; return length(max(d, 0.0)) + min(max(d.x, d.y), 0.0); }
float sdCircle(vec2 p, vec2 c, float r) { return length(p - c) - r; }
float sdEllipse(vec2 p, vec2 c, vec2 r) { vec2 q = (p - c) / r; return (length(q) - 1.0) * min(r.x, r.y); }
float sdSeg(vec2 p, vec2 a, vec2 b) { vec2 pa = p - a, ba = b - a; float h = clamp(dot(pa, ba) / max(dot(ba, ba), 1e-9), 0.0, 1.0); return length(pa - ba * h); }
float sdTaper(vec2 p, vec2 a, vec2 b, float ra, float rb) { vec2 pa = p - a, ba = b - a; float h = clamp(dot(pa, ba) / max(dot(ba, ba), 1e-9), 0.0, 1.0); return length(pa - ba * h) - mix(ra, rb, h); }
float sdTri(vec2 p, vec2 c, float w, float h) { p -= c; p.x = abs(p.x); return max(p.x * h + p.y * w - w * h, -p.y) / length(vec2(w, h)); }
float sdTrap(vec2 p, float r1, float r2, float he) {
  vec2 k1 = vec2(r2, he), k2 = vec2(r2 - r1, 2.0 * he);
  p.x = abs(p.x);
  vec2 ca = vec2(p.x - min(p.x, (p.y < 0.0) ? r1 : r2), abs(p.y) - he);
  vec2 cb = p - k1 + k2 * clamp(dot(k1 - p, k2) / dot(k2, k2), 0.0, 1.0);
  float s = (cb.x < 0.0 && ca.y < 0.0) ? -1.0 : 1.0;
  return s * sqrt(min(dot(ca, ca), dot(cb, cb)));
}
float smin(float a, float b, float k) { float h = sat(0.5 + 0.5 * (b - a) / k); return mix(b, a, h) - k * h * (1.0 - h); }
float cov(float d) { return 1.0 - smoothstep(-aa, aa, d); }
float line(float d, float w) { return 1.0 - smoothstep(w - aa, w + aa, d); }
vec2 ik(vec2 a, vec2 c, float l1, float l2, float bend) {
  vec2 d = c - a; float L = clamp(length(d), 1e-4, l1 + l2 - 1e-4); vec2 u = d / max(length(d), 1e-4);
  float x = (l1 * l1 - l2 * l2 + L * L) / (2.0 * L); float y = sqrt(max(l1 * l1 - x * x, 0.0));
  return a + u * x + vec2(-u.y, u.x) * y * bend;
}
// petal / lotus helpers
float sdVesica(vec2 p, float r, float d) { p = abs(p); float b = sqrt(r * r - d * d); return ((p.y - b) * d > p.x * b) ? length(p - vec2(0.0, b)) : length(p - vec2(-d, 0.0)) - r; }
float flicker(float t, float s) { return 0.86 + 0.08 * sin(t * 13.0 + s) + 0.06 * sin(t * 29.7 + s * 2.3) + 0.1 * (vnoise(vec2(t * 9.0, s * 7.0)) - 0.5); }
vec3 hue6(float i) {
  float k = mod(i, 6.0);
  if (k < 0.5) return vec3(1.0, 0.18, 0.28);
  if (k < 1.5) return vec3(0.25, 1.0, 0.35);
  if (k < 2.5) return vec3(0.35, 0.55, 1.0);
  if (k < 3.5) return vec3(1.0, 0.82, 0.25);
  if (k < 4.5) return vec3(1.0, 0.3, 0.95);
  return vec3(0.3, 0.95, 1.0);
}
float sstep(float a, float b, float x) { return smoothstep(a, b, x); }
// ============================================================================
// DIYA FOLK - CORNER SHOT 1: "The crew sets up"
// One shader, two layers. The scene lives in an "unfolded" world: x = 0 is the corner,
// x > 0 is the RIGHT wall (ladder), x < 0 is the LEFT wall (ledge band above the window, switch).
// Add it twice in the mapper: one layer "Left wall", one "Right wall", pin each to its face.
// Units: model units, face height = 9 / charSize. Black everywhere nothing is drawn.
//
// Story (about 38 s, then holds, cycle 45 s):
//   0-4   dark; three flames pop alight at the ladder base - A asleep at the ladder foot holding the
//         plug end, B asleep on a heap of wire hugging the coil; something glows inside the heap...
//   4-17  A climbs; B slings the coil on its back; C bursts out of the heap (bouncing B off it) and
//         follows; C stops halfway to turn round and wave, then hurries up.
//   12-18 at the top each one peeks round the corner and jumps across onto the ledge
//   15-22 A lays the wire along the ledge and sets the plug end down; B pays out the coil; C tapes it down
//   22-26 C wobbles off the edge, grabs the wire and dangles; A and B haul it back up; cheer
//   28-43 all three sit on the ledge, peek down at the lights below, wave at us. Fade at 43.5.
// ============================================================================
const float CYCLE = 45.0;
float STEP = 0.42;                // seconds per rung (set from the ladder length so the timing stays fixed)
float NST = 16.0;                 // climbing steps before climbing over the top
const float LADZ = 0.42;          // ladder rails: depth behind the climber
float Hm, FLOOR_Y, LEDGE_Y, LX, HX, SW_X, TOP_Y, RNG, HK_Y;
float HKX0, HKX1, HKX2, HKX3, GX, SA, SB, SC, AEND, TX0, TX1, TX2;

float sdEllipsoid(vec3 p, vec3 r) { float k0 = length(p / r); float k1 = length(p / (r * r)); return k0 * (k0 - 1.0) / k1; }
float sdCap(vec3 p, vec3 a, vec3 b, float r) { vec3 pa = p - a, ba = b - a; float h = clamp(dot(pa, ba) / dot(ba, ba), 0.0, 1.0); return length(pa - ba * h) - r; }
float sdCapT(vec3 p, vec3 a, vec3 b, float ra, float rb) { vec3 pa = p - a, ba = b - a; float h = clamp(dot(pa, ba) / dot(ba, ba), 0.0, 1.0); return length(pa - ba * h) - mix(ra, rb, h); }
float sdBox3(vec3 p, vec3 b, float r) { vec3 q = abs(p) - b; return length(max(q, 0.0)) + min(max(q.x, max(q.y, q.z)), 0.0) - r; }
float smin3(float a, float b, float k) { float h = clamp(0.5 + 0.5 * (b - a) / k, 0.0, 1.0); return mix(b, a, h) - k * h * (1.0 - h); }
mat3 rotY(float a) { float c = cos(a), s = sin(a); return mat3(c, 0.0, -s, 0.0, 1.0, 0.0, s, 0.0, c); }
mat3 rotZ(float a) { float c = cos(a), s = sin(a); return mat3(c, s, 0.0, -s, c, 0.0, 0.0, 0.0, 1.0); }
mat3 rotX(float a) { float c = cos(a), s = sin(a); return mat3(1.0, 0.0, 0.0, 0.0, c, s, 0.0, -s, c); }
float bump(float t, float a, float b) { return sin(sat((t - a) / (b - a)) * PI); }

// ---------------------------------------------------------------- poses
struct Pose {
  vec3 pos; float yaw; float roll; float pitch; float sq; float lid; vec2 look; float ign;
  vec3 hL; vec3 hR; vec3 fL; vec3 fR; float hasCoil; vec3 coilP;
};
Pose PA, PB, PC;
Pose mk(vec3 pos) {
  Pose P;
  P.pos = pos; P.yaw = 0.0; P.roll = 0.0; P.pitch = 0.0; P.sq = 1.0; P.lid = 0.0; P.look = vec2(0.0); P.ign = 1.0;
  P.hL = vec3(-0.6, 0.18, -0.26); P.hR = vec3(0.6, 0.18, -0.28);
  P.fL = vec3(-0.17, 0.065, -0.05); P.fR = vec3(0.17, 0.065, -0.05);
  P.hasCoil = 0.0; P.coilP = vec3(0.0);
  return P;
}
void sitFloor(inout Pose P) {
  P.fL = vec3(-0.21, 0.07, -0.42); P.fR = vec3(0.21, 0.07, -0.42);
  P.hL = vec3(-0.6, 0.06, -0.2); P.hR = vec3(0.6, 0.06, -0.2);
}
void sleepy(inout Pose P, float t, float seed) {
  P.lid = 1.0; P.roll = 0.1 * sin(seed * 3.0); P.sq = 0.97 + 0.02 * sin(t * 2.2 + seed);
}
void walkLimbs(inout Pose P, float ph, float amt) {
  float s = sin(ph), c = cos(ph);
  P.fL = vec3(-0.17, 0.065 + 0.1 * max(s, 0.0) * amt, -0.05 - 0.22 * c * amt);
  P.fR = vec3(0.17, 0.065 + 0.1 * max(-s, 0.0) * amt, -0.05 + 0.22 * c * amt);
  P.hL = vec3(-0.6, 0.22, -0.26 + 0.22 * c * amt);
  P.hR = vec3(0.6, 0.22, -0.26 - 0.22 * c * amt);
  P.pos.y += 0.05 * abs(s) * amt;
  P.roll = 0.04 * s * amt;
}
// walk along x on surface y between times t0..t1 (eased at the ends)
void walkX(inout Pose P, float t, float t0, float t1, float x0, float x1, float y, float rate) {
  float k = sat((t - t0) / (t1 - t0));
  float kk = mix(k, easeIO(k), 0.35);
  P.pos = vec3(mix(x0, x1, kk), y, 0.0);
  P.yaw = x1 < x0 ? PI * 0.5 : -PI * 0.5;
  float amt = smoothstep(0.0, 0.08, k) * smoothstep(1.0, 0.92, k);
  walkLimbs(P, (t - t0) * rate, amt);
  P.pitch = 0.08 * amt;
}
// ballistic hop from a to b, peak height h above the higher end
void hopTo(inout Pose P, float t, float t0, float t1, vec3 a, vec3 b, float h) {
  float k = sat((t - t0) / (t1 - t0));
  P.pos = mix(a, b, k) + vec3(0.0, h * 4.0 * k * (1.0 - k), 0.0);
  P.hL = vec3(-0.55, 0.78, -0.18); P.hR = vec3(0.55, 0.78, -0.18);
  P.fL = vec3(-0.17, 0.13, 0.04); P.fR = vec3(0.17, 0.1, -0.12);
  P.sq = 1.0 + 0.12 * sin(k * PI);
}
// squash before take-off and on landing
float takeoffSquash(float t, float t0, float t1) {
  return 1.0 - 0.2 * bump(t, t0 - 0.16, t0) - 0.22 * bump(t, t1, t1 + 0.22);
}
// hand-over-hand ladder climbing, back to us. tc = time climbing, sd = seconds per step
void climbPose(inout Pose P, float tc, float sd, float y0) {
  float c = max(tc, 0.0) / sd;
  float n = floor(c), f = fract(c);
  float e = easeIO(f);
  float steps = min(n + e, NST);
  if (n >= NST) { e = 1.0; f = 1.0; }
  float sc = RNG / 0.25;
  P.pos = vec3(LX, y0 + RNG * steps, 0.0);
  float side = mod(n, 2.0) < 0.5 ? 1.0 : -1.0;
  float lift = sin(f * PI) * step(n, NST - 0.5);
  float hMove = mix(0.95, 0.95 + 0.25 * sc, e) + 0.06 * lift;
  float hHold = mix(0.95 + 0.25 * sc, 0.95, e);
  float fMove = mix(-0.27 * sc, -0.02, e) + 0.05 * lift;
  float fHold = mix(-0.02, -0.27 * sc, e);
  P.hL = vec3(-0.36, side > 0.0 ? hMove : hHold, -0.4 + 0.06 * lift * step(0.0, side));
  P.hR = vec3(0.36, side > 0.0 ? hHold : hMove, -0.4 + 0.06 * lift * step(side, 0.0));
  P.fL = vec3(-0.17, side > 0.0 ? fHold : fMove, -0.22);
  P.fR = vec3(0.17, side > 0.0 ? fMove : fHold, -0.22);
  P.yaw = PI;
  P.roll = 0.07 * lift * side;
}
// climbing over onto the top rung (standing, still back to us)
void climbOver(inout Pose P, float t, float t0, float y0) {
  float k = easeIO((t - t0) / 0.45);
  vec3 a = vec3(LX, y0 + RNG * NST, 0.0), b = vec3(LX, TOP_Y, 0.0);
  P.pos = mix(a, b, k) + vec3(0.0, 0.18 * sin(k * PI), 0.0);
  P.yaw = PI;
  P.hL = mix(vec3(-0.36, 1.0, -0.4), vec3(-0.55, 0.2, -0.3), k);
  P.hR = mix(vec3(0.36, 1.1, -0.4), vec3(0.55, 0.2, -0.3), k);
  P.fL = mix(vec3(-0.17, -0.02, -0.22), vec3(-0.17, 0.065, -0.05), k);
  P.fR = mix(vec3(0.17, 0.1, -0.22), vec3(0.17, 0.065, -0.05), k);
}
// a hop straight up, right hand raised to a hook
void hookHop(inout Pose P, float t, float t0, float x, float y) {
  float k = sat((t - t0) / 0.8);
  P.pos = vec3(x, y + 0.85 * sin(k * PI), 0.0);
  P.yaw = 0.0;
  P.hR = vec3(0.3, 1.45, -0.12); P.hL = vec3(-0.55, 0.55, -0.2);
  P.fL = vec3(-0.17, 0.065 + 0.08 * sin(k * PI), 0.0); P.fR = vec3(0.17, 0.065 + 0.1 * sin(k * PI), -0.05);
  P.sq = 1.0 - 0.2 * bump(t, t0 - 0.12, t0 + 0.04) + 0.1 * sin(k * PI) - 0.18 * bump(t, t0 + 0.8, t0 + 1.0);
  P.look = vec2(0.15, 0.6);
}
void sitLedge(inout Pose P, float x, float t, float seed) {
  P.pos = vec3(x, LEDGE_Y - 0.02, 0.0);
  P.yaw = 0.0;
  float s = sin(t * 3.4 + seed);
  P.fL = vec3(-0.19, -0.28 + 0.05 * s, -0.42 + 0.12 * s);
  P.fR = vec3(0.19, -0.28 - 0.05 * s, -0.42 - 0.12 * s);
  P.hL = vec3(-0.6, 0.04, -0.1); P.hR = vec3(0.6, 0.04, -0.1);
}
void kneelPull(inout Pose P, float t, float t0, float x) {
  P.pos = vec3(x, LEDGE_Y - 0.04, 0.0);
  P.yaw = 0.0;
  float tug = 0.5 + 0.5 * sin((t - t0) * 7.0);
  float heave = sstep(25.15, 25.6, t);
  P.pitch = mix(0.55 - 0.25 * tug, -0.35, heave);
  P.fL = vec3(-0.18, 0.05, 0.2); P.fR = vec3(0.18, 0.05, 0.2);
  vec3 dn = vec3(0.0, -0.1 + 0.15 * tug, -0.62);
  P.hL = mix(vec3(-0.2, 0.0, 0.0) + dn, vec3(-0.25, 0.45, -0.45), heave);
  P.hR = mix(vec3(0.2, 0.0, 0.0) + dn, vec3(0.25, 0.45, -0.45), heave);
  P.look = vec2(0.0, -0.5);
}
// sitting on the ledge at the end: peek down over the edge at the lights below, then wave at us
void endSit(inout Pose P, float x, float t, float seed, float tSit) {
  sitLedge(P, x, t, seed);
  P.pos.y += 0.25 * bump(t, tSit, tSit + 0.35);
  float pk = sstep(31.0 + seed * 0.15, 31.5 + seed * 0.15, t) * (1.0 - sstep(33.3, 33.8, t));
  P.pitch = 0.5 * pk;
  P.look = mix(P.look, vec2(0.0, -0.9), pk);
  float wv = bump(t, 35.0 + seed * 0.25, 36.7 + seed * 0.25);
  P.hR = mix(P.hR, vec3(0.62 + 0.07 * sin(t * 10.0), 0.85, -0.3), wv);
}
void tapeDown(inout Pose P, float t, float t0, float x) {
  float b = bump(t, t0, t0 + 0.45);
  P.pos = vec3(x, LEDGE_Y, 0.0);
  P.yaw = PI * 0.5;
  P.sq = 1.0 - 0.15 * b;
  P.pitch = 0.55 * b;
  P.hL = mix(P.hL, vec3(-0.14, 0.05, -0.58), b); P.hR = mix(P.hR, vec3(0.14, 0.05, -0.58), b);
  P.look = vec2(0.0, -0.8 * b);
}
void cheer(inout Pose P, float t, float t0, float seed) {
  float k = sat((t - t0) / 0.6);
  P.pos.y += 0.5 * sin(k * PI);
  P.hL = vec3(-0.5, 0.95, -0.2); P.hR = vec3(0.5, 0.95, -0.2);
  P.sq = 1.0 + 0.1 * sin(k * PI) - 0.15 * bump(t, t0 + 0.6, t0 + 0.8);
  P.yaw = 0.0;
}
float blink(float t, float seed) { return step(0.955, fract(t * 0.31 + seed * 0.37)); }

// coil carried by B (local positions)
const vec3 COIL_LAP = vec3(0.0, 0.3, -0.55);
const vec3 COIL_BACK = vec3(0.0, 0.4, 0.52);
const vec3 COIL_HANDS = vec3(0.0, 0.04, -0.64);

// ---------------------------------------------------------------- choreography
float climbY0() { return FLOOR_Y + 0.27; }
Pose poseA(float t) {
  Pose P = mk(vec3(LX, FLOOR_Y, -0.1));
  float yC = climbY0();
  float tTop = 4.3 + NST * STEP;           // 11.02
  P.ign = sstep(1.0, 1.15, t) + 0.35 * bump(t, 1.0, 1.5) * (1.0 - sat((t - 1.25) / 0.25));
  if (t < 2.7) {
    sitFloor(P);
    P.hR = vec3(0.4, 0.28, -0.5);          // hugging the plug
    if (t < 1.5) sleepy(P, t, 1.0);
    else { P.lid = 1.0 - sstep(1.5, 1.75, t); P.look = vec2(0.5 * sin((t - 1.9) * 4.0) * sstep(1.9, 2.1, t), 0.0); }
  } else if (t < 3.9) {
    // stand up, big stretch, then point up the ladder
    float k = sstep(2.7, 3.0, t);
    P.pos.y = FLOOR_Y + 0.0;
    P.hL = mix(vec3(-0.6, 0.06, -0.2), vec3(-0.35, 1.15, -0.1), sstep(2.7, 3.0, t) * (1.0 - sstep(3.3, 3.45, t)));
    P.hR = mix(vec3(0.4, 0.28, -0.5), vec3(0.35, 1.15, -0.1), k);
    if (t > 3.35) P.hR = mix(P.hR, vec3(0.42, 1.25, -0.3), sstep(3.35, 3.5, t));
    P.sq = 1.0 + 0.1 * bump(t, 2.8, 3.3);
    P.lid = 0.5 * bump(t, 2.8, 3.3);
    P.look = vec2(0.0, 0.8 * sstep(3.35, 3.5, t));
  } else if (t < 4.3) {
    P.yaw = PI * sstep(3.9, 4.25, t);
    P.hR = vec3(0.36, 1.0, -0.4); P.hL = vec3(-0.36, 1.0, -0.4);
  } else if (t < tTop) {
    climbPose(P, t - 4.3, STEP, yC);
    P.pos.y = mix(FLOOR_Y, P.pos.y, sstep(4.3, 4.6, t));
  } else if (t < tTop + 0.45) {
    climbOver(P, t, tTop, yC);
  } else if (t < 11.9) {
    // peek round the corner
    P.pos = vec3(LX, TOP_Y, 0.0);
    P.yaw = mix(PI, PI * 0.5, sstep(tTop + 0.45, 11.7, t));
    P.pitch = 0.18 * sstep(11.5, 11.75, t);
    P.look = vec2(0.0, -0.3);
  } else if (t < 12.62) {
    hopTo(P, t, 11.9, 12.6, vec3(LX, TOP_Y, 0.0), vec3(-1.4, LEDGE_Y, 0.0), 0.9);
    P.yaw = PI * 0.5;
  } else if (t < 16.8) {
    // walk the plug end along the ledge, string paying out behind
    walkX(P, t, 12.75, 16.8, -1.4, AEND + 0.5, LEDGE_Y, 9.0);
    if (t < 12.75) { P.pos = vec3(-1.4, LEDGE_Y, 0.0); P.yaw = PI * 0.5; }
    P.sq = 1.0 - 0.22 * bump(t, 12.6, 12.82);
    P.hR = vec3(0.45, 0.35, -0.45);
  } else if (t < 17.5) {
    // set the plug down at the end of the run
    P.pos = vec3(AEND + 0.5, LEDGE_Y, 0.0);
    P.yaw = PI * 0.5;
    float b = bump(t, 16.8, 17.5);
    P.pitch = 0.5 * b; P.sq = 1.0 - 0.1 * b;
    P.hR = mix(vec3(0.45, 0.35, -0.45), vec3(0.12, 0.05, -0.6), b);
    P.look = vec2(0.0, -0.7 * b);
  } else if (t < 22.9) {
    P.pos = vec3(AEND + 0.5, LEDGE_Y, 0.0);
    P.yaw = PI * 0.5 * (1.0 - sstep(17.5, 17.8, t));
    // dust off hands, then two happy hops, then watch C
    float cl = step(17.8, t) * step(t, 18.4);
    if (cl > 0.5) { float c = abs(sin((t - 17.8) * 16.0)); P.hL = vec3(-0.12 - 0.1 * c, 0.5, -0.45); P.hR = vec3(0.12 + 0.1 * c, 0.5, -0.45); }
    float hp = max(bump(t, 19.0, 19.55), bump(t, 19.7, 20.25));
    P.pos.y += 0.45 * hp;
    if (hp > 0.0) { P.hL = vec3(-0.5, 0.9, -0.2); P.hR = vec3(0.5, 0.9, -0.2); P.sq = 1.0 + 0.1 * hp; }
    P.sq -= 0.15 * (bump(t, 19.55, 19.7) + bump(t, 20.25, 20.45));
    P.look = vec2(0.6, 0.0);
    if (t > 22.5) { P.lid = 0.0; P.sq = 1.0 + 0.1 * bump(t, 22.6, 22.9); P.look = vec2(0.7, -0.2); }  // gasp
  } else if (t < 23.75) {
    walkX(P, t, 22.9, 23.75, AEND + 0.5, GX - 0.65, LEDGE_Y, 14.0);
    P.pitch = 0.2;
  } else if (t < 25.95) {
    kneelPull(P, t, 23.75, GX - 0.65);
  } else if (t < 26.7) {
    P.pos = vec3(GX - 0.65, LEDGE_Y, 0.0);
    cheer(P, t, 26.05, 1.0);
  } else if (t < 28.2) {
    walkX(P, t, 26.7, 28.2, GX - 0.65, SA, LEDGE_Y, 9.0);
  } else if (t < 28.6) {
    P.pos = vec3(SA, LEDGE_Y, 0.0);
    P.yaw = PI * 0.5 * (1.0 - sstep(28.2, 28.5, t));
  } else {
    endSit(P, SA, t, 0.0, 28.6);
  }
  P.lid = max(P.lid, blink(t, 1.0) * (1.0 - step(t, 2.0)));
  return P;
}
Pose poseB(float t) {
  vec3 heapTop = vec3(HX, FLOOR_Y + 0.42, 0.0);
  Pose P = mk(heapTop);
  P.hasCoil = 1.0;
  P.coilP = COIL_LAP;
  float yC = climbY0();
  float tc0 = 6.3, tTop = tc0 + NST * STEP;     // 13.02
  P.ign = sstep(1.6, 1.75, t) + 0.35 * bump(t, 1.6, 2.1) * (1.0 - sat((t - 1.85) / 0.25));
  if (t < 3.6) {
    sitFloor(P);
    P.hL = vec3(-0.32, 0.3, -0.58); P.hR = vec3(0.32, 0.3, -0.58);
    if (t < 2.0) sleepy(P, t, 2.0);
    else P.lid = 1.0 - sstep(2.0, 2.2, t);
    if (t > 2.3) P.look = vec2(-0.6, 0.0);
    if (t > 3.0) {
      // sling the coil over the head onto the back
      float k = sstep(3.0, 3.5, t);
      P.coilP = mix(COIL_LAP, COIL_BACK, k) + vec3(0.0, 0.75 * sin(k * PI), 0.0);
      P.hL = mix(P.hL, vec3(-0.3, 1.0, 0.1), sin(k * PI)); P.hR = mix(P.hR, vec3(0.3, 1.0, 0.1), sin(k * PI));
      P.fL = mix(P.fL, vec3(-0.17, 0.065, -0.05), k); P.fR = mix(P.fR, vec3(0.17, 0.065, -0.05), k);
    }
  } else if (t < 4.6) {
    P.coilP = COIL_BACK;
    P.look = vec2(-0.6, 0.3);
    P.yaw = 0.25;
  } else if (t < 5.45) {
    // C bursts out of the heap underneath: B is bounced off onto the ladder foot
    P.coilP = COIL_BACK;
    hopTo(P, t, 4.6, 5.4, heapTop, vec3(LX, FLOOR_Y, 0.0), 0.9);
    P.roll = 0.6 * sin(sat((t - 4.6) / 0.8) * PI);
    P.lid = 0.0; P.look = vec2(0.0, 0.5);
  } else if (t < 6.3) {
    P.pos = vec3(LX, FLOOR_Y, 0.0);
    P.coilP = COIL_BACK;
    P.sq = 1.0 - 0.25 * bump(t, 5.4, 5.62);
    P.yaw = mix(0.6, PI, sstep(5.95, 6.25, t));       // glare back at C, then turn to the ladder
    P.look = vec2(0.6, 0.0);
    P.hL = vec3(-0.6, 0.35, -0.3); P.hR = vec3(0.62, 0.35, -0.3);     // hands on hips-ish
  } else if (t < tTop) {
    P.coilP = COIL_BACK;
    climbPose(P, t - tc0, STEP, yC);
    P.pos.y = mix(FLOOR_Y, P.pos.y, sstep(tc0, tc0 + 0.3, t));
  } else if (t < tTop + 0.45) {
    P.coilP = COIL_BACK;
    climbOver(P, t, tTop, yC);
  } else if (t < 13.8) {
    P.coilP = COIL_BACK;
    P.pos = vec3(LX, TOP_Y, 0.0);
    P.yaw = mix(PI, PI * 0.5, sstep(tTop + 0.45, 13.7, t));
  } else if (t < 14.55) {
    P.coilP = COIL_BACK;
    hopTo(P, t, 13.8, 14.5, vec3(LX, TOP_Y, 0.0), vec3(-0.8, LEDGE_Y, 0.0), 0.8);
    P.yaw = PI * 0.5;
  } else if (t < 15.7) {
    P.coilP = COIL_BACK;
    P.pos = vec3(-0.8, LEDGE_Y, 0.0);
    P.yaw = PI * 0.5 * (1.0 - sstep(14.55, 14.85, t));
    P.sq = 1.0 - 0.22 * bump(t, 14.5, 14.7);
    P.look = vec2(-0.6, 0.0);
  } else if (t < 22.9) {
    // pay the wire out from the coil, which turns in its hands
    P.pos = vec3(-0.8, LEDGE_Y, 0.0);
    float k = sstep(15.7, 16.2, t);
    P.coilP = mix(COIL_BACK, COIL_HANDS, k) + vec3(0.0, 0.6 * sin(k * PI), 0.0);
    P.hL = vec3(-0.27, 0.06, -0.64); P.hR = vec3(0.27, 0.06, -0.64);
    P.look = vec2(-0.6, 0.1);
    P.yaw = 0.2;
    P.pos.y += 0.015 * abs(sin(t * 3.0));
    if (t > 22.5) { P.sq = 1.0 + 0.1 * bump(t, 22.6, 22.9); P.look = vec2(-0.7, -0.2); }
  } else if (t < 23.8) {
    P.coilP = COIL_BACK;
    walkX(P, t, 22.9, 23.8, -0.8, GX + 0.65, LEDGE_Y, 14.0);
    P.pitch = 0.2;
  } else if (t < 25.95) {
    P.coilP = COIL_BACK;
    kneelPull(P, t, 23.85, GX + 0.65);
  } else if (t < 26.7) {
    P.coilP = COIL_BACK;
    P.pos = vec3(GX + 0.65, LEDGE_Y, 0.0);
    cheer(P, t, 26.15, 2.0);
  } else if (t < 28.0) {
    P.coilP = COIL_BACK;
    walkX(P, t, 26.7, 28.0, GX + 0.65, SB, LEDGE_Y, 9.0);
  } else if (t < 29.6) {
    P.pos = vec3(SB, LEDGE_Y, 0.0);
    P.coilP = COIL_BACK;
    P.look = vec2(-0.7, 0.0);
    if (t > 29.0) P.look = vec2(-0.7, 0.4);
  } else {
    endSit(P, SB, t, 2.0, 29.6);
    P.coilP = mix(COIL_BACK, vec3(0.0, 0.12, 0.5), sstep(29.6, 30.0, t));   // tucked down behind, out of sight
  }
  P.lid = max(P.lid, blink(t, 2.0) * (1.0 - step(t, 2.3)));
  return P;
}
Pose poseC(float t) {
  vec3 hidden = vec3(HX, FLOOR_Y, 0.55);
  Pose P = mk(hidden);
  float yC = climbY0();
  float tc0 = 8.3, nPause = floor(NST * 0.45), tP0 = tc0 + nPause * STEP;
  float tP1 = tP0 + 2.2;                                        // resume 13.44
  float fast = 2.9 / (NST - nPause);
  float tTop = tP1 + (NST - nPause) * fast;                     // 16.14
  P.ign = sstep(2.4, 2.55, t);
  if (t < 4.6) {
    // buried in the heap, squashed small (its flame lights the loops from inside)
    P.sq = 0.4;
    P.lid = 1.0;
  } else if (t < 5.6) {
    // burst up out of the heap, shake off
    float k = sat((t - 4.6) / 0.35);
    P.sq = mix(0.4, 1.0, k) + 0.25 * bump(t, 4.6, 4.95);
    P.pos.y += 0.35 * bump(t, 4.6, 5.0);
    P.roll = 0.25 * sin((t - 4.9) * 22.0) * sstep(4.9, 5.0, t) * (1.0 - sstep(5.4, 5.6, t));
    P.hL = vec3(-0.6, 0.75, -0.2); P.hR = vec3(0.6, 0.75, -0.2);
  } else if (t < 7.6) {
    P.look = vec2(sin((t - 5.6) * 2.5) * 0.7, 0.1);
    if (t > 6.6) P.look = vec2(-0.7, 0.5);
  } else if (t < 8.3) {
    hopTo(P, t, 7.6, 8.25, hidden, vec3(LX, FLOOR_Y, 0.0), 0.75);
    P.yaw = PI * sstep(7.7, 8.2, t);
  } else if (t < tP0) {
    climbPose(P, t - tc0, STEP, yC);
    P.pos.y = mix(FLOOR_Y, P.pos.y, sstep(tc0, tc0 + 0.3, t));
  } else if (t < tP1) {
    // halfway: turn round to us, let go with one hand and wave
    climbPose(P, nPause * STEP - 0.001, STEP, yC);
    float k = sstep(tP0, tP0 + 0.35, t) * (1.0 - sstep(tP1 - 0.4, tP1, t));
    float over = sin(sat((t - tP0 - 0.35) / 0.35) * PI) * 0.25 * (1.0 - sat((t - tP0 - 0.35) / 0.35));
    P.yaw = PI - (2.55 + over) * k;
    P.roll = 0.0;
    P.hL = vec3(-0.36, 1.2, -0.4);
    P.fL = vec3(-0.17, -0.02, -0.22); P.fR = vec3(0.17, -0.27, -0.22);
    P.hR = mix(vec3(0.36, 0.95, -0.4), vec3(0.72 + 0.07 * sin(t * 10.0), 0.85, 0.0), k);
  } else if (t < tTop) {
    climbPose(P, nPause * STEP + (t - tP1) / fast * STEP, STEP, yC);
  } else if (t < tTop + 0.45) {
    climbOver(P, t, tTop, yC);
  } else if (t < 17.2) {
    P.pos = vec3(LX, TOP_Y, 0.0);
    P.yaw = mix(PI, PI * 0.5, sstep(tTop + 0.45, 17.0, t));
  } else if (t < 17.95) {
    hopTo(P, t, 17.2, 17.9, vec3(LX, TOP_Y, 0.0), vec3(-1.95, LEDGE_Y, 0.0), 0.9);
    P.yaw = PI * 0.5;
  } else if (t < 18.6) {
    walkX(P, t, 18.0, 18.6, -1.95, TX0 + 0.5, LEDGE_Y, 10.0);
    P.sq = 1.0 - 0.22 * bump(t, 17.9, 18.1);
  } else if (t < 19.1) {
    tapeDown(P, t, 18.6, TX0 + 0.5);
  } else if (t < 19.7) {
    walkX(P, t, 19.1, 19.7, TX0 + 0.5, TX1 + 0.5, LEDGE_Y, 10.0);
  } else if (t < 20.2) {
    tapeDown(P, t, 19.7, TX1 + 0.5);
  } else if (t < 20.8) {
    walkX(P, t, 20.2, 20.8, TX1 + 0.5, TX2 + 0.5, LEDGE_Y, 10.0);
  } else if (t < 21.3) {
    tapeDown(P, t, 20.8, TX2 + 0.5);
  } else if (t < 22.1) {
    walkX(P, t, 21.3, 21.9, TX2 + 0.5, GX, LEDGE_Y, 10.0);
    P.yaw = mix(P.yaw, 0.0, sstep(21.85, 22.1, t));
  } else if (t < 22.85) {
    // wobble on the edge, arms windmilling
    P.pos = vec3(GX, LEDGE_Y, 0.0);
    float k = sat((t - 22.1) / 0.75);
    P.pitch = 0.35 * sin((t - 22.1) * 11.0) * k + 0.3 * k;
    float w = (t - 22.1) * 16.0;
    P.hL = vec3(-0.55, 0.4, -0.2) + 0.3 * vec3(0.0, sin(w), cos(w));
    P.hR = vec3(0.55, 0.4, -0.2) + 0.3 * vec3(0.0, sin(w + 2.0), cos(w + 2.0));
    P.look = vec2(0.0, -0.6);
    P.lid = 0.0;
  } else if (t < 25.2) {
    // falls off the front, grabs the wire, dangles
    float k = sat((t - 22.85) / 0.3);
    float hang = LEDGE_Y - 1.35;
    P.pos = vec3(GX + 0.08 * sin((t - 23.1) * 2.6) * sstep(23.1, 23.3, t), mix(LEDGE_Y, hang, k * k) - 0.12 * bump(t, 23.15, 23.45), -0.5 * k);
    P.hL = vec3(-0.18, 1.22, -0.06); P.hR = vec3(0.18, 1.22, -0.06);
    P.fL = vec3(-0.17, 0.02 + 0.09 * sin(t * 9.0), -0.1); P.fR = vec3(0.17, 0.02 - 0.09 * sin(t * 9.0), -0.1);
    P.roll = 0.1 * sin((t - 23.1) * 2.6) * sstep(23.1, 23.3, t);
    P.look = vec2(0.0, 0.7);
    P.sq = 1.0 + 0.08 * bump(t, 23.15, 23.45);
  } else if (t < 26.0) {
    // hauled up and plonked on the ledge
    float k = sstep(25.2, 25.7, t);
    P.pos = vec3(GX, mix(LEDGE_Y - 1.35, LEDGE_Y, k) + 0.3 * sin(k * PI), mix(-0.5, 0.0, k));
    P.hL = vec3(-0.25, 1.0 - 0.8 * k, -0.1); P.hR = vec3(0.25, 1.0 - 0.8 * k, -0.1);
    P.sq = 1.0 - 0.2 * bump(t, 25.7, 25.95);
  } else if (t < 26.8) {
    P.pos = vec3(GX, LEDGE_Y, 0.0);
    cheer(P, t, 26.1, 3.0);
  } else if (t < 27.6) {
    walkX(P, t, 26.8, 27.6, GX, SC, LEDGE_Y, 10.0);
  } else if (t < 29.8) {
    P.pos = vec3(SC, LEDGE_Y, 0.0);
    P.look = vec2(-0.7, 0.0);
    if (t > 29.0) P.look = vec2(-0.7, 0.45);
  } else {
    endSit(P, SC, t, 1.0, 29.8);
    if (t > 39.0 && t < 40.5) { P.lid = 0.6; P.sq = 1.0 + 0.06 * bump(t, 39.0, 40.5); }   // yawn
  }
  P.lid = max(P.lid, blink(t, 3.0) * step(5.0, t));
  return P;
}

// ---------------------------------------------------------------- the diya folk body
const vec2 LIPD = vec2(1.0, 0.0);
float diyaE(vec2 v) {
  v = vec2(dot(v, LIPD), dot(v, vec2(-LIPD.y, LIPD.x)));
  vec2 a = vec2(0.12, 0.0), b = vec2(0.65, 0.0);
  vec2 pa = v - a, ba = b - a;
  float h = clamp(dot(pa, ba) / dot(ba, ba), 0.0, 1.0);
  float lip = length(pa - ba * h) - mix(0.36, 0.075, h);
  return smin3(length(v) - 0.5, lip, 0.16) + 0.5;
}
// (distance, material) in the character's local space. 1 clay 2 rim 3 eye 4 limb 5 hand/foot 6 oil 7 wick 8 coil
vec2 folk(vec3 p, Pose P) {
  float e = mix(length(p.xz), diyaE(p.xz), smoothstep(0.16, 0.47, p.y));
  float bowl = (length(vec2(e / 0.5, (p.y - 0.36) / 0.3)) - 1.0) * 0.27;
  bowl = max(bowl, p.y - 0.5);
  bowl = max(bowl, -max(e - 0.405, 0.455 - p.y));
  bowl = smin3(bowl, sdEllipsoid(p - vec3(0.0, 0.08, 0.0), vec3(0.22, 0.06, 0.22)), 0.06);
  vec2 res = vec2(bowl, 1.0);
  float rim = length(vec2(e - 0.425, p.y - 0.5)) - 0.032;
  if (rim < res.x) res = vec2(rim, 2.0);
  float oil = max(e - 0.415, abs(p.y - 0.462) - 0.008);
  if (oil < res.x) res = vec2(oil, 6.0);
  float wick = sdCapT(p, vec3(0.0, 0.45, 0.0), vec3(0.0, 0.53, 0.0), 0.02, 0.016);
  if (wick < res.x) res = vec2(wick, 7.0);
  float eyes = min(length(p - vec3(-0.16, 0.36, -0.4)) - 0.115, length(p - vec3(0.16, 0.36, -0.4)) - 0.115);
  if (eyes < res.x) res = vec2(eyes, 3.0);
  vec3 shL = vec3(-0.42, 0.33, -0.12), shR = vec3(0.42, 0.33, -0.12);
  float limbs = min(sdCapT(p, shL, P.hL, 0.07, 0.055), sdCapT(p, shR, P.hR, 0.07, 0.055));
  limbs = min(limbs, min(sdCapT(p, vec3(-0.14, 0.14, 0.0), P.fL + vec3(0.0, 0.04, 0.0), 0.08, 0.065), sdCapT(p, vec3(0.14, 0.14, 0.0), P.fR + vec3(0.0, 0.04, 0.0), 0.08, 0.065)));
  if (limbs < res.x) res = vec2(limbs, 4.0);
  float ends = min(length(p - P.hL) - 0.095, length(p - P.hR) - 0.095);
  ends = min(ends, min(sdEllipsoid(p - P.fL - vec3(0.0, 0.0, -0.05), vec3(0.11, 0.065, 0.14)), sdEllipsoid(p - P.fR - vec3(0.0, 0.0, -0.05), vec3(0.11, 0.065, 0.14))));
  if (ends < res.x) res = vec2(ends, 5.0);
  if (P.hasCoil > 0.5) {
    vec3 cq = p - P.coilP;
    float coil = length(vec2(length(cq.xy) - 0.27, cq.z)) - 0.075;
    if (coil < res.x) res = vec2(coil, 8.0);
  }
  return res;
}
vec3 toLocal(vec3 p, Pose P) {
  vec3 q = rotY(-P.yaw) * (p - P.pos);
  vec3 c = vec3(0.0, 0.1, 0.0);
  q = rotX(P.pitch) * (q - c) + c;
  q = rotZ(-P.roll) * (q - c) + c;
  q.y /= P.sq; q.xz *= sqrt(P.sq);
  return q;
}
vec3 toWorld(vec3 q, Pose P) {
  q.y *= P.sq; q.xz /= sqrt(P.sq);
  vec3 c = vec3(0.0, 0.1, 0.0);
  q = rotZ(P.roll) * (q - c) + c;
  q = rotX(-P.pitch) * (q - c) + c;
  return P.pos + rotY(P.yaw) * q;
}
float sqK(Pose P) { return min(P.sq, 1.0 / sqrt(P.sq)); }
Pose getP(int i) { if (i == 0) return PA; if (i == 1) return PB; return PC; }

// ---------------------------------------------------------------- props + scene
bool uF0, uF1, uF2, uLad, uBase, uSw;
float SW_Y;
vec2 mapScene(vec3 p) {
  vec2 res = vec2(1e5, 0.0);
  if (uLad) {
    vec3 lq = p - vec3(LX, 0.0, LADZ);
    float yTopR = TOP_Y + 0.55;
    float rails = min(length(vec2(lq.x + 0.55, lq.z)), length(vec2(lq.x - 0.55, lq.z))) - 0.048;
    rails = max(rails, max(FLOOR_Y - p.y, p.y - yTopR));
    rails = min(rails, min(length(p - vec3(LX - 0.55, yTopR, LADZ)), length(p - vec3(LX + 0.55, yTopR, LADZ))) - 0.048);
    float ry = mod(p.y - FLOOR_Y + RNG * 0.5, RNG) - RNG * 0.5;
    float rungs = max(length(vec2(ry, lq.z)) - 0.034, abs(lq.x) - 0.56);
    rungs = max(rungs, max(FLOOR_Y + RNG * 0.5 - p.y, p.y - TOP_Y - RNG * 0.5));
    res = vec2(min(rails, rungs), 60.0);
  }
  if (uBase) {
    // wooden plank shelf the ladder stands on
    float plank = sdBox3(p - vec3(1.55, FLOOR_Y - 0.07, 0.05), vec3(1.45, 0.06, 0.7), 0.012);
    if (plank < res.x) res = vec2(plank, 61.0);
    // a heap of wire loops
    float heap = 1e5;
    for (int k = 0; k < 5; k++) {
      float fk = float(k);
      vec3 hq = p - vec3(HX + 0.06 * sin(fk * 2.1), FLOOR_Y + 0.05 + fk * 0.085, 0.05 * cos(fk * 1.7));
      hq = rotZ(0.12 * sin(fk * 2.7)) * rotX(0.1 * cos(fk * 1.3)) * hq;
      float R = 0.5 - fk * 0.05;
      heap = min(heap, length(vec2(length(hq.xz) - R, hq.y)) - 0.052);
    }
    if (heap < res.x) res = vec2(heap, 62.0);
  }
  if (uSw) {
    vec3 sq = p - vec3(SW_X, SW_Y, 0.3);
    float plate = sdBox3(sq, vec3(0.74, 0.46, 0.04), 0.05);
    if (plate < res.x) res = vec2(plate, 63.0);
    vec3 rq = sq - vec3(-0.32, 0.0, -0.08);
    rq = rotX(0.13) * rq;     // rocker sits in OFF (top half sticking out, waiting to be pressed)
    float rocker = sdBox3(rq, vec3(0.15, 0.25, 0.035), 0.025);
    if (rocker < res.x) res = vec2(rocker, 64.0);
  }
  if (uF0) { vec2 c = folk(toLocal(p, PA), PA); c.x *= sqK(PA); if (c.x < res.x) res = vec2(c.x, c.y); }
  if (uF1) { vec2 c = folk(toLocal(p, PB), PB); c.x *= sqK(PB); if (c.x < res.x) res = vec2(c.x, c.y + 20.0); }
  if (uF2) { vec2 c = folk(toLocal(p, PC), PC); c.x *= sqK(PC); if (c.x < res.x) res = vec2(c.x, c.y + 40.0); }
  return res;
}
vec3 calcNormal(vec3 p) {
  const vec2 k = vec2(1.0, -1.0);
  float e = 0.0015;
  return normalize(k.xyy * mapScene(p + k.xyy * e).x + k.yyx * mapScene(p + k.yyx * e).x + k.yxy * mapScene(p + k.yxy * e).x + k.xxx * mapScene(p + k.xxx * e).x);
}
vec3 flameW(Pose P) { return toWorld(vec3(0.0, 0.66, 0.0), P); }
vec3 clayColour(vec3 q, int who) {
  vec3 base = who == 0 ? vec3(0.92, 0.42, 0.16) : (who == 1 ? vec3(0.78, 0.3, 0.14) : vec3(0.96, 0.56, 0.22));
  vec3 bandC = who == 0 ? vec3(1.0, 0.8, 0.25) : (who == 1 ? vec3(0.25, 0.8, 0.75) : vec3(1.0, 0.4, 0.5));
  float ang = atan(q.x, -q.z), h = q.y;
  base *= 0.9 + 0.1 * vnoise(vec2(ang * 12.0, h * 30.0));
  base = mix(base, bandC * 1.25, smoothstep(0.016, 0.009, abs(h - 0.215)));
  base = mix(base, vec3(0.1, 0.55, 0.45), smoothstep(0.01, 0.005, abs(h - 0.12)));
  float di = floor(ang / TAU * 18.0);
  vec3 dc = mod(di, 3.0) < 1.0 ? vec3(0.9, 0.1, 0.15) : (mod(di, 3.0) < 2.0 ? vec3(0.1, 0.45, 0.9) : vec3(0.1, 0.7, 0.35));
  base = mix(base, dc, smoothstep(0.03, 0.02, length(vec2((fract(ang / TAU * 18.0) - 0.5) * 0.17, h - 0.16))));
  float pet = abs(length(vec2((fract(ang / TAU * 14.0 + 0.5) - 0.5) * 0.22, (h - 0.27) * 1.6)) - 0.03);
  base = mix(base, vec3(1.0, 0.95, 0.85), smoothstep(0.008, 0.003, pet) * step(0.24, h));
  float ch = min(length(vec2(ang + 0.62, (h - 0.27) * 1.5)), length(vec2(ang - 0.62, (h - 0.27) * 1.5)));
  base = mix(base, vec3(1.0, 0.45, 0.45), smoothstep(0.12, 0.05, ch) * 0.7);
  float sm = abs(length(vec2(ang * 0.5, h - 0.33)) - 0.07);
  base = mix(base, vec3(0.25, 0.06, 0.04), smoothstep(0.012, 0.006, sm) * step(h, 0.31) * step(abs(ang), 0.45) * step(0.24, h));
  float br = min(abs(length(vec2(ang + 0.36, (h - 0.44) * 1.2)) - 0.08), abs(length(vec2(ang - 0.36, (h - 0.44) * 1.2)) - 0.08));
  base = mix(base, vec3(0.25, 0.08, 0.04), smoothstep(0.012, 0.006, br) * step(0.455, h) * step(abs(ang), 0.6));
  vec2 bq = vec2((abs(ang) - PI) * 0.5, h - 0.33);
  float bs = abs(fract(length(bq) * 30.0 - atan(bq.y, bq.x) / TAU) - 0.5);
  base = mix(base, bandC, smoothstep(0.12, 0.05, bs) * step(length(bq), 0.08) * step(2.7, abs(ang)));
  return base;
}
// "ON" lettering in rocker-face coordinates
float sdON(vec2 p) {
  float o = abs(length((p - vec2(-0.05, 0.0)) / vec2(0.04, 0.055)) - 1.0) * 0.04 - 0.009;
  float n = min(min(sdSeg(p, vec2(0.025, -0.055), vec2(0.025, 0.055)), sdSeg(p, vec2(0.095, -0.055), vec2(0.095, 0.055))), sdSeg(p, vec2(0.025, 0.055), vec2(0.095, -0.055))) - 0.011;
  return min(o, n);
}
vec3 SW_LED;
float t2seed(int w) { return float(w) * 0.37; }
vec3 shade(vec3 p, vec3 rd, vec2 m) {
  vec3 n = calcNormal(p);
  vec3 alb = vec3(0.5); float spec = 0.2, gloss = 20.0; float emis = 0.0; vec3 emisC = vec3(0.0);
  bool isChar = m.y < 59.5;
  if (isChar) {
    int who = int(floor(m.y / 20.0));
    float id = mod(m.y, 20.0);
    Pose P = getP(who);
    vec3 q = toLocal(p, P);
    if (id < 1.5) { alb = clayColour(q, who); spec = 0.55; gloss = 40.0; }
    else if (id < 2.5) { alb = vec3(0.66, 0.27, 0.1); spec = 0.6; gloss = 50.0; }
    else if (id < 3.5) {
      vec3 eC = vec3(q.x < 0.0 ? -0.16 : 0.16, 0.36, -0.4);
      vec3 en = normalize(q - eC);
      vec3 look = normalize(vec3(P.look.x * 0.45, -0.05 + P.look.y * 0.35, -1.0));
      float d = dot(en, look);
      alb = d > 0.8 ? vec3(0.04, 0.03, 0.03) : (d > 0.72 ? vec3(0.35, 0.18, 0.08) : vec3(0.98));
      if (d > 0.9 && en.y > 0.0 && en.x * sign(q.x) < 0.2) alb += 0.0;
      spec = 1.0; gloss = 120.0;
      float lidLine = 0.115 * (1.0 - 2.0 * P.lid);
      if (q.y - 0.36 > lidLine) { alb = clayColour(q, who) * 0.85; spec = 0.4; gloss = 30.0; }
      if (abs(q.y - 0.36 - lidLine) < 0.012 && P.lid > 0.05) alb = vec3(0.15, 0.05, 0.03);
    }
    else if (id < 4.5) { alb = vec3(0.62, 0.26, 0.11); spec = 0.35; gloss = 25.0; }
    else if (id < 5.5) { alb = vec3(0.7, 0.3, 0.13); spec = 0.35; gloss = 25.0; }
    else if (id < 6.5) { alb = vec3(0.75, 0.45, 0.08); spec = 1.0; gloss = 90.0; }
    else if (id < 7.5) { alb = vec3(0.12, 0.07, 0.05); emis = 0.6 * P.ign; emisC = vec3(1.1, 0.45, 0.12); }
    else {
      // wire coil: cream cable with wound grooves and the odd little bulb
      vec3 cq = q - P.coilP;
      float a = atan(cq.y, cq.x);
      float wind = 0.5 + 0.5 * sin(atan(cq.z, length(cq.xy) - 0.27) * 3.0 + a * 40.0);
      alb = vec3(0.86, 0.8, 0.68) * (0.75 + 0.25 * wind);
      float bl = step(0.93, fract(a / TAU * 9.0 + t2seed(who)));
      alb = mix(alb, vec3(1.0, 0.85, 0.55), bl * 0.6);
      spec = 0.5; gloss = 40.0;
    }
  } else if (m.y < 60.5) {
    alb = vec3(0.4, 0.24, 0.12) * (0.75 + 0.25 * vnoise(vec2(p.x * 9.0 + p.z * 9.0, p.y * 70.0)));
    spec = 0.15;
  } else if (m.y < 61.5) {
    alb = vec3(0.5, 0.31, 0.17) * (0.7 + 0.3 * vnoise(vec2(p.x * 2.5, p.z * 40.0 + p.y * 40.0)));
    spec = 0.1;
  } else if (m.y < 62.5) {
    float a = atan(p.z, p.x - HX);
    alb = vec3(0.86, 0.8, 0.68) * (0.8 + 0.2 * sin(a * 50.0 + p.y * 30.0));
    alb = mix(alb, vec3(1.0, 0.85, 0.55), step(0.94, fract(a / TAU * 13.0 + p.y * 3.0)) * 0.6);
    spec = 0.45; gloss = 40.0;
  } else {
    // switch plate (63) / rocker (64)
    vec2 sp = p.xy - vec2(SW_X, SW_Y);
    alb = vec3(0.92, 0.9, 0.86); spec = 0.6; gloss = 60.0;
    if (m.y < 63.5) {
      // socket on the right: round face, 3 holes; brand-less screw caps; indicator LED above the rocker
      vec2 sq = sp - vec2(0.3, 0.0);
      float face = length(sq) - 0.26;
      alb = mix(alb, vec3(0.84, 0.82, 0.78), step(face, 0.0));
      float holes = min(min(length(sq - vec2(-0.08, -0.06)) - 0.032, length(sq - vec2(0.08, -0.06)) - 0.032), length(sq - vec2(0.0, 0.09)) - 0.04);
      alb = mix(alb, vec3(0.05), step(holes, 0.0));
      float screws = min(length(sp - vec2(-0.66, 0.0)), length(sp - vec2(0.66, 0.0))) - 0.03;
      alb = mix(alb, vec3(0.6), step(screws, 0.0));
      float led = length(sp - vec2(-0.32, 0.36)) - 0.03;
      if (led < 0.0) { emis = 1.0; emisC = SW_LED * 2.5; }
    } else {
      vec2 rp = sp - vec2(-0.32, 0.0);
      float on = sdON((rp - vec2(0.0, 0.11)) * 1.0);
      alb = mix(alb, vec3(0.75, 0.08, 0.06), step(on, 0.0));
      alb = mix(alb, vec3(0.8, 0.8, 0.78), step(rp.y, -0.005) * 0.4);
      gloss = 80.0;
    }
  }
  vec3 col = vec3(0.0);
  for (int i = 0; i < 3; i++) {
    Pose P = getP(i);
    if (P.ign < 0.01) continue;
    vec3 L = flameW(P) - p; float dist = length(L); L /= dist;
    vec3 lc = vec3(1.0, 0.62, 0.28) * 3.0 * P.ign / (1.0 + dist * dist * 1.0);
    col += alb * lc * sat(dot(n, L) * 0.85 + 0.15);
    col += lc * pow(sat(dot(n, normalize(L - rd))), gloss) * spec;
  }
  // switch LED: a faint red-amber light on the plate around it
  vec3 Ls = vec3(SW_X - 0.32, SW_Y + 0.36, 0.2) - p; float ds = length(Ls);
  col += alb * SW_LED * 0.5 / (1.0 + ds * ds * 18.0) * sat(dot(n, Ls / ds) + 0.3);
  // tiny cool fill so forms read on black, warm rim
  col += alb * vec3(0.32, 0.3, 0.38) * (0.25 + 0.75 * sat(dot(n, normalize(vec3(-0.4, 0.6, -0.8))))) * 0.09;
  if (isChar) {
    // soft warm front fill (the characters glow a little from their own flame) + rim
    float on = 0.0;
    for (int i = 0; i < 3; i++) { Pose P = getP(i); on = max(on, P.ign * step(length(p.xy - P.pos.xy - vec2(0.0, 0.4)), 1.6)); }
    col += alb * vec3(1.0, 0.72, 0.5) * (0.12 + 0.3 * sat(dot(n, vec3(0.0, 0.25, -1.0)))) * on;
    col += vec3(1.0, 0.65, 0.35) * pow(1.0 - sat(dot(n, -rd)), 3.0) * 0.2 * on;
  } else {
    col += alb * vec3(1.0, 0.8, 0.6) * 0.03;
  }
  col += emisC * emis;
  return col;
}

// ---------------------------------------------------------------- 2D overlays
vec3 flame2D(vec2 sp, vec2 base, float h, float seed) {
  vec2 q = (sp - base) / vec2(h * 0.42, h);
  q.x += (vnoise(vec2(gT * 5.0, seed)) - 0.5) * 0.35 * sat(q.y) + sin(q.y * 6.0 - gT * 9.0) * 0.04 * sat(q.y);
  float fd = length(vec2(q.x, max(q.y, 0.0) * 0.72 + min(q.y, 0.0) * 1.8));
  float body = 1.0 - smoothstep(0.75, 0.95, fd + q.y * 0.1);
  float core = 1.0 - smoothstep(0.0, 0.55, fd);
  vec3 c = mix(vec3(1.0, 0.38, 0.05), vec3(1.0, 0.85, 0.45), smoothstep(0.9, 0.3, fd));
  c = mix(c, vec3(1.0, 0.98, 0.9), core);
  c += vec3(0.2, 0.35, 1.0) * smoothstep(0.35, 0.0, length(q - vec2(0.0, -0.12))) * 0.4;
  return c * body * 1.4;
}
float lightAt(vec2 p) {
  float L = 0.0;
  for (int i = 0; i < 3; i++) {
    Pose P = getP(i);
    vec3 f = flameW(P);
    float d = length(p - f.xy);
    L += P.ign * 1.6 / (1.0 + d * d * 1.4);
  }
  return L;
}
float floorAt(float x) { return x < 0.0 ? LEDGE_Y + 0.035 : (x < 3.0 ? FLOOR_Y + 0.035 : -100.0); }
vec2 bez(vec2 a, vec2 c, vec2 b, float u) { return mix(mix(a, c, u), mix(c, b, u), u); }
vec4 gWire;        // rgb + coverage accumulated
float gWireMinD;
void wireSeg(vec2 p, vec2 a, vec2 b, float sag, float clampIt, float bulbs) {
  vec2 d = b - a; float len = length(d);
  if (len < 0.02) return;
  vec2 c = (a + b) * 0.5 + vec2(0.0, -sag) + vec2(0.45, 0.0) * pow(abs(d.y) / len, 3.0) * step(0.8, len);
  vec2 lo = min(min(a, b), c) - 0.2, hi = max(max(a, b), c) + 0.2;
  if (p.x < lo.x || p.y < lo.y - 0.1 || p.x > hi.x || p.y > hi.y) return;
  float dm = 1e5; vec2 prev = a;
  if (clampIt > 0.5) prev.y = max(prev.y, floorAt(prev.x));
  for (int i = 1; i <= 14; i++) {
    vec2 q = bez(a, c, b, float(i) / 14.0);
    if (clampIt > 0.5) q.y = max(q.y, floorAt(q.x));
    dm = min(dm, sdSeg(p, prev, q)); prev = q;
  }
  gWireMinD = min(gWireMinD, dm);
  if (bulbs > 0.5) {
    float nb = floor(len / 0.3);
    for (int j = 0; j < 30; j++) {
      if (float(j) >= nb) break;
      vec2 q = bez(a, c, b, (float(j) + 0.5) / nb);
      if (clampIt > 0.5) q.y = max(q.y, floorAt(q.x) + 0.04);
      vec2 bq = p - q - vec2(0.0, -0.065);
      float bd = length(bq / vec2(0.034, 0.05)) - 1.0;
      if (bd < 0.6) {
        float cv = cov(bd * 0.034);
        float L = lightAt(p);
        vec3 glass = vec3(0.28, 0.25, 0.2) * (0.25 + L) + vec3(1.0, 0.85, 0.6) * smoothstep(0.5, 0.0, length(bq - vec2(-0.01, 0.015)) / 0.02) * (0.15 + 0.6 * L);
        float cap = cov(sdBox(p, q + vec2(0.0, -0.018), vec2(0.018, 0.016)));
        glass = mix(glass, vec3(0.5, 0.48, 0.42) * (0.2 + L), cap);
        gWire.rgb = mix(gWire.rgb, glass, max(cv, cap));
        gWire.a = max(gWire.a, max(cv, cap));
      }
    }
  }
}

// ---------------------------------------------------------------- main
void main() {
  vec2 uv = isf_FragNormCoord;
  float A = RENDERSIZE.x / RENDERSIZE.y;
  Hm = 9.0 / charSize;
  vec2 w;
  if (wallSide == 0) w = vec2(-(1.0 - uv.x) * A * stretch * Hm, uv.y * Hm);
  else if (wallSide == 1) w = vec2(uv.x * A * stretch * Hm, uv.y * Hm);
  else w = vec2((uv.x - 0.58) * A * Hm, uv.y * Hm);
  gPx = Hm / RENDERSIZE.y; aa = gPx;
  FLOOR_Y = floorHeight * Hm; LEDGE_Y = ledgeHeight * Hm; LX = max(ladderDist * Hm, 0.68); HX = LX + 1.22;
  SW_X = -switchDist * Hm; SW_Y = LEDGE_Y + 1.0;
  TOP_Y = LEDGE_Y + 0.3;
  // longer ladder = more rungs, stepped a little quicker, so the story timing never changes
  NST = max(10.0, floor((TOP_Y - FLOOR_Y) / 0.3) - 2.0);
  STEP = 6.72 / NST;
  RNG = (TOP_Y - FLOOR_Y) / (NST + 2.0);
  AEND = SW_X;                                       // where A sets the plug end down
  TX0 = mix(-1.95, AEND + 0.5, 0.25); TX1 = mix(-1.95, AEND + 0.5, 0.5); TX2 = mix(-1.95, AEND + 0.5, 0.75);
  GX = mix(TX1, TX2, 0.5);                           // where C falls off
  SA = mix(AEND, -1.25, 0.2); SB = -1.25; SC = mix(SA, SB, 0.5);   // where they sit at the end
  float t;
  if (clockMode == 0) t = mod(DATE.w + startOffset, CYCLE);
  else if (clockMode == 1) t = mod(TIME + startOffset, CYCLE);
  else t = previewTime;
  gT = t;
  PA = poseA(t); PB = poseB(t); PC = poseC(t);
  float master = 1.0 - sstep(43.5, 44.8, t);
  SW_LED = vec3(0.0);

  // which objects can this pixel see? (orthographic camera looking straight at the wall)
  uF0 = length(w - PA.pos.xy - vec2(0.0, 0.45)) < 1.7 && PA.ign > -1.0;
  uF1 = length(w - PB.pos.xy - vec2(0.0, 0.5)) < 1.8;
  uF2 = length(w - PC.pos.xy - vec2(0.0, 0.5)) < 1.8;
  uLad = abs(w.x - LX) < 0.7 && w.y > FLOOR_Y - 0.1 && w.y < TOP_Y + 0.7;
  uBase = w.x > 0.0 && w.x < 3.1 && w.y > FLOOR_Y - 0.2 && w.y < FLOOR_Y + 0.6;
  uSw = false;                                       // no switch in this shot
  vec3 col = vec3(0.0);
  float hitZ = 1e5;
  if (uF0 || uF1 || uF2 || uLad || uBase || uSw) {
    vec3 ro = vec3(w, -3.0), rd = vec3(0.0, 0.0, 1.0);
    float tt = 0.0; vec2 m = vec2(0.0); bool hit = false;
    for (int i = 0; i < 80; i++) {
      vec2 h = mapScene(ro + rd * tt);
      if (h.x < 0.0015) { m = h; hit = true; break; }
      tt += h.x * 0.9;
      if (tt > 4.6) break;
    }
    if (hit) { col = shade(ro + rd * tt, rd, m); hitZ = -3.0 + tt; }
  }
  // warm light the flames throw on the wall itself + a lit line along the ledge edge
  float L = lightAt(w);
  col += vec3(1.0, 0.5, 0.18) * 0.05 * L * L * step(1e4, hitZ);
  if (w.x < 0.0) col += vec3(1.0, 0.6, 0.3) * 0.25 * L * smoothstep(0.05, 0.0, abs(w.y - LEDGE_Y + 0.035)) * step(1e4, hitZ);
  // the wire: from B's coil, laid along the ledge to the plug end (A carries it, then sets it down)
  gWire = vec4(0.0); gWireMinD = 1e5;
  vec2 aHand = toWorld(PA.hR, PA).xy;
  vec2 W0 = toWorld(PB.coilP + vec3(0.25, 0.0, 0.0), PB).xy;
  vec2 plugRest = vec2(AEND, LEDGE_Y + 0.05);
  vec2 END = t > 17.15 ? plugRest : aHand;
  float gag = sstep(22.85, 23.0, t) * (1.0 - sstep(25.55, 25.75, t));
  if (gag > 0.001) {
    vec2 cH = (toWorld(PC.hL, PC).xy + toWorld(PC.hR, PC).xy) * 0.5 + vec2(0.0, 0.06);
    vec2 P1 = vec2(GX + 0.3, LEDGE_Y + 0.035), P2 = vec2(GX - 0.3, LEDGE_Y + 0.035);
    vec2 M = mix(vec2(GX, LEDGE_Y + 0.035), cH, gag);
    wireSeg(w, W0, P1, 0.25, 1.0, 0.0);
    wireSeg(w, P1, M, 0.03, 0.0, 0.0);
    wireSeg(w, M, P2, 0.03, 0.0, 0.0);
    wireSeg(w, P2, END, 0.25, 1.0, 0.0);
  } else {
    wireSeg(w, W0, END, 0.25, 1.0, 0.0);
  }
  {
    float Lw = lightAt(w);
    float wc = cov(gWireMinD - 0.018);
    float across = sat(1.0 - gWireMinD / 0.018);
    vec3 cable = vec3(0.88, 0.82, 0.7) * (0.12 + Lw) * (0.6 + 0.4 * across);
    col = mix(col, cable, wc);
  }
  // C's tape strips holding the wire down
  for (int k = 0; k < 3; k++) {
    float tx = k == 0 ? TX0 : (k == 1 ? TX1 : TX2);
    float tt0 = k == 0 ? 18.82 : (k == 1 ? 19.92 : 21.02);
    float on = step(tt0, t) * (1.0 - gag * step(abs(tx - GX), 0.5));
    if (on > 0.5) {
      vec2 q = rot(0.12 * (float(k) - 1.0)) * (w - vec2(tx, LEDGE_Y + 0.04));
      float tp = sdBox(q, vec2(0.0), vec2(0.09, 0.032)) - 0.006;
      col = mix(col, vec3(0.95, 0.85, 0.55) * (0.1 + 0.8 * lightAt(w)), cov(tp) * 0.92);
    }
  }
  // the plug at the end of the wire
  {
    vec2 dirP = t > 17.15 ? vec2(-1.0, 0.0) : normalize(vec2(-1.0, -0.4));
    vec2 pq = w - END;
    pq = vec2(dot(pq, dirP), dot(pq, vec2(-dirP.y, dirP.x)));
    float body = sdBox(pq, vec2(-0.06, 0.0), vec2(0.06, 0.045)) - 0.014;
    float pins = min(sdBox(pq, vec2(0.03, 0.02), vec2(0.03, 0.007)), sdBox(pq, vec2(0.03, -0.02), vec2(0.03, 0.007)));
    float Lp = lightAt(w);
    col = mix(col, vec3(0.8, 0.78, 0.72) * (0.08 + 0.7 * Lp), cov(body));
    col = mix(col, vec3(0.85, 0.7, 0.35) * (0.15 + Lp), cov(pins));
  }

  // flames on top (hidden by whatever is in front of their base)
  for (int i = 0; i < 3; i++) {
    Pose P = getP(i);
    if (P.ign < 0.01) continue;
    vec3 fb = toWorld(vec3(0.0, 0.5, 0.0), P);
    float occl = hitZ < fb.z - 0.06 ? 0.0 : 1.0;
    vec3 fc = flame2D(w, fb.xy, 0.3 * P.ign * sqrt(P.sq), float(i) * 3.0);
    col += fc * occl + fc * (1.0 - occl) * 0.1 * step(fb.y + 0.05, w.y);
    col += vec3(1.0, 0.5, 0.15) * exp(-length(w - fb.xy - vec2(0.0, 0.12)) / 0.16) * 0.14 * P.ign;
    // ignition spark
    float ti = i == 0 ? 1.0 : (i == 1 ? 1.6 : 2.4);
    float spk = bump(t, ti - 0.05, ti + 0.2);
    if (spk > 0.0 && occl > 0.5) col += vec3(1.0, 0.9, 0.6) * spk * exp(-length(w - fb.xy) / 0.06) * 1.5;
  }
  // C's flame glowing up through the heap before it bursts out
  if (t < 4.7) {
    vec2 hc = vec2(HX, FLOOR_Y + 0.3);
    col += vec3(1.0, 0.5, 0.15) * PC.ign * exp(-length((w - hc) / vec2(1.0, 0.7)) / 0.35) * 0.35 * (0.85 + 0.15 * sin(t * 13.0));
  }

  // preview guides: corner fold + ledge
  if (wallSide == 2) {
    col += vec3(0.12) * line(abs(w.x), gPx * 1.0);
    col += vec3(0.08, 0.08, 0.1) * line(abs(w.y - LEDGE_Y), gPx * 0.8) * step(w.x, 0.0);
  }
  col *= master;
  // mapping guides: layer border, a 1-unit grid that runs continuously across both walls,
  // the corner (magenta), the ledge line and ladder base (cyan)
  if (showGrid) {
    vec2 px = uv * RENDERSIZE;
    float bd = min(min(px.x, RENDERSIZE.x - px.x), min(px.y, RENDERSIZE.y - px.y));
    vec2 g = abs(fract(w + 0.5) - 0.5) / gPx;
    float grid = 1.0 - smoothstep(0.5, 1.5, min(g.x, g.y));
    vec2 g5 = abs(fract(w / 4.0 + 0.5) - 0.5) * 4.0 / gPx;
    float grid5 = 1.0 - smoothstep(1.0, 2.0, min(g5.x, g5.y));
    col = max(col, vec3(0.25) * grid);
    col = max(col, vec3(0.55) * grid5);
    col = max(col, vec3(1.0, 0.0, 1.0) * (1.0 - smoothstep(1.5, 3.0, abs(w.x) / gPx)));
    if (w.x < 0.0) col = max(col, vec3(0.0, 1.0, 1.0) * (1.0 - smoothstep(1.5, 3.0, abs(w.y - LEDGE_Y) / gPx)));
    else col = max(col, vec3(0.0, 1.0, 1.0) * (1.0 - smoothstep(1.5, 3.0, abs(w.y - FLOOR_Y) / gPx)));
    col = max(col, vec3(1.0, 1.0, 0.0) * (1.0 - smoothstep(3.0, 5.0, bd)));
    // aspect check: these must look like perfect circles on the wall (squares like squares).
    // If they look like ovals, change "Horizontal stretch" on that layer until they are round.
    vec2 cc = (floor(w / 4.0) + 0.5) * 4.0;
    float cr = abs(length(w - cc) - 1.6) / gPx;
    col = max(col, vec3(0.2, 1.0, 0.3) * (1.0 - smoothstep(1.5, 3.0, cr)));
    col = max(col, vec3(0.2, 1.0, 0.3) * (1.0 - smoothstep(2.0, 4.0, length(w - cc) / gPx)));
    master = 1.0;
  }
  col *= 1.3;
  col = col / (1.0 + col * 0.3);
  gl_FragColor = vec4(max(col, 0.0) * brightness, 1.0);
}
