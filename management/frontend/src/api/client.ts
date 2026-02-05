import axios, { AxiosInstance } from 'axios'
import { ManagementApiClient } from './generated/api-client'

// Create axios instance with interceptors
const createAxiosInstance = (): AxiosInstance => {
  const instance = axios.create({
    baseURL: import.meta.env.VITE_API_URL || 'http://localhost:5002',
    headers: {
      'Content-Type': 'application/json',
    },
    // Disable automatic JSON parsing so NSwag's generated JSON.parse() works correctly
    transformResponse: [(data) => data],
  })

  // Request interceptor to add auth token
  instance.interceptors.request.use(
    (config) => {
      const token = localStorage.getItem('access_token')
      if (token) {
        config.headers.Authorization = `Bearer ${token}`
      }
      return config
    },
    (error) => {
      return Promise.reject(error)
    }
  )

  // Response interceptor to handle token refresh
  instance.interceptors.response.use(
    (response) => response,
    async (error) => {
      const originalRequest = error.config

      if (error.response?.status === 401 && !originalRequest._retry) {
        originalRequest._retry = true

        try {
          const refreshToken = localStorage.getItem('refresh_token')
          if (refreshToken) {
            // Create a temporary client without interceptors to avoid infinite loop
            const tempInstance = axios.create({
              transformResponse: [(data) => data],
            })
            const tempClient = new ManagementApiClient(
              import.meta.env.VITE_API_URL || 'http://localhost:5002',
              tempInstance
            )

            const response = await tempClient.refreshToken({
              RefreshToken: refreshToken,
            })

            const { AccessToken, RefreshToken: newRefreshToken } = response
            if (AccessToken) {
              localStorage.setItem('access_token', AccessToken)
              if (newRefreshToken) {
                localStorage.setItem('refresh_token', newRefreshToken)
              }

              originalRequest.headers.Authorization = `Bearer ${AccessToken}`
              return instance(originalRequest)
            }
          }
        } catch (refreshError) {
          // If refresh fails, clear tokens and redirect to login
          localStorage.removeItem('access_token')
          localStorage.removeItem('refresh_token')
          localStorage.removeItem('token_expires_at')
          window.location.href = '/login'
          return Promise.reject(refreshError)
        }
      }

      return Promise.reject(error)
    }
  )

  return instance
}

// Create and export the API client instance
const axiosInstance = createAxiosInstance()
export const apiClient = new ManagementApiClient(
  import.meta.env.VITE_API_URL || 'http://localhost:5002',
  axiosInstance
)

// Export the axios instance for any direct usage if needed
export const axiosClient = axiosInstance
