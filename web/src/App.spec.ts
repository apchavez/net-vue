import { describe, it, expect, beforeEach } from "vitest";
import { mount } from "@vue/test-utils";
import { createPinia, setActivePinia } from "pinia";
import { createRouter, createMemoryHistory } from "vue-router";
import vuetify from "./plugins/vuetify";
import { useAuthStore } from "./stores/auth";
import App from "./App.vue";

describe("App", () => {
  let router: ReturnType<typeof createRouter>;

  beforeEach(async () => {
    localStorage.clear();
    setActivePinia(createPinia());
    router = createRouter({
      history: createMemoryHistory(),
      routes: [
        { path: "/", component: { template: "<div>home</div>" } },
        { path: "/login", component: { template: "<div>login</div>" } },
      ],
    });
    router.push("/");
    await router.isReady();
  });

  it("hides the app bar when not authenticated", async () => {
    const wrapper = mount(App, { global: { plugins: [router, vuetify] } });
    await wrapper.vm.$nextTick();

    expect(wrapper.find(".v-app-bar").exists()).toBe(false);
  });

  it("shows the username and a logout button when authenticated", async () => {
    useAuthStore().setSession("tok", "admin", ["ADMIN", "USER"]);
    const wrapper = mount(App, { global: { plugins: [router, vuetify] } });
    await wrapper.vm.$nextTick();

    expect(wrapper.text()).toContain("admin");
    const logoutButton = wrapper
      .findAll("button")
      .find((b) => b.text().includes("Logout"));
    expect(logoutButton).toBeDefined();
  });

  it("logging out clears the session and redirects to /login", async () => {
    const auth = useAuthStore();
    auth.setSession("tok", "admin", ["ADMIN", "USER"]);
    const wrapper = mount(App, { global: { plugins: [router, vuetify] } });
    await wrapper.vm.$nextTick();

    const logoutButton = wrapper
      .findAll("button")
      .find((b) => b.text().includes("Logout"))!;
    await logoutButton.trigger("click");
    await new Promise((resolve) => setTimeout(resolve, 0));

    expect(auth.isAuthenticated).toBe(false);
    expect(router.currentRoute.value.fullPath).toBe("/login");
  });
});
