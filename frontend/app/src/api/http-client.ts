import axios, { type AxiosInstance } from 'axios'
import type { AxiosError, InternalAxiosRequestConfig } from 'axios'
import { useAuthStore } from '../stores/auth'
import { toast } from 'vue-sonner'

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

// Flag to prevent multiple simultaneous refresh attempts
let isRefreshing = false
let failedQueue: Array<{
  resolve: (value?: any) => void
  reject: (reason?: any) => void
}> = []

const processQueue = (error: Error | null, token: string | null = null) => {
  failedQueue.forEach(prom => {
    if (error) {
      prom.reject(error)
    } else {
      prom.resolve(token)
    }
  })
  
  failedQueue = []
}

/**
 * Request interceptor to add authentication token
 */
apiClient.interceptors.request.use(
  (config: InternalAxiosRequestConfig) => {
    const authStore = useAuthStore()
    const token = authStore.getAccessToken()
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
 * Response interceptor to handle 401 errors and attempt token refresh
 */
apiClient.interceptors.response.use(
  (response) => {
    return response
  },
  async (error: AxiosError) => {
    const originalRequest = error.config as InternalAxiosRequestConfig & { _retry?: boolean }
    
    // If we get a 401 Unauthorized response, attempt to refresh the token
    if (error.response?.status === 401 && originalRequest && !originalRequest._retry) {
      // Don't attempt refresh for login, logout, or refresh endpoints
      if (originalRequest.url?.includes('/auth/login') ||
          originalRequest.url?.includes('/auth/logout') ||
          originalRequest.url?.includes('/auth/refresh')) {
        const errorData = error.response?.data as any
        const errorMessage = errorData?.detail
          || errorData?.title
          || errorData?.message
          || 'Authentication failed. Please log in again.'
        
        toast.error(errorMessage)
        const authStore = useAuthStore()
        authStore.logout(true)
        return Promise.reject(error)
      }

      if (isRefreshing) {
        // If a refresh is already in progress, queue this request
        return new Promise((resolve, reject) => {
          failedQueue.push({ resolve, reject })
        }).then(token => {
          if (originalRequest.headers) {
            originalRequest.headers.Authorization = `Bearer ${token}`
          }
          return apiClient(originalRequest)
        }).catch(err => {
          return Promise.reject(err)
        })
      }

      originalRequest._retry = true
      isRefreshing = true

      const authStore = useAuthStore()
      
      try {
        // Attempt to refresh the token
        const newToken = await authStore.refreshAccessToken()
        
        if (newToken) {
          // Update the failed request with the new token
          if (originalRequest.headers) {
            originalRequest.headers.Authorization = `Bearer ${newToken}`
          }
          
          // Process queued requests
          processQueue(null, newToken)
          
          // Retry the original request
          return apiClient(originalRequest)
        } else {
          throw new Error('Token refresh failed')
        }
      } catch (refreshError) {
        // Refresh failed, logout the user
        processQueue(refreshError as Error, null)
        
        toast.error('Your session has expired. Please log in again.')
        authStore.logout(true)
        
        return Promise.reject(refreshError)
      } finally {
        isRefreshing = false
      }
    }
    
    return Promise.reject(error)
  }
)

export default apiClient
