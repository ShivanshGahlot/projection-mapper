import { defineConfig } from 'vite';
import { resolve } from 'node:path';

export default defineConfig({
  // Relative asset paths so the built app works from any folder.
  base: './',
  build: {
    rollupOptions: {
      input: {
        main: resolve(__dirname, 'index.html'),
        selftest: resolve(__dirname, 'selftest.html'),
      },
    },
  },
});
