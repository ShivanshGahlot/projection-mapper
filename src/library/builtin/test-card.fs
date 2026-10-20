/*{
  "DESCRIPTION": "Static alignment pattern for lining up projection mapping: grid, circles, diagonals, border, coloured corner markers (red top-left, green top-right, blue bottom-left, yellow bottom-right) and colour bars.",
  "CREDIT": "Built-in",
  "ISFVSN": "2",
  "CATEGORIES": ["Generator", "Utility"],
  "INPUTS": [
    {"NAME": "lineColor", "TYPE": "color", "DEFAULT": [1.0, 1.0, 1.0, 1.0], "LABEL": "Line colour"},
    {"NAME": "rows", "TYPE": "float", "DEFAULT": 8.0, "MIN": 2.0, "MAX": 24.0, "LABEL": "Grid squares from top to bottom"},
    {"NAME": "showChecker", "TYPE": "bool", "DEFAULT": true, "LABEL": "Checkerboard shading"},
    {"NAME": "showCircles", "TYPE": "bool", "DEFAULT": true, "LABEL": "Circles"},
    {"NAME": "showDiagonals", "TYPE": "bool", "DEFAULT": true, "LABEL": "Diagonal lines"},
    {"NAME": "showBars", "TYPE": "bool", "DEFAULT": true, "LABEL": "Colour bars"}
  ]
}*/

#ifdef GL_ES
precision highp float;
#endif

const float PI = 3.14159265359;
const float TAU = 6.28318530718;

float hash21(vec2 p) {
  vec3 q = fract(vec3(p.xyx) * vec3(0.1031, 0.1030, 0.0973));
  q += dot(q, q.yzx + 33.33);
  return fract((q.x + q.y) * q.z);
}

vec2 grad2(vec2 i) {
  float a = hash21(i) * TAU;
  return vec2(cos(a), sin(a));
}

// Smooth value noise, 0..1.
float vnoise(vec2 p) {
  vec2 i = floor(p);
  vec2 f = fract(p);
  f = f * f * (3.0 - 2.0 * f);
  return mix(mix(hash21(i), hash21(i + vec2(1.0, 0.0)), f.x),
             mix(hash21(i + vec2(0.0, 1.0)), hash21(i + vec2(1.0, 1.0)), f.x), f.y);
}

// Gradient noise, roughly -1..1.
float gnoise(vec2 p) {
  vec2 i = floor(p);
  vec2 f = fract(p);
  vec2 u = f * f * f * (f * (f * 6.0 - 15.0) + 10.0);
  float a = dot(grad2(i), f);
  float b = dot(grad2(i + vec2(1.0, 0.0)), f - vec2(1.0, 0.0));
  float c = dot(grad2(i + vec2(0.0, 1.0)), f - vec2(0.0, 1.0));
  float d = dot(grad2(i + vec2(1.0, 1.0)), f - vec2(1.0, 1.0));
  return mix(mix(a, b, u.x), mix(c, d, u.x), u.y) * 1.4;
}

mat2 rot(float a) {
  float c = cos(a), s = sin(a);
  return mat2(c, -s, s, c);
}

// Fractal sum of gradient noise, roughly 0..1.
float gfbm(vec2 p) {
  float s = 0.0;
  float a = 0.5;
  for (int i = 0; i < 4; i++) {
    s += a * gnoise(p);
    p = rot(0.5) * p * 2.02 + vec2(11.7, 3.1);
    a *= 0.5;
  }
  return 0.5 + 0.5 * s / 0.9375;
}

vec3 ramp3(vec3 a, vec3 b, vec3 c, float k) {
  k = clamp(k, 0.0, 1.0);
  return k < 0.5 ? mix(a, b, smoothstep(0.0, 1.0, k * 2.0)) : mix(b, c, smoothstep(0.0, 1.0, k * 2.0 - 1.0));
}

// Same as ramp3 but wraps back to the first colour, for endless cycling.
vec3 cycle3(vec3 a, vec3 b, vec3 c, float k) {
  k = fract(k) * 3.0;
  if (k < 1.0) return mix(a, b, smoothstep(0.0, 1.0, k));
  if (k < 2.0) return mix(b, c, smoothstep(0.0, 1.0, k - 1.0));
  return mix(c, a, smoothstep(0.0, 1.0, k - 2.0));
}

