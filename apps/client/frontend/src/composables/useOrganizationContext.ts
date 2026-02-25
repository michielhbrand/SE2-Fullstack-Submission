import { useOrganizationStore } from '@/stores/organization'
import { useAuthStore } from '@/stores/auth'

export function useOrganizationContext() {
  const organizationStore = useOrganizationStore()
  const authStore = useAuthStore()

  const ensureOrganizationContext = async (): Promise<number | null> => {
    if (organizationStore.currentOrganizationId) {
      return organizationStore.currentOrganizationId
    }
    const success = await organizationStore.initializeOrganizationContext(authStore.isAdmin)
    return success ? organizationStore.currentOrganizationId : null
  }

  return { ensureOrganizationContext }
}
