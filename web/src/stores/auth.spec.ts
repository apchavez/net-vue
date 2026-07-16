import { describe, it, expect, beforeEach } from "vitest";
import { setActivePinia, createPinia } from "pinia";
import { useAuthStore } from "./auth";

describe("auth store", () => {
  beforeEach(() => {
    localStorage.clear();
    setActivePinia(createPinia());
  });

  it("starts unauthenticated", () => {
    const store = useAuthStore();
    expect(store.isAuthenticated).toBe(false);
  });

  it("sets session and persists to localStorage", () => {
    const store = useAuthStore();
    store.setSession("tok", "admin", ["ADMIN", "USER"]);
    expect(store.isAuthenticated).toBe(true);
    expect(store.isAdmin).toBe(true);
    expect(localStorage.getItem("auth_token")).toBe("tok");
  });

  it("logs out and clears localStorage", () => {
    const store = useAuthStore();
    store.setSession("tok", "user", ["USER"]);
    store.logout();
    expect(store.isAuthenticated).toBe(false);
    expect(store.isAdmin).toBe(false);
    expect(localStorage.getItem("auth_token")).toBeNull();
  });

  it("isAdmin is false for a USER-only role", () => {
    const store = useAuthStore();
    store.setSession("tok", "user", ["USER"]);
    expect(store.isAdmin).toBe(false);
  });
});
