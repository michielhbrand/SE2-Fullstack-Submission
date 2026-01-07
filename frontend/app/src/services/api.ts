import axios from 'axios'
import type { AxiosError, InternalAxiosRequestConfig } from 'axios'
import { authService } from './auth'

const API_URL = 'http://localhost:5000'

// Create axios instance
const apiClient = axios.create({
  baseURL: API_URL,
  headers: {
    'Content-Type': 'application/json',
  },
})

// Request interceptor to add auth token
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

// Response interceptor to handle 401 errors
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

// Template API functions
export const templateApi = {
  // Get all templates
  getTemplates: async () => {
    const response = await apiClient.get('http://localhost:5001/api/Template')
    return response.data
  },

  // Get a specific template
  getTemplate: async (templateName: string) => {
    const response = await apiClient.get(`http://localhost:5001/api/Template/${encodeURIComponent(templateName)}`)
    return response.data
  },

  // Upload a new template
  uploadTemplate: async (name: string, content: string) => {
    const response = await apiClient.post('http://localhost:5001/api/Template', {
      name,
      content
    })
    return response.data
  },

  // Get preview URL for a template
  getPreviewUrl: (templateName: string) => {
    return `http://localhost:5001/api/Template/${encodeURIComponent(templateName)}/preview`
  }
}

export default apiClient
