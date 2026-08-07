import axios from "axios";
import { useAuthStore } from "../stores/auth";
import router from "../router";

const httpClient = axios.create({
  baseURL: import.meta.env.VITE_API_URL ?? "http://localhost:8080",
});

httpClient.interceptors.request.use((config) => {
  const auth = useAuthStore();
  if (auth.token) {
    config.headers.Authorization = `Bearer ${auth.token}`;
  }
  return config;
});

httpClient.interceptors.response.use(
  (response) => response,
  (error) => {
    console.error(
      JSON.stringify({
        event: "api_request_failed",
        method: error.config?.method,
        url: error.config?.url,
        status: error.response?.status,
        message: error.message,
      }),
    );
    if (error.response?.status === 401) {
      const auth = useAuthStore();
      auth.logout();
      router.push("/login");
    }
    return Promise.reject(error);
  },
);

export default httpClient;
