import { apiClient } from '../api/client'
import type {
  OrganizationResponse,
  CreateOrganizationRequest,
  UpdateOrganizationRequest,
} from '../api/generated/api-client'

/**
 * Service for managing organizations using the generated API client
 */
export const organizationService = {
  /**
   * Get all organizations
   */
  async getAll(): Promise<OrganizationResponse[]> {
    return await apiClient.getOrganizations()
  },

  /**
   * Get organization by ID
   * Note: Fetches all organizations and filters by ID since there's no single GET endpoint
   */
  async getById(id: number): Promise<OrganizationResponse | undefined> {
    const organizations = await apiClient.getOrganizations()
    return organizations.find((org) => org.Id === id)
  },

  /**
   * Create a new organization
   */
  async create(data: CreateOrganizationRequest): Promise<OrganizationResponse> {
    return await apiClient.createOrganization(data)
  },

  /**
   * Update an existing organization
   * Note: Returns void, so we fetch the updated organization after update
   */
  async update(id: number, data: UpdateOrganizationRequest): Promise<OrganizationResponse | undefined> {
    await apiClient.updateOrganization(id, data)
    // Fetch the updated organization
    return await this.getById(id)
  },

  /**
   * Delete an organization
   */
  async delete(id: number): Promise<void> {
    return await apiClient.deleteOrganization(id)
  },
}
