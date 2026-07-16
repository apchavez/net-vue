/// <reference types="vitest/config" />
import { defineConfig, configDefaults } from "vitest/config";
import vue from "@vitejs/plugin-vue";

export default defineConfig({
  plugins: [vue()],
  test: {
    environment: "jsdom",
    globals: true,
    css: true,
    setupFiles: ["./src/test/setup.ts"],
    exclude: [...configDefaults.exclude, "e2e/**"],
    server: {
      deps: {
        inline: ["vuetify"],
      },
    },
  },
});
