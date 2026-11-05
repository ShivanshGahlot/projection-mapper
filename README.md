# Projection Mapper

Projection mapping editor for the browser. Corner-pin images, videos, text, webcam feeds, and ISF shaders to physical surfaces, then send the scene to a separate projector window.

## Run locally

Use Node.js 20 or later, npm, and Chrome with WebGL2 enabled.

```sh
npm ci
npm run dev
```

Open the local URL printed by Vite. For a production build, run `npm run build` and serve `dist/` over HTTP on localhost or over HTTPS.

## Map a scene

1. Add a surface layer. Use its alignment grid and drag the corners or edge handles in the Output panel to fit your surface.
2. Choose a fill in Properties, or select a shader layer and load an ISF `.fs` file. To turn a mapped surface into a cutout, set **Type** to **Mask**. Use **Invert mask** to keep only the inside.
3. Click the monitor button, move the output window onto the projector display, and press **F** in that window for fullscreen. Use **Sync videos** in the editor to restart the video layers together.

Projects autosave in the browser. Use **File → Export Project** to back up your work and media as a `.mapping.json` file, especially before switching browsers or changing the local server URL.

The Corner 1 Diya Folk shader in `examples/shaders/` can be imported into a shader layer for a two-wall setup.

## Verify changes

```sh
npm run build
```

With the dev server running, run the shader self-test in Chrome. Adjust the URL to match your server.

```sh
npm run selftest -- http://localhost:5173/selftest.html
```

The runner defaults to Chrome on macOS. Set the `CHROME` environment variable to the browser executable on other systems and pass `--angle=swiftshader` if needed.
