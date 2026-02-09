import axios, { AxiosInstance } from "axios";
import { ManagementApiClient } from "./generated/api-client";

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
      const token = localStorage.getItem("access_token");
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
        originalRequest._retry = true;

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

              originalRequest.headers.Authorization = `Bearer ${AccessToken}`;
              return instance(originalRequest);
            }
          }
        } catch (refreshError) {
          localStorage.removeItem("access_token");
          localStorage.removeItem("refresh_token");
          localStorage.removeItem("token_expires_at");
          window.location.href = "/login";
          return Promise.reject(refreshError);
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
