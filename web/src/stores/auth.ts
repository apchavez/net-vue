import { defineStore } from "pinia";

const TOKEN_KEY = "auth_token";
const ROLES_KEY = "auth_roles";
const USERNAME_KEY = "auth_username";

export const useAuthStore = defineStore("auth", {
  state: () => ({
    token: localStorage.getItem(TOKEN_KEY) as string | null,
    roles: JSON.parse(localStorage.getItem(ROLES_KEY) ?? "[]") as string[],
    username: localStorage.getItem(USERNAME_KEY) as string | null,
  }),
  getters: {
    isAuthenticated: (state) => !!state.token,
    isAdmin: (state) => state.roles.includes("ADMIN"),
  },
  actions: {
    setSession(token: string, username: string, roles: string[]) {
      this.token = token;
      this.username = username;
      this.roles = roles;
      localStorage.setItem(TOKEN_KEY, token);
      localStorage.setItem(USERNAME_KEY, username);
      localStorage.setItem(ROLES_KEY, JSON.stringify(roles));
    },
    logout() {
      this.token = null;
      this.username = null;
      this.roles = [];
      localStorage.removeItem(TOKEN_KEY);
      localStorage.removeItem(USERNAME_KEY);
      localStorage.removeItem(ROLES_KEY);
    },
  },
});
