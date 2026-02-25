<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted, watch } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useAuthStore } from '../stores/auth'
import { useUIStore } from '../stores/ui'
import { useOrganizationStore } from '../stores/organization'
import { clientApi, invoiceApi, quoteApi } from '../services/api'
import { Button, Separator, ScrollArea, DropdownMenu, DropdownMenuTrigger, DropdownMenuContent, DropdownMenuItem } from '../components/ui/index'
import packageJson from '../../package.json'
import NewInvoiceModal from './modals/NewInvoiceModal.vue'
import NewQuoteModal from './modals/NewQuoteModal.vue'
import OrganizationSwitcherModal from './modals/OrganizationSwitcherModal.vue'
import ServerStatus from './ServerStatus.vue'

const router = useRouter()
const route = useRoute()

const authStore = useAuthStore()
const uiStore = useUIStore()
const organizationStore = useOrganizationStore()

const headerTitle = computed(() => {
  const orgName = organizationStore.currentOrganization?.name
  return orgName ? `${orgName}` : 'Invoice Tracker'
})

const showOrgSwitcher = ref(false)
const lastScrollY = ref(0)
const clients = ref<any[]>([])

// Ensure organization context is initialized (e.g. after page refresh where Pinia state is lost)
const ensureOrganizationContext = async (): Promise<number | null> => {
  if (organizationStore.currentOrganizationId) {
    return organizationStore.currentOrganizationId
  }
  // Lazy-init org context from localStorage / API
  const success = await organizationStore.initializeOrganizationContext(authStore.isAdmin)
  return success ? organizationStore.currentOrganizationId : null
}

onMounted(async () => {
  uiStore.loadSidebarState()
  
  if (!authStore.username) {
    authStore.loadTokenFromStorage()
  }
  
  window.addEventListener('scroll', handleScroll)
  
  // Ensure org context is available before fetching data
  await ensureOrganizationContext()
  await fetchClients()
})

onUnmounted(() => {
  window.removeEventListener('scroll', handleScroll)
})

const handleScroll = () => {
  const currentScrollY = window.scrollY
  
  if (currentScrollY > 50 && currentScrollY > lastScrollY.value) {
    uiStore.setHeaderCollapsed(true)
  } else if (currentScrollY < lastScrollY.value) {
    uiStore.setHeaderCollapsed(false)
  }
  
  lastScrollY.value = currentScrollY
}

const handleLogout = async () => {
  await authStore.logout()
}

const fetchClients = async () => {
  try {
    const orgId = organizationStore.currentOrganizationId
    if (!orgId) {
      console.warn('No organization selected, skipping client fetch')
      return
    }
    const response = await clientApi.getClients(orgId, 1, 100)
    clients.value = response.data || []
  } catch (error) {
    console.error('Failed to fetch clients:', error)
  }
}

watch(() => uiStore.showNewInvoiceModal, (newVal) => {
  if (newVal) fetchClients()
})
watch(() => uiStore.showNewQuoteModal, (newVal) => {
  if (newVal) fetchClients()
})

const saveNewInvoice = async (data: { clientId: number, items: any[], templateId?: number, payByDays: number, vatInclusive: boolean }) => {
  try {
    const invoice = {
      clientId: data.clientId,
      templateId: data.templateId,
      payByDays: data.payByDays,
      vatInclusive: data.vatInclusive,
      items: data.items.map(item => ({
        description: item.description,
        quantity: item.amount,
        unitPrice: item.pricePerUnit
      }))
    }

    await invoiceApi.createInvoice(invoice, organizationStore.currentOrganizationId ?? undefined)
    
    uiStore.closeNewInvoiceModal()
    uiStore.showSuccess('Invoice created successfully')
    
    if (route.path !== '/invoices') {
      router.push('/invoices')
    }
  } catch (error: any) {
    console.error('Failed to save invoice:', error)
    uiStore.showError(error.response?.data?.message || 'Failed to save invoice')
  }
}

const saveNewQuote = async (data: { clientId: number, items: any[], templateId?: number, vatInclusive: boolean }) => {
  try {
    const quote = {
      clientId: data.clientId,
      templateId: data.templateId,
      vatInclusive: data.vatInclusive,
      items: data.items.map(item => ({
        description: item.description,
        quantity: item.amount,
        unitPrice: item.pricePerUnit
      }))
    }

    await quoteApi.createQuote(quote, organizationStore.currentOrganizationId ?? undefined)
    
    uiStore.closeNewQuoteModal()
    uiStore.showSuccess('Quote created successfully')
    
    if (route.path !== '/quotes') {
      router.push('/quotes')
    }
  } catch (error: any) {
    console.error('Failed to save quote:', error)
    uiStore.showError(error.response?.data?.message || 'Failed to save quote')
  }
}
</script>

