import js from "@eslint/js";
import pluginVue from "eslint-plugin-vue";
import tseslint from "typescript-eslint";

export default tseslint.config(
  {
    ignores: [
      "dist/**",
      "node_modules/**",
      "playwright-report/**",
      "test-results/**",
    ],
  },
  js.configs.recommended,
  ...tseslint.configs.recommended,
  ...pluginVue.configs["flat/recommended"],
  {
    files: ["**/*.vue"],
    languageOptions: { parserOptions: { parser: tseslint.parser } },
  },
  {
    rules: {
      "vue/multi-word-component-names": "off",
      // Vuetify's dotted dynamic slot names (e.g. #item.actions) are valid Vue syntax but
      // eslint-plugin-vue's valid-v-slot rule mistakes the dotted suffix for a modifier.
      "vue/valid-v-slot": "off",
    },
  },
  {
    files: ["vite.config.ts"],
    rules: {
      "@typescript-eslint/triple-slash-reference": "off",
    },
  },
);
