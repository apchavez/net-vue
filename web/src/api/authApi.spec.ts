import { describe, it, expect, vi, beforeEach } from "vitest";

vi.mock("./httpClient", () => ({
  default: { post: vi.fn() },
}));

import httpClient from "./httpClient";
import { login } from "./authApi";

describe("authApi", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("posts credentials to /api/v1/auth/login", async () => {
    vi.mocked(httpClient.post).mockResolvedValue({
      data: {
        token: "tok",
        tokenType: "Bearer",
        expiresIn: 3600,
        username: "admin",
        roles: ["ADMIN"],
      },
    } as never);

    await login("admin", "admin123");

    expect(httpClient.post).toHaveBeenCalledWith("/api/v1/auth/login", {
      username: "admin",
      password: "admin123",
    });
  });

  it("propagates a rejection on invalid credentials", async () => {
    vi.mocked(httpClient.post).mockRejectedValueOnce(
      new Error("Request failed with status code 401"),
    );

    await expect(login("admin", "wrong")).rejects.toThrow(
      "Request failed with status code 401",
    );
  });
});