<template>
  <div class="min-h-screen bg-gray-50">
    <!-- Header -->
    <header
      class="fixed top-0 left-0 right-0 z-50 bg-white border-b border-gray-200 transition-all duration-300 ease-in-out"
      :class="uiStore.headerCollapsed ? '-translate-y-full' : 'translate-y-0'"
    >
      <div class="flex items-center justify-between px-6 h-16">
        <div class="flex items-center gap-4">
          <Button
            variant="ghost"
            size="sm"
            @click="uiStore.toggleSidebar"
            class="lg:hidden"
          >
            <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6h16M4 12h16M4 18h16"/>
            </svg>
          </Button>
          <div class="flex items-center gap-1">
            <h1 class="text-xl font-semibold text-gray-900">{{ headerTitle }}</h1>
            <!-- Organization Switch Icon -->
            <button
              v-if="organizationStore.hasMultipleOrganizations"
              @click="showOrgSwitcher = true"
              class="p-1 rounded-md text-gray-400 hover:text-gray-600 hover:bg-gray-100 transition-colors"
              title="Switch organization"
            >
              <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 7h12m0 0l-4-4m4 4l-4 4m0 6H4m0 0l4 4m-4-4l4-4"/>
              </svg>
            </button>
          </div>
          <!-- New Item Dropdown -->
          <DropdownMenu>
            <DropdownMenuTrigger as-child>
              <Button variant="default">
                <svg class="w-5 h-5 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4"/>
                </svg>
                New
                <svg class="w-4 h-4 ml-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7"/>
                </svg>
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="start">
              <DropdownMenuItem @click="uiStore.openNewQuoteModal">
                <svg class="w-4 h-4 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"/>
                </svg>
                New Quote
              </DropdownMenuItem>
              <DropdownMenuItem @click="uiStore.openNewInvoiceModal">
                <svg class="w-4 h-4 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"/>
                </svg>
                New Invoice
              </DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>
        </div>
        
        <div class="flex items-center gap-4">
          <div class="hidden md:flex items-center gap-2 text-sm text-gray-600">
            <ServerStatus />
            <span>Welcome,</span>
            <span class="font-medium text-gray-900">{{ authStore.firstName || authStore.username || 'User' }}</span>
          </div>
          <Button
            variant="ghost"
            size="sm"
            @click="handleLogout"
            class="text-red-600 hover:text-red-700 hover:bg-red-50"
          >
            <svg class="w-5 h-5 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 16l4-4m0 0l-4-4m4 4H7m6 4v1a3 3 0 01-3 3H6a3 3 0 01-3-3V7a3 3 0 013-3h4a3 3 0 013 3v1"/>
            </svg>
            Sign Out
          </Button>
        </div>
      </div>
    </header>

    <!-- Sidebar + Content wrapper -->
    <div class="flex pt-16 min-h-screen">
    <!-- Sidebar -->
    <aside
      class="shrink-0 bg-white border-r border-gray-200 transition-all duration-300 ease-in-out sticky top-16 h-[calc(100vh-4rem)]"
      :class="uiStore.sidebarCollapsed ? 'w-16' : 'w-64'"
    >
      <div class="flex flex-col h-full">
        <div class="hidden lg:flex items-center justify-between px-4 py-4">
          <h2
            v-if="!uiStore.sidebarCollapsed"
            class="text-sm font-semibold text-gray-700 transition-opacity duration-200"
          >
            Navigation
          </h2>
          <Button
            variant="ghost"
            size="sm"
            @click="uiStore.toggleSidebar"
            class=" transition-colors duration-50 active:bg-transparent"
          >
            <svg
              class="w-5 h-5 transition-transform duration-300"
              :class="uiStore.sidebarCollapsed ? 'rotate-180' : ''"
              fill="none"
              stroke="currentColor"
              viewBox="0 0 24 24"
            >
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7"/>
            </svg>
          </Button>
        </div>

        <Separator />

        <ScrollArea class="flex-1 px-3 py-4">
          <nav class="space-y-1">
            <router-link
              to="/dashboard"
              class="flex items-center gap-3 px-3 py-2 text-sm font-medium rounded-lg hover:bg-gray-100 transition-colors"
              :class="[
                uiStore.sidebarCollapsed ? 'justify-left' : '',
                route.path === '/dashboard' ? 'text-gray-900 bg-gray-100' : 'text-gray-700'
              ]"
            >
              <svg class="w-5 h-5 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 12l2-2m0 0l7-7 7 7M5 10v10a1 1 0 001 1h3m10-11l2 2m-2-2v10a1 1 0 01-1 1h-3m-6 0a1 1 0 001-1v-4a1 1 0 011-1h2a1 1 0 011 1v4a1 1 0 001 1m-6 0h6"/>
              </svg>
              <span v-if="!uiStore.sidebarCollapsed" class="transition-opacity duration-200">Dashboard</span>
            </router-link>

            <router-link
              to="/clients"
              class="flex items-center gap-3 px-3 py-2 text-sm font-medium rounded-lg hover:bg-gray-100 transition-colors"
              :class="[
                uiStore.sidebarCollapsed ? 'justify-left' : '',
                route.path === '/clients' ? 'text-gray-900 bg-gray-100' : 'text-gray-700'
              ]"
            >
              <svg class="w-5 h-5 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0zm6 3a2 2 0 11-4 0 2 2 0 014 0zM7 10a2 2 0 11-4 0 2 2 0 014 0z"/>
              </svg>
              <span v-if="!uiStore.sidebarCollapsed" class="transition-opacity duration-200">Clients</span>
            </router-link>

            <router-link
              to="/workflows"
              class="flex items-center gap-3 px-3 py-2 text-sm font-medium rounded-lg hover:bg-gray-100 transition-colors"
              :class="[
                uiStore.sidebarCollapsed ? 'justify-left' : '',
                route.path.startsWith('/workflows') ? 'text-gray-900 bg-gray-100' : 'text-gray-700'
              ]"
            >
              <svg class="w-5 h-5 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2m-6 9l2 2 4-4"/>
              </svg>
              <span v-if="!uiStore.sidebarCollapsed" class="transition-opacity duration-200">Workflows</span>
            </router-link>

            <Separator class="my-3" />

            <router-link
              to="/quotes"
              class="flex items-center gap-3 px-3 py-2 text-sm font-medium rounded-lg hover:bg-gray-100 transition-colors"
              :class="[
                uiStore.sidebarCollapsed ? 'justify-left' : '',
                route.path === '/quotes' ? 'text-gray-900 bg-gray-100' : 'text-gray-700'
              ]"
            >
              <svg class="w-5 h-5 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"/>
              </svg>
              <span v-if="!uiStore.sidebarCollapsed" class="transition-opacity duration-200">Quotes</span>
            </router-link>

            <router-link
              to="/invoices"
              class="flex items-center gap-3 px-3 py-2 text-sm font-medium rounded-lg hover:bg-gray-100 transition-colors"
              :class="[
                uiStore.sidebarCollapsed ? 'justify-left' : '',
                route.path === '/invoices' ? 'text-gray-900 bg-gray-100' : 'text-gray-700'
              ]"
            >
              <svg class="w-5 h-5 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"/>
              </svg>
              <span v-if="!uiStore.sidebarCollapsed" class="transition-opacity duration-200">Invoices</span>
            </router-link>

            <router-link
              to="/templates"
              class="flex items-center gap-3 px-3 py-2 text-sm font-medium rounded-lg hover:bg-gray-100 transition-colors"
              :class="[
                uiStore.sidebarCollapsed ? 'justify-left' : '',
                route.path === '/templates' ? 'text-gray-900 bg-gray-100' : 'text-gray-700'
              ]"
            >
              <svg class="w-5 h-5 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 5a1 1 0 011-1h14a1 1 0 011 1v2a1 1 0 01-1 1H5a1 1 0 01-1-1V5zM4 13a1 1 0 011-1h6a1 1 0 011 1v6a1 1 0 01-1 1H5a1 1 0 01-1-1v-6zM16 13a1 1 0 011-1h2a1 1 0 011 1v6a1 1 0 01-1 1h-2a1 1 0 01-1-1v-6z"/>
              </svg>
              <span v-if="!uiStore.sidebarCollapsed" class="transition-opacity duration-200">Templates</span>
            </router-link>
          </nav>
        </ScrollArea>

        <div class="p-4 border-t border-gray-200">
          <div v-if="!uiStore.sidebarCollapsed" class="text-xs text-gray-500 text-center">
            v{{ packageJson.version }}
          </div>
        </div>
      </div>
    </aside>

    <!-- Main Content -->
    <main
      class="flex-1 min-w-0 transition-all duration-300 ease-in-out"
    >
      <slot />
    </main>
    </div>

    <!-- New Invoice Modal -->
    <NewInvoiceModal
      :show="uiStore.showNewInvoiceModal"
      :clients="clients"
      @close="uiStore.closeNewInvoiceModal"
      @save="saveNewInvoice"
    />

    <!-- New Quote Modal -->
    <NewQuoteModal
      :show="uiStore.showNewQuoteModal"
      :clients="clients"
      @close="uiStore.closeNewQuoteModal"
      @save="saveNewQuote"
    />

    <!-- Organization Switcher Dialog -->
    <OrganizationSwitcherModal
      :show="showOrgSwitcher"
      @close="showOrgSwitcher = false"
    />
  </div>
</template>
