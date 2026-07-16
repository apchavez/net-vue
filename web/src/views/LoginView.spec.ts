import { describe, it, expect, vi, beforeEach } from "vitest";
import { mount } from "@vue/test-utils";
import { createPinia, setActivePinia } from "pinia";
import { createRouter, createMemoryHistory } from "vue-router";
import vuetify from "../plugins/vuetify";
import LoginView from "./LoginView.vue";

vi.mock("../api/authApi", () => ({
  login: vi.fn(),
}));

import { login } from "../api/authApi";

describe("LoginView", () => {
  let router: ReturnType<typeof createRouter>;

  beforeEach(() => {
    setActivePinia(createPinia());
    router = createRouter({
      history: createMemoryHistory(),
      routes: [
        { path: "/", component: LoginView },
        { path: "/products", component: { template: "<div />" } },
      ],
    });
    vi.clearAllMocks();
  });

  it("renders the login form", () => {
    const wrapper = mount(LoginView, {
      global: { plugins: [router, vuetify] },
    });
    expect(wrapper.find('[data-testid="submit"]').exists()).toBe(true);
  });

  it("shows an error on invalid credentials", async () => {
    vi.mocked(login).mockRejectedValueOnce(new Error("401"));
    const wrapper = mount(LoginView, {
      global: { plugins: [router, vuetify] },
    });
    await wrapper.find("form").trigger("submit.prevent");
    await flushPromises();
    expect(wrapper.text()).toContain("Invalid username or password");
  });
});

function flushPromises() {
  return new Promise((resolve) => setTimeout(resolve, 0));
}
