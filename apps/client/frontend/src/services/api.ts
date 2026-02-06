import { ApiClient, type UserRole } from '../api/generated/api-client'
import { apiClient } from '../api/http-client'

// Create a single instance of the generated API client with our configured axios instance
const client = new ApiClient(undefined, apiClient)

// Auth API functions
export const authApi = {
  // User login
  login: async (username: string, password: string) => {
    return await client.auth_Login({ username, password })
  },

  // Admin login
  adminLogin: async (username: string, password: string) => {
    return await client.auth_AdminLogin({ username, password })
  },

  // Refresh token
  refreshToken: async (refreshToken: string) => {
    return await client.auth_RefreshToken({ refreshToken })
  },

  // Logout
  logout: async (refreshToken: string) => {
    return await client.auth_Logout({ refreshToken })
  },

  // Get all users (admin only)
  getAllUsers: async () => {
    return await client.auth_GetAllUsers()
  },

  // Update user role (admin only)
  updateUserRole: async (userId: string, role: UserRole) => {
    return await client.auth_UpdateUserRole(userId, { role })
  },

  // Create a new user (admin only)
  createUser: async (userData: {
    username: string
    email: string
    firstName: string
    lastName: string
    password: string
    role: string
  }) => {
    return await client.auth_CreateUser(userData)
  }
}

// Template API functions
export const templateApi = {
  // Get all templates
  getTemplates: async (page: number = 1, pageSize: number = 100) => {
    return await client.template_GetTemplates(page, pageSize)
  },

  // Get a specific template
  getTemplate: async (id: number) => {
    return await client.template_GetTemplate(id)
  },

  // Create a new template
  createTemplate: async (name: string, version: number, content: string) => {
    return await client.template_CreateTemplate({ name, version, content })
  },

  // Delete a template
  deleteTemplate: async (id: number) => {
    return await client.template_DeleteTemplate(id)
  },

  // Get preview URL for a template
  getPreviewUrl: async (id: number) => {
    return await client.template_GetTemplatePreviewUrl(id)
  }
}

// Quote API functions
export const quoteApi = {
  // Get all quotes
  getQuotes: async (page: number = 1, pageSize: number = 10) => {
    return await client.quote_GetQuotes(page, pageSize)
  },

  // Get a specific quote
  getQuote: async (id: number) => {
    return await client.quote_GetQuote(id)
  },

  // Create a new quote
  createQuote: async (quoteData: any) => {
    return await client.quote_CreateQuote(quoteData)
  },

  // Update a quote
  updateQuote: async (id: number, quoteData: any) => {
    return await client.quote_UpdateQuote(id, quoteData)
  },

  // Delete a quote
  deleteQuote: async (id: number) => {
    return await client.quote_DeleteQuote(id)
  },

  // Get PDF URL for a quote
  getPdfUrl: async (id: number) => {
    return await client.quote_GetQuotePdfUrl(id)
  },

  // Get quote templates
  getTemplates: async () => {
    return await client.quote_GetTemplates()
  }
}

// Client API functions
export const clientApi = {
  // Get all clients
  getClients: async (page: number = 1, pageSize: number = 10, search?: string) => {
    return await client.client_GetClients(page, pageSize, search)
  },

  // Get a specific client
  getClient: async (id: number) => {
    return await client.client_GetClient(id)
  },

  // Create a new client
  createClient: async (clientData: any) => {
    return await client.client_CreateClient(clientData)
  },

  // Update a client
  updateClient: async (id: number, clientData: any) => {
    return await client.client_UpdateClient(id, clientData)
  },

  // Delete a client
  deleteClient: async (id: number) => {
    return await client.client_DeleteClient(id)
  }
}

// Invoice API functions
export const invoiceApi = {
  // Get all invoices
  getInvoices: async (page: number = 1, pageSize: number = 10) => {
    return await client.invoice_GetInvoices(page, pageSize)
  },

  // Get a specific invoice
  getInvoice: async (id: number) => {
    return await client.invoice_GetInvoice(id)
  },

  // Create a new invoice
  createInvoice: async (invoiceData: any) => {
    return await client.invoice_CreateInvoice(invoiceData)
  },

  // Update an invoice
  updateInvoice: async (id: number, invoiceData: any) => {
    return await client.invoice_UpdateInvoice(id, invoiceData)
  },

  // Delete an invoice
  deleteInvoice: async (id: number) => {
    return await client.invoice_DeleteInvoice(id)
  },

  // Get PDF URL for an invoice
  getPdfUrl: async (id: number) => {
    return await client.invoice_GetInvoicePdfUrl(id)
  },

  // Get invoice templates
  getTemplates: async () => {
    return await client.invoice_GetTemplates()
  }
}

// Health API functions
export const healthApi = {
  // Get health status
  getHealth: async () => {
    return await client.health_GetHealth()
  }
}

// Export the generated client instance for direct access if needed
export { client as generatedClient }

// Export the configured axios instance for backward compatibility
export { apiClient as default } from '../api/http-client'
