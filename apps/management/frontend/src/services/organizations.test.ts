import { describe, it, expect, vi, beforeEach } from 'vitest'
import { organizationService } from './organizations'
import type { OrganizationResponse } from '../api/generated/api-client'

// Mock the generated API client
vi.mock('../api/client', () => ({
  apiClient: {
    getOrganizations: vi.fn(),
    createOrganization: vi.fn(),
    updateOrganization: vi.fn(),
  },
}))

const mockOrgs: OrganizationResponse[] = [
  { Id: 1, Name: 'Acme Corp', Email: 'acme@example.com' } as OrganizationResponse,
  { Id: 2, Name: 'Beta Ltd', Email: 'beta@example.com' } as OrganizationResponse,
]

describe('organizationService', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  describe('getAll()', () => {
    it('delegates to apiClient.getOrganizations()', async () => {
      const { apiClient } = await import('../api/client')
      vi.mocked(apiClient.getOrganizations).mockResolvedValueOnce(mockOrgs)

      const result = await organizationService.getAll()

      expect(apiClient.getOrganizations).toHaveBeenCalledOnce()
      expect(result).toEqual(mockOrgs)
    })

    it('passes optional query params through', async () => {
      const { apiClient } = await import('../api/client')
      vi.mocked(apiClient.getOrganizations).mockResolvedValueOnce([])

      await organizationService.getAll({ search: 'acme', sortBy: 'name' })

      expect(apiClient.getOrganizations).toHaveBeenCalledWith(
        'acme',
        undefined,
        'name',
        undefined,
      )
    })
  })

  describe('getById()', () => {
    // NOTE: There is no backend GET /organizations/:id endpoint.
    // This method fetches all organisations and filters client-side — an anti-pattern
    // that performs an O(n) full-list fetch for every single-resource lookup.
    it('calls getOrganizations() then filters by id', async () => {
      const { apiClient } = await import('../api/client')
      vi.mocked(apiClient.getOrganizations).mockResolvedValueOnce(mockOrgs)

      const result = await organizationService.getById(2)

      expect(apiClient.getOrganizations).toHaveBeenCalledOnce()
      expect(result?.Id).toBe(2)
    })

    it('returns undefined when id is not found', async () => {
      const { apiClient } = await import('../api/client')
      vi.mocked(apiClient.getOrganizations).mockResolvedValueOnce(mockOrgs)

      const result = await organizationService.getById(999)

      expect(result).toBeUndefined()
    })
  })

  describe('create()', () => {
    it('delegates to apiClient.createOrganization()', async () => {
      const { apiClient } = await import('../api/client')
      const created = mockOrgs[0]!
      vi.mocked(apiClient.createOrganization).mockResolvedValueOnce(created)

      const payload = { Name: 'Acme Corp' } as Parameters<typeof organizationService.create>[0]
      const result = await organizationService.create(payload)

      expect(apiClient.createOrganization).toHaveBeenCalledWith(payload)
      expect(result).toBe(created)
    })
  })

  describe('update()', () => {
    it('calls updateOrganization() then refetches via getById()', async () => {
      const { apiClient } = await import('../api/client')
      vi.mocked(apiClient.updateOrganization).mockResolvedValueOnce(undefined)
      // getById calls getOrganizations internally
      vi.mocked(apiClient.getOrganizations).mockResolvedValueOnce(mockOrgs)

      const payload = { Name: 'Acme Updated' } as Parameters<typeof organizationService.update>[1]
      const result = await organizationService.update(1, payload)

      expect(apiClient.updateOrganization).toHaveBeenCalledWith(1, payload)
      expect(apiClient.getOrganizations).toHaveBeenCalledOnce()
      expect(result?.Id).toBe(1)
    })
  })
})
