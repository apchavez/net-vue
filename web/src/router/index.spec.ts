import { describe, it, expect, beforeEach } from "vitest";
import { setActivePinia, createPinia } from "pinia";
import { useAuthStore } from "../stores/auth";
import router from "./index";

describe("router guard", () => {
  beforeEach(async () => {
    localStorage.clear();
    setActivePinia(createPinia());
    await router.push("/login");
  });

  it("redirects to /login when navigating to a protected route unauthenticated", async () => {
    await router.push("/products");
    expect(router.currentRoute.value.name).toBe("login");
  });

  it("allows navigating to a protected route when authenticated", async () => {
    useAuthStore().setSession("tok", "admin", ["ADMIN"]);
    await router.push("/products");
    expect(router.currentRoute.value.name).toBe("products");
  });

  it("does not gate /login itself", async () => {
    await router.push("/login");
    expect(router.currentRoute.value.name).toBe("login");
  });

  it("redirects an unknown path to /products (which then gates to /login if unauthenticated)", async () => {
    await router.push("/does-not-exist");
    expect(router.currentRoute.value.name).toBe("login");
  });

  it("redirects an unknown path to /products when authenticated", async () => {
    useAuthStore().setSession("tok", "admin", ["ADMIN"]);
    await router.push("/does-not-exist");
    expect(router.currentRoute.value.name).toBe("products");
  });
});
