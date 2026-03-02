import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { organizationApi } from '../services/api'
import type { OrganizationResponse } from '../api/generated/api-client'
import { toast } from 'vue-sonner'
import { extractErrorMessage } from '../lib/error-utils'

const SELECTED_ORG_KEY = 'selected_organization_id'

export const useOrganizationStore = defineStore('organization', () => {
  const organizations = ref<OrganizationResponse[]>([])
  const currentOrganization = ref<OrganizationResponse | null>(null)
  const isLoading = ref(false)

  const organizationIds = computed(() =>
    organizations.value
      .map((org) => org.id)
      .filter((id): id is number => id !== undefined)
  )
  const hasOrganizations = computed(() => organizations.value.length > 0)
  const currentOrganizationId = computed(() => currentOrganization.value?.id ?? null)
  const hasMultipleOrganizations = computed(() => organizations.value.length > 1)

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
        return false
      }
      
      // Step 2: Check localStorage for a previously selected org
      const savedOrgId = localStorage.getItem(SELECTED_ORG_KEY)
      let selectedOrg: OrganizationResponse | undefined
      
      if (savedOrgId) {
        const parsedId = parseInt(savedOrgId, 10)
        selectedOrg = orgs.find((o) => o.id === parsedId)
      }
      
      // Fall back to the first organization if saved org not found
      if (!selectedOrg) {
        selectedOrg = orgs[0]
      }
      
      if (!selectedOrg || selectedOrg.id === undefined) {
        toast.error('Invalid organization data')
        return false
      }
      
      // Use the full org data we already have instead of fetching again
      currentOrganization.value = selectedOrg
      localStorage.setItem(SELECTED_ORG_KEY, String(selectedOrg.id))
      
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
    localStorage.removeItem(SELECTED_ORG_KEY)
  }

  async function switchOrganization(organizationId: number): Promise<boolean> {
    // Check if the org is in our list
    const org = organizations.value.find((o) => o.id === organizationId)
    if (!org) {
      toast.error('Invalid organization ID')
      return false
    }
    
    // Persist the selection to localStorage
    localStorage.setItem(SELECTED_ORG_KEY, String(organizationId))
    
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
