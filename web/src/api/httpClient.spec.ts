import { describe, it, expect, vi, beforeEach } from "vitest";
import { setActivePinia, createPinia } from "pinia";
import type { AxiosInstance } from "axios";

const pushMock = vi.fn();
vi.mock("../router", () => ({
  default: { push: (...args: unknown[]) => pushMock(...args) },
}));

type RequestConfig = { headers: Record<string, string> };
type ResponseError = { response: { status: number } };

// axios doesn't publicly type interceptor internals; this narrows the
// runtime shape just enough to reach the registered fulfilled/rejected
// handlers without resorting to `any`.
interface InstrumentedAxios extends AxiosInstance {
  interceptors: {
    request: {
      handlers: Array<{
        fulfilled: (config: RequestConfig) => Promise<RequestConfig>;
      }>;
    };
    response: {
      handlers: Array<{ rejected: (error: ResponseError) => Promise<never> }>;
    };
  } & AxiosInstance["interceptors"];
}

describe("httpClient", () => {
  beforeEach(async () => {
    localStorage.clear();
    setActivePinia(createPinia());
    pushMock.mockClear();
    vi.resetModules();
  });

  it("adds the Authorization header when a token is present", async () => {
    const { useAuthStore } = await import("../stores/auth");
    useAuthStore().setSession("tok-123", "admin", ["ADMIN"]);

    const httpClient = (await import("./httpClient"))
      .default as InstrumentedAxios;
    const requestHandler =
      httpClient.interceptors.request.handlers[0].fulfilled;

    const config = await requestHandler({ headers: {} });

    expect(config.headers.Authorization).toBe("Bearer tok-123");
  });

  it("does not add an Authorization header when there is no token", async () => {
    const httpClient = (await import("./httpClient"))
      .default as InstrumentedAxios;
    const requestHandler =
      httpClient.interceptors.request.handlers[0].fulfilled;

    const config = await requestHandler({ headers: {} });

    expect(config.headers.Authorization).toBeUndefined();
  });

  it("logs out and redirects to /login on a 401 response", async () => {
    const { useAuthStore } = await import("../stores/auth");
    const auth = useAuthStore();
    auth.setSession("tok-123", "admin", ["ADMIN"]);

    const httpClient = (await import("./httpClient"))
      .default as InstrumentedAxios;
    const responseErrorHandler =
      httpClient.interceptors.response.handlers[0].rejected;

    await expect(
      responseErrorHandler({ response: { status: 401 } }),
    ).rejects.toBeDefined();

    expect(auth.isAuthenticated).toBe(false);
    expect(pushMock).toHaveBeenCalledWith("/login");
  });

  it("leaves the session untouched on non-401 errors", async () => {
    const { useAuthStore } = await import("../stores/auth");
    const auth = useAuthStore();
    auth.setSession("tok-123", "admin", ["ADMIN"]);

    const httpClient = (await import("./httpClient"))
      .default as InstrumentedAxios;
    const responseErrorHandler =
      httpClient.interceptors.response.handlers[0].rejected;

    await expect(
      responseErrorHandler({ response: { status: 500 } }),
    ).rejects.toBeDefined();

    expect(auth.isAuthenticated).toBe(true);
    expect(pushMock).not.toHaveBeenCalled();
  });
});
