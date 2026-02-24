import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { organizationApi } from '../services/api'
import type { OrganizationResponse } from '../api/generated/api-client'
import { toast } from 'vue-sonner'

export const useOrganizationStore = defineStore('organization', () => {
  const organizations = ref<OrganizationResponse[]>([])
  const currentOrganization = ref<OrganizationResponse | null>(null)
  const isLoading = ref(false)

  // Computed
  const organizationIds = computed(() =>
    organizations.value
      .map((org) => org.id)
      .filter((id): id is number => id !== undefined)
  )
  const hasOrganizations = computed(() => organizations.value.length > 0)
  const currentOrganizationId = computed(() => currentOrganization.value?.id ?? null)
  const hasMultipleOrganizations = computed(() => organizations.value.length > 1)

  // Helper function to extract error message from API error response
  function extractErrorMessage(error: any, defaultMessage: string): string {
    // Case 1: Check for validation errors first (highest priority)
    if (error?.errors && typeof error.errors === 'object') {
      const validationMessages: string[] = []
      for (const field in error.errors) {
        const fieldErrors = error.errors[field]
        if (Array.isArray(fieldErrors)) {
          validationMessages.push(...fieldErrors)
        }
      }
      if (validationMessages.length > 0) {
        return validationMessages.join('. ')
      }
    }
    
    // Case 2: NSwag-generated client throws the parsed Problem Details object directly
    if (error?.detail || error?.title) {
      return error.detail || error.title || defaultMessage
    }
    
    // Case 3: Axios error with response data
    let errorData = error?.response?.data
    
    // If data is a string, try to parse it as JSON
    if (typeof errorData === 'string') {
      try {
        errorData = JSON.parse(errorData)
      } catch (parseError) {
        console.error('Failed to parse error response:', parseError)
      }
    }
    
    // Case 4: Check for ASP.NET Core validation errors in response data
    if (errorData?.errors && typeof errorData.errors === 'object') {
      const validationMessages: string[] = []
      for (const field in errorData.errors) {
        const fieldErrors = errorData.errors[field]
        if (Array.isArray(fieldErrors)) {
          validationMessages.push(...fieldErrors)
        }
      }
      if (validationMessages.length > 0) {
        return validationMessages.join('. ')
      }
    }
    
    // Extract the error message with proper fallback chain
    return errorData?.detail
      || errorData?.title
      || errorData?.message
      || error?.message
      || defaultMessage
  }

  // Actions
  async function fetchOrganizations(): Promise<OrganizationResponse[]> {
    try {
      isLoading.value = true
      const fetchedOrgs = await organizationApi.getOrganizations()
      
      organizations.value = fetchedOrgs
      
      return fetchedOrgs
    } catch (error: any) {
      const errorMessage = extractErrorMessage(error, 'Failed to fetch organizations')
      toast.error(errorMessage)
      organizations.value = []
      return []
    } finally {
      isLoading.value = false
    }
  }

  async function fetchMyOrganizations(): Promise<OrganizationResponse[]> {
    try {
      isLoading.value = true
      const fetchedOrgs = await organizationApi.getMyOrganizations()
      
      organizations.value = fetchedOrgs
      return fetchedOrgs
    } catch (error: any) {
      const errorMessage = extractErrorMessage(error, 'Failed to fetch organizations')
      toast.error(errorMessage)
      organizations.value = []
      return []
    } finally {
      isLoading.value = false
    }
  }

  // Keep backward compatibility
  async function fetchOrganizationIds(): Promise<number[]> {
    const orgs = await fetchOrganizations()
    return orgs
      .map((org) => org.id)
      .filter((id): id is number => id !== undefined)
  }

  async function fetchOrganizationDetails(organizationId: number): Promise<OrganizationResponse | null> {
    try {
      isLoading.value = true
      const organization = await organizationApi.getOrganization(organizationId)
      currentOrganization.value = organization
      return organization
    } catch (error: any) {
      const errorMessage = extractErrorMessage(error, 'Failed to fetch organization details')
      toast.error(errorMessage)
      currentOrganization.value = null
      return null
    } finally {
      isLoading.value = false
    }
  }

  async function initializeOrganizationContext(isAdmin: boolean = true): Promise<boolean> {
    try {
      // Step 1: Fetch organizations (admin gets all, regular user gets their own)
      let orgs: OrganizationResponse[]
      if (isAdmin) {
        orgs = await fetchOrganizations()
      } else {
        orgs = await fetchMyOrganizations()
      }
      
      if (orgs.length === 0) {
        console.warn('[Organization Store] No organizations found for user')
        return false
      }
      
      // Step 2: Set the first organization as current
      const firstOrg = orgs[0]
      if (!firstOrg || firstOrg.id === undefined) {
        toast.error('Invalid organization data')
        return false
      }
      
      // Use the full org data we already have instead of fetching again
      currentOrganization.value = firstOrg
      
      return true
    } catch (error: any) {
      const errorMessage = extractErrorMessage(error, 'Failed to initialize organization context')
      toast.error(errorMessage)
      return false
    }
  }

  function clearOrganizationContext() {
    organizations.value = []
    currentOrganization.value = null
  }

  async function switchOrganization(organizationId: number): Promise<boolean> {
    // Check if the org is in our list
    const org = organizations.value.find((o) => o.id === organizationId)
    if (!org) {
      toast.error('Invalid organization ID')
      return false
    }
    
    // Fetch full details for the selected org
    const organization = await fetchOrganizationDetails(organizationId)

    return organization !== null
  }

  return {
    // State
    organizations,
    organizationIds,
    currentOrganization,
    isLoading,
    
    // Computed
    hasOrganizations,
    currentOrganizationId,
    hasMultipleOrganizations,
    
    // Actions
    fetchOrganizations,
    fetchOrganizationIds,
    fetchOrganizationDetails,
    initializeOrganizationContext,
    clearOrganizationContext,
    switchOrganization,
  }
})
