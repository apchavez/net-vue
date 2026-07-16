import httpClient from "./httpClient";

export interface LoginResponse {
  token: string;
  tokenType: string;
  expiresIn: number;
  username: string;
  roles: string[];
}

export function login(username: string, password: string) {
  return httpClient.post<LoginResponse>("/api/v1/auth/login", {
    username,
    password,
  });
}
