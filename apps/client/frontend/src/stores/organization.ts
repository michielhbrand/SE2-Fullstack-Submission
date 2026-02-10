import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { organizationApi } from '../services/api'
import type { OrganizationResponse } from '../api/generated/api-client'
import { toast } from 'vue-sonner'

export const useOrganizationStore = defineStore('organization', () => {
  const organizationIds = ref<number[]>([])
  const currentOrganization = ref<OrganizationResponse | null>(null)
  const isLoading = ref(false)

  // Computed
  const hasOrganizations = computed(() => organizationIds.value.length > 0)
  const currentOrganizationId = computed(() => currentOrganization.value?.id || null)

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
  async function fetchOrganizationIds(): Promise<number[]> {
    try {
      isLoading.value = true
      console.log('[Organization Store] Fetching organization IDs...')
      const organizations = await organizationApi.getOrganizations()
      console.log('[Organization Store] Received organizations:', organizations)
      
      // Extract organization IDs from the response
      organizationIds.value = organizations
        .map((org: OrganizationResponse) => org.id)
        .filter((id: number | undefined): id is number => id !== undefined)
      
      console.log('[Organization Store] Extracted organization IDs:', organizationIds.value)
      return organizationIds.value
    } catch (error: any) {
      console.error('[Organization Store] Error fetching organization IDs:', error)
      const errorMessage = extractErrorMessage(error, 'Failed to fetch organization IDs')
      toast.error(errorMessage)
      organizationIds.value = []
      return []
    } finally {
      isLoading.value = false
    }
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

  async function initializeOrganizationContext(): Promise<boolean> {
    try {
      // Step 1: Fetch all organization IDs
      const ids = await fetchOrganizationIds()
      
      if (ids.length === 0) {
        toast.error('No organizations found for this admin user')
        return false
      }
      
      // Step 2: Fetch details for the first organization
      const firstOrgId = ids[0]
      if (firstOrgId === undefined) {
        toast.error('Invalid organization ID')
        return false
      }
      
      const organization = await fetchOrganizationDetails(firstOrgId)
      
      if (!organization) {
        toast.error('Failed to load organization details')
        return false
      }
      
      return true
    } catch (error: any) {
      const errorMessage = extractErrorMessage(error, 'Failed to initialize organization context')
      toast.error(errorMessage)
      return false
    }
  }

  function clearOrganizationContext() {
    organizationIds.value = []
    currentOrganization.value = null
  }

  async function switchOrganization(organizationId: number): Promise<boolean> {
    if (!organizationIds.value.includes(organizationId)) {
      toast.error('Invalid organization ID')
      return false
    }
    
    const organization = await fetchOrganizationDetails(organizationId)
    return organization !== null
  }

  return {
    // State
    organizationIds,
    currentOrganization,
    isLoading,
    
    // Computed
    hasOrganizations,
    currentOrganizationId,
    
    // Actions
    fetchOrganizationIds,
    fetchOrganizationDetails,
    initializeOrganizationContext,
    clearOrganizationContext,
    switchOrganization,
  }
})
