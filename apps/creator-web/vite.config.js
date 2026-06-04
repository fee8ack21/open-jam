import { defineConfig } from 'vite';
import vue from '@vitejs/plugin-vue';

// https://vite.dev/config/
export default defineConfig({
  plugins: [
    // Enabled for when you convert the .js template-string components
    // into single-file components (.vue). Harmless until then.
    vue(),
  ],
  resolve: {
    alias: {
      // The components use string `template:` options, which need the
      // runtime template compiler. The esm-bundler full build includes it.
      // (Remove this alias once every component is a precompiled .vue SFC.)
      vue: 'vue/dist/vue.esm-bundler.js',
    },
  },
  server: {
    port: 5173,
    open: true,
  },
});
