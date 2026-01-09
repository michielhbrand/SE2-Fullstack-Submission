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
  getTemplates: async (page: number = 1, pageSize: number = 100) => {
    const response = await apiClient.get('/api/Template', {
      params: { page, pageSize }
    })
    return response.data
  },

  // Get a specific template
  getTemplate: async (id: number) => {
    const response = await apiClient.get(`/api/Template/${id}`)
    return response.data
  },

  // Create a new template
  createTemplate: async (name: string, version: number, content: string) => {
    const response = await apiClient.post('/api/Template', {
      name,
      version,
      content
    })
    return response.data
  },

  // Delete a template
  deleteTemplate: async (id: number) => {
    const response = await apiClient.delete(`/api/Template/${id}`)
    return response.data
  },

  // Get preview URL for a template
  getPreviewUrl: async (id: number) => {
    const response = await apiClient.get(`/api/Template/${id}/preview-url`)
    return response.data
  }
}

// Quote API functions
export const quoteApi = {
  // Get all quotes
  getQuotes: async (page: number = 1, pageSize: number = 10) => {
    const response = await apiClient.get('/api/Quote', {
      params: { page, pageSize }
    })
    return response.data
  },

  // Get a specific quote
  getQuote: async (id: number) => {
    const response = await apiClient.get(`/api/Quote/${id}`)
    return response.data
  },

  // Create a new quote
  createQuote: async (quoteData: any) => {
    const response = await apiClient.post('/api/Quote', quoteData)
    return response.data
  },

  // Update a quote
  updateQuote: async (id: number, quoteData: any) => {
    const response = await apiClient.put(`/api/Quote/${id}`, quoteData)
    return response.data
  },

  // Delete a quote
  deleteQuote: async (id: number) => {
    const response = await apiClient.delete(`/api/Quote/${id}`)
    return response.data
  },

  // Get PDF URL for a quote
  getPdfUrl: async (id: number) => {
    const response = await apiClient.get(`/api/Quote/${id}/pdf-url`)
    return response.data
  },

  // Get quote templates
  getTemplates: async () => {
    const response = await apiClient.get('/api/Quote/templates')
    return response.data
  }
}

export default apiClient
