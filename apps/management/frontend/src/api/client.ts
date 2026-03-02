import axios, { AxiosInstance } from "axios";
import { ManagementApiClient } from "./generated/api-client";
import { useAuthStore } from "../stores/auth";

let isRefreshing = false;
let failedQueue: Array<{
  resolve: (token: string) => void;
  reject: (e: unknown) => void;
}> = [];

const processQueue = (error: unknown, token: string | null) => {
  failedQueue.forEach((prom) => {
    if (error) {
      prom.reject(error);
    } else {
      prom.resolve(token!);
    }
  });
  failedQueue = [];
};

// Create axios instance with interceptors
const createAxiosInstance = (): AxiosInstance => {
  const instance = axios.create({
    baseURL: import.meta.env.VITE_API_URL || "http://localhost:5002",
    headers: {
      "Content-Type": "application/json",
    },
    transformResponse: [(data) => data],
  });

  // Request interceptor to add auth token
  instance.interceptors.request.use(
    (config) => {
      const authStore = useAuthStore();
      const token = authStore.accessToken;
      if (token) {
        config.headers.Authorization = `Bearer ${token}`;
      }
      return config;
    },
    (error) => {
      return Promise.reject(error);
    },
  );

  // Response interceptor to handle token refresh
  instance.interceptors.response.use(
    (response) => response,
    async (error) => {
      const originalRequest = error.config;

      if (error.response?.status === 401 && !originalRequest._retry) {
        if (isRefreshing) {
          return new Promise<string>((resolve, reject) => {
            failedQueue.push({ resolve, reject });
          })
            .then((token) => {
              originalRequest.headers.Authorization = `Bearer ${token}`;
              return instance(originalRequest);
            })
            .catch((err) => Promise.reject(err));
        }

        originalRequest._retry = true;
        isRefreshing = true;

        try {
          const refreshToken = localStorage.getItem("refresh_token");
          if (refreshToken) {
            const tempInstance = axios.create({
              transformResponse: [(data) => data],
            });
            const tempClient = new ManagementApiClient(
              import.meta.env.VITE_API_URL || "http://localhost:5002",
              tempInstance,
            );

            const response = await tempClient.refreshToken({
              RefreshToken: refreshToken,
            });

            const { AccessToken, RefreshToken: newRefreshToken } = response;
            if (AccessToken) {
              localStorage.setItem("access_token", AccessToken);
              if (newRefreshToken) {
                localStorage.setItem("refresh_token", newRefreshToken);
              }

              processQueue(null, AccessToken);
              originalRequest.headers.Authorization = `Bearer ${AccessToken}`;
              return instance(originalRequest);
            }
          }
          throw new Error("No refresh token available");
        } catch (refreshError) {
          processQueue(refreshError, null);
          localStorage.removeItem("access_token");
          localStorage.removeItem("refresh_token");
          localStorage.removeItem("token_expires_at");
          window.location.href = "/login";
          return Promise.reject(refreshError);
        } finally {
          isRefreshing = false;
        }
      }

      return Promise.reject(error);
    },
  );

  return instance;
};

const axiosInstance = createAxiosInstance();
export const apiClient = new ManagementApiClient(
  import.meta.env.VITE_API_URL || "http://localhost:5002",
  axiosInstance,
);

export const axiosClient = axiosInstance;
