/*{
  "DESCRIPTION": "A single flat colour. Handy for lighting a surface or blocking out an area.",
  "CREDIT": "Built-in",
  "ISFVSN": "2",
  "CATEGORIES": ["Generator", "Utility"],
  "INPUTS": [
    {"NAME": "color", "TYPE": "color", "DEFAULT": [1.0, 0.55, 0.15, 1.0], "LABEL": "Colour"},
    {"NAME": "brightness", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.0, "MAX": 1.0, "LABEL": "Brightness"}
  ]
}*/

void main() {
  gl_FragColor = vec4(color.rgb * brightness, 1.0);
}
