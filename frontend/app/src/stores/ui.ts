import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { toast } from 'vue-sonner'

export const useUIStore = defineStore('ui', () => {
  // Sidebar state
  const sidebarCollapsed = ref(false)
  const headerCollapsed = ref(false)
  
  // Modal states
  const showNewInvoiceModal = ref(false)
  const showNewQuoteModal = ref(false)
  const showNewClientModal = ref(false)
  const showEditClientModal = ref(false)
  
  // Dropdown states
  const showNewDropdown = ref(false)
  
  // Edit data
  const editClientData = ref<any>(null)
  
  // Notification/Toast state
  const notifications = ref<Array<{
    id: string
    type: 'success' | 'error' | 'info' | 'warning'
    message: string
    duration?: number
  }>>([])

  // Computed
  const hasNotifications = computed(() => notifications.value.length > 0)

  // Sidebar actions
  function toggleSidebar() {
    sidebarCollapsed.value = !sidebarCollapsed.value
    localStorage.setItem('sidebarCollapsed', sidebarCollapsed.value.toString())
  }

  function setSidebarCollapsed(collapsed: boolean) {
    sidebarCollapsed.value = collapsed
    localStorage.setItem('sidebarCollapsed', collapsed.toString())
  }

  function setHeaderCollapsed(collapsed: boolean) {
    headerCollapsed.value = collapsed
  }

  function loadSidebarState() {
    const saved = localStorage.getItem('sidebarCollapsed')
    if (saved !== null) {
      sidebarCollapsed.value = saved === 'true'
    }
  }

  // Modal actions - Invoice
  function openNewInvoiceModal() {
    showNewInvoiceModal.value = true
    showNewDropdown.value = false
  }

  function closeNewInvoiceModal() {
    showNewInvoiceModal.value = false
  }

  // Modal actions - Quote
  function openNewQuoteModal() {
    showNewQuoteModal.value = true
    showNewDropdown.value = false
  }

  function closeNewQuoteModal() {
    showNewQuoteModal.value = false
  }

  // Modal actions - Client
  function openNewClientModal() {
    showNewClientModal.value = true
  }

  function closeNewClientModal() {
    showNewClientModal.value = false
  }

  function openEditClientModal(clientData: any) {
    editClientData.value = clientData
    showEditClientModal.value = true
  }

  function closeEditClientModal() {
    showEditClientModal.value = false
    editClientData.value = null
  }

  // Dropdown actions
  function toggleNewDropdown() {
    showNewDropdown.value = !showNewDropdown.value
  }

  function closeNewDropdown() {
    showNewDropdown.value = false
  }

  // Notification actions
  function addNotification(
    type: 'success' | 'error' | 'info' | 'warning',
    message: string,
    duration: number = 5000
  ) {
    const id = `${Date.now()}-${Math.random()}`
    notifications.value.push({ id, type, message, duration })

    if (duration > 0) {
      setTimeout(() => {
        removeNotification(id)
      }, duration)
    }

    return id
  }

  function removeNotification(id: string) {
    const index = notifications.value.findIndex(n => n.id === id)
    if (index !== -1) {
      notifications.value.splice(index, 1)
    }
  }

  function clearNotifications() {
    notifications.value = []
  }

  // Convenience notification methods using Sonner
  function showSuccess(message: string, duration?: number) {
    toast.success(message, { duration })
    return addNotification('success', message, duration)
  }

  function showError(message: string, duration?: number) {
    toast.error(message, { duration })
    return addNotification('error', message, duration)
  }

  function showInfo(message: string, duration?: number) {
    toast.info(message, { duration })
    return addNotification('info', message, duration)
  }

  function showWarning(message: string, duration?: number) {
    toast.warning(message, { duration })
    return addNotification('warning', message, duration)
  }

  return {
    // State
    sidebarCollapsed,
    headerCollapsed,
    showNewInvoiceModal,
    showNewQuoteModal,
    showNewClientModal,
    showEditClientModal,
    showNewDropdown,
    editClientData,
    notifications,
    
    // Computed
    hasNotifications,
    
    // Actions
    toggleSidebar,
    setSidebarCollapsed,
    setHeaderCollapsed,
    loadSidebarState,
    openNewInvoiceModal,
    closeNewInvoiceModal,
    openNewQuoteModal,
    closeNewQuoteModal,
    openNewClientModal,
    closeNewClientModal,
    openEditClientModal,
    closeEditClientModal,
    toggleNewDropdown,
    closeNewDropdown,
    addNotification,
    removeNotification,
    clearNotifications,
    showSuccess,
    showError,
    showInfo,
    showWarning,
  }
})
