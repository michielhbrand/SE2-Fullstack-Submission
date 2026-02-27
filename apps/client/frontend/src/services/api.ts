import { ApiClient, type UserRole } from '../api/generated/api-client'
import { apiClient } from '../api/http-client'

const client = new ApiClient(undefined, apiClient)

export const TemplateType = {
  Invoice: 0,
  Quote: 1
} as const
export type TemplateType = (typeof TemplateType)[keyof typeof TemplateType]

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
  getAllUsers: async (page: number = 1, pageSize: number = 100, searchTerm?: string) => {
    return await client.user_GetUserDirectory(page, pageSize, searchTerm)
  },

  // Update user role (admin only)
  updateUserRole: async (userId: string, role: UserRole) => {
    return await client.user_UpdateUserRole(userId, { role })
  },

  // Create a new user (admin only)
  createUser: async (userData: {
    username: string
    email: string
    firstName: string
    lastName: string
    password: string
    role: string
    organizationId?: number
  }) => {
    return await client.user_CreateUser(userData)
  },

  updateUser: async (userId: string, userData: {
    firstName: string
    lastName: string
    role: UserRole
    active: boolean
  }) => {
    return await client.user_UpdateUserDetails(userId, userData)
  }
}

// Template API functions
export const templateApi = {
  // Get all templates for an organization
  getTemplates: async (organizationId: number, page: number = 1, pageSize: number = 25) => {
    return await client.template_GetTemplates(organizationId, page, pageSize)
  },

  // Get templates filtered by type for an organization
  getTemplatesByType: async (organizationId: number, type: TemplateType) => {
    return await client.template_GetTemplatesByType(organizationId, type)
  },

  // Get a specific template
  getTemplate: async (id: number) => {
    return await client.template_GetTemplate(id)
  },

  // Create a new template
  createTemplate: async (name: string, version: number, content: string, type: TemplateType = TemplateType.Invoice, organizationId?: number) => {
    return await client.template_CreateTemplate({ name, version, content, type }, organizationId)
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
  // Get all quotes for an organization
  getQuotes: async (organizationId: number, page: number = 1, pageSize: number = 10) => {
    return await client.quote_GetQuotes(organizationId, page, pageSize)
  },

  // Get a specific quote
  getQuote: async (id: number) => {
    return await client.quote_GetQuote(id)
  },

  // Create a new quote
  createQuote: async (quoteData: any, organizationId?: number) => {
    return await client.quote_CreateQuote(quoteData, organizationId)
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
  }
}

// Client API functions
export const clientApi = {
  // Get all clients for an organization
  getClients: async (organizationId: number, page: number = 1, pageSize: number = 10, search?: string) => {
    return await client.client_GetClients(organizationId, page, pageSize, search)
  },

  // Get a specific client
  getClient: async (id: number) => {
    return await client.client_GetClient(id)
  },

  // Create a new client
  createClient: async (clientData: any, organizationId?: number) => {
    return await client.client_CreateClient(clientData, organizationId)
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
  // Get all invoices for an organization
  getInvoices: async (organizationId: number, page: number = 1, pageSize: number = 10, statusFilter?: string, search?: string) => {
    const response = await apiClient.get('/api/invoice', {
      params: { organizationId, page, pageSize, statusFilter: statusFilter || undefined, search: search || undefined }
    })
    return JSON.parse(response.data)
  },

  // Get a specific invoice
  getInvoice: async (id: number) => {
    return await client.invoice_GetInvoice(id)
  },

  // Create a new invoice
  createInvoice: async (invoiceData: any, organizationId?: number) => {
    return await client.invoice_CreateInvoice(invoiceData, organizationId)
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

  // Manually trigger the overdue invoice check for an organisation
  processOverdue: async (organizationId: number) => {
    return await client.invoice_ProcessOverdue(organizationId)
  }
}

// Organization API functions
export const organizationApi = {
  // Get all organizations (admin)
  getOrganizations: async () => {
    return await client.organization_GetOrganizations()
  },

  // Get organizations for the current user
  getMyOrganizations: async () => {
    return await client.organizationMember_GetMyOrganizations()
  },

  // Get a specific organization
  getOrganization: async (id: number) => {
    return await client.organization_GetOrganization(id)
  },

  // Create a new organization
  createOrganization: async (organizationData: any) => {
    return await client.organization_CreateOrganization(organizationData)
  },

  // Update an organization
  updateOrganization: async (id: number, organizationData: any) => {
    return await client.organization_UpdateOrganization(id, organizationData)
  },

  // Delete an organization
  deleteOrganization: async (id: number) => {
    return await client.organization_DeleteOrganization(id)
  },

  // Get members of an organization
  getOrganizationMembers: async (orgId: number) => {
    return await client.organizationMember_GetOrganizationMembers(orgId)
  }
}

// Health API functions
export const healthApi = {
  // Get health status
  getHealth: async () => {
    return await client.health_GetHealth()
  }
}

// Workflow API functions
export const workflowApi = {
  // Get paginated list of workflows for an organization
  getWorkflows: async (organizationId: number, page: number = 1, pageSize: number = 10, search?: string, statuses?: string[]) => {
    return await client.workflow_GetWorkflows(organizationId, page, pageSize, search, statuses?.join(','))
  },

  // Get a specific workflow by ID with all events
  getWorkflow: async (id: number) => {
    return await client.workflow_GetWorkflow(id)
  },

  // Create a new workflow
  createWorkflow: async (data: { type: string; clientId: number; quoteId?: number; invoiceId?: number }, organizationId: number) => {
    return await client.workflow_CreateWorkflow(data, organizationId)
  },

  // Add an event to a workflow
  addEvent: async (workflowId: number, data: { eventType: string; description?: string; payByDays?: number }) => {
    return await client.workflow_AddEvent(workflowId, data)
  },

  // Cancel a workflow
  cancelWorkflow: async (id: number) => {
    return await client.workflow_CancelWorkflow(id)
  },

  // Terminate a workflow
  terminateWorkflow: async (id: number) => {
    return await client.workflow_TerminateWorkflow(id)
  },

  // Get workflow by quote ID
  getWorkflowByQuote: async (quoteId: number) => {
    return await client.workflow_GetWorkflowByQuote(quoteId)
  },

  // Get workflow by invoice ID
  getWorkflowByInvoice: async (invoiceId: number) => {
    return await client.workflow_GetWorkflowByInvoice(invoiceId)
  },

  // Convert quote to invoice
  convertQuoteToInvoice: async (data: { quoteId: number; templateId?: number; organizationId: number }) => {
    return await client.invoice_ConvertQuoteToInvoice(data)
  }
}

// Export the generated client instance for direct access if needed
export { client as generatedClient }

// Export the configured axios instance for backward compatibility
export { apiClient as default } from '../api/http-client'
