// A full-screen quad on attribute 0, shared by every program on a context.

const quads = new WeakMap<WebGL2RenderingContext, WebGLVertexArrayObject>();

export function bindQuad(gl: WebGL2RenderingContext): void {
  let vao = quads.get(gl);
  if (!vao) {
    vao = gl.createVertexArray()!;
    gl.bindVertexArray(vao);
    const buf = gl.createBuffer();
    gl.bindBuffer(gl.ARRAY_BUFFER, buf);
    gl.bufferData(gl.ARRAY_BUFFER, new Float32Array([-1, -1, 1, -1, -1, 1, 1, 1]), gl.STATIC_DRAW);
    gl.enableVertexAttribArray(0);
    gl.vertexAttribPointer(0, 2, gl.FLOAT, false, 0, 0);
    quads.set(gl, vao);
  }
  gl.bindVertexArray(vao);
}

export function drawQuad(gl: WebGL2RenderingContext): void {
  bindQuad(gl);
  gl.drawArrays(gl.TRIANGLE_STRIP, 0, 4);
}