// Centre-origin coordinates where y spans -0.5..0.5 and x spans -aspect/2..aspect/2.
vec2 centred() {
  return (gl_FragCoord.xy - 0.5 * RENDERSIZE.xy) / RENDERSIZE.y;
}

float aspect() {
  return RENDERSIZE.x / RENDERSIZE.y;
}

float line(float d, float w) {
  return 1.0 - smoothstep(w - 0.5, w + 0.5, d);
}

void main() {
  vec2 gp = gl_FragCoord.xy - 0.5 * RENDERSIZE.xy;
  vec2 size = RENDERSIZE.xy;
  vec2 uv = gl_FragCoord.xy / size;
  float cellPx = size.y / rows;

  float chk = mod(floor(gp.x / cellPx) + floor(gp.y / cellPx), 2.0);
  vec3 col = vec3(showChecker ? 0.05 + 0.05 * chk : 0.06);

  vec2 q = abs(mod(gp + 0.5 * cellPx, cellPx) - 0.5 * cellPx);
  float grid = line(min(q.x, q.y), 1.0);
  col = mix(col, lineColor.rgb * 0.5, grid);
  col = mix(col, lineColor.rgb, line(min(abs(gp.x), abs(gp.y)), 1.5));

  if (showDiagonals) {
    float d1 = abs(gp.x * size.y - gp.y * size.x) / length(size);
    float d2 = abs(gp.x * size.y + gp.y * size.x) / length(size);
    col = mix(col, lineColor.rgb * 0.7, line(min(d1, d2), 1.0));
  }
  if (showCircles) {
    float r = length(gp);
    float rings = min(abs(r - 0.5 * size.y), min(abs(r - 0.33 * size.y), abs(r - 0.16 * size.y)));
    col = mix(col, lineColor.rgb, line(rings, 1.5));
  }
  if (showBars) {
    float bx = (uv.x - 0.12) / 0.76;
    if (bx > 0.0 && bx < 1.0) {
      float yv = uv.y;
      if (yv > 0.11 && yv < 0.21) {
        float k = floor(bx * 7.0);
        vec3 bar = k < 1.0 ? vec3(1.0) : k < 2.0 ? vec3(1.0, 1.0, 0.0) : k < 3.0 ? vec3(0.0, 1.0, 1.0)
                 : k < 4.0 ? vec3(0.0, 1.0, 0.0) : k < 5.0 ? vec3(1.0, 0.0, 1.0) : k < 6.0 ? vec3(1.0, 0.0, 0.0) : vec3(0.0, 0.0, 1.0);
        col = bar;
      } else if (yv > 0.22 && yv < 0.27) {
        col = vec3(floor(bx * 11.0) / 10.0);
      }
    }
  }

  // Border and orientation markers.
  float edge = min(min(gl_FragCoord.x, size.x - gl_FragCoord.x), min(gl_FragCoord.y, size.y - gl_FragCoord.y));
  col = mix(col, lineColor.rgb, line(abs(edge - 3.0), 2.0));
  float m = 0.07 * size.y;
  float inset = 0.03 * size.y;
  vec2 fc = gl_FragCoord.xy;
  vec2 bl = fc - vec2(inset);
  vec2 tr = size - fc - vec2(inset);
  if (bl.x > 0.0 && bl.x < m && bl.y > 0.0 && bl.y < m) col = vec3(0.1, 0.3, 1.0);
  if (tr.x > 0.0 && tr.x < m && bl.y > 0.0 && bl.y < m) col = vec3(1.0, 0.9, 0.1);
  if (bl.x > 0.0 && bl.x < m && tr.y > 0.0 && tr.y < m) col = vec3(1.0, 0.1, 0.1);
  if (tr.x > 0.0 && tr.x < m && tr.y > 0.0 && tr.y < m) col = vec3(0.1, 0.9, 0.2);

  gl_FragColor = vec4(col, 1.0);
}
