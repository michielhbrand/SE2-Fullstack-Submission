import axios, { type AxiosInstance } from 'axios'
import type { AxiosError, InternalAxiosRequestConfig } from 'axios'
import { authService } from '../services/auth'

/**
 * Configured Axios instance for the generated API client
 * This instance is used by all generated API client classes
 */
export const apiClient: AxiosInstance = axios.create({
  baseURL: import.meta.env.VITE_API_URL || 'http://localhost:5000',
  headers: {
    'Content-Type': 'application/json',
  },
  // Disable automatic JSON parsing so NSwag's generated JSON.parse() works correctly
  transformResponse: [(data) => data],
})

/**
 * Request interceptor to add authentication token
 */
apiClient.interceptors.request.use(
  (config: InternalAxiosRequestConfig) => {
    const token = authService.getAccessToken()
    if (token && config.headers) {
      config.headers.Authorization = `Bearer ${token}`
    }
    return config
  },
  (error: AxiosError) => {
    return Promise.reject(error)
  }
)

/**
 * Response interceptor to handle 401 errors
 */
apiClient.interceptors.response.use(
  (response) => {
    return response
  },
  (error: AxiosError) => {
    // If we get a 401 Unauthorized response, the token is invalid or expired
    if (error.response?.status === 401) {
      console.log('Received 401 response, logging out...')
      authService.logout(true)
    }
    return Promise.reject(error)
  }
)

export default apiClient
