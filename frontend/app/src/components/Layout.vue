<script setup lang="ts">
import { ref, onMounted, onUnmounted } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { authService } from '../services/auth'
import { Button, Separator, ScrollArea } from '../components/ui/index'
import packageJson from '../../package.json'
import NewInvoiceModal from './modals/NewInvoiceModal.vue'
import ServerStatus from './ServerStatus.vue'

const router = useRouter()
const route = useRoute()
const username = ref('')
let sidebarCollapsed = ref(false)
const headerCollapsed = ref(false)
const lastScrollY = ref(0)
const showNewInvoiceModal = ref(false)
const clients = ref<any[]>([])

onMounted(async () => {
  const userInfo = await authService.getUserInfo()
  if (userInfo) {
    username.value = userInfo.username
  }

  sidebarCollapsed.value = localStorage.getItem('sidebarCollapsed') === 'true'
  
  window.addEventListener('scroll', handleScroll)
  await fetchClients()
})

onUnmounted(() => {
  window.removeEventListener('scroll', handleScroll)
})

const handleScroll = () => {
  const currentScrollY = window.scrollY
  
  if (currentScrollY > 50 && currentScrollY > lastScrollY.value) {
    headerCollapsed.value = true
  } else if (currentScrollY < lastScrollY.value) {
    headerCollapsed.value = false
  }
  
  lastScrollY.value = currentScrollY
}

const toggleSidebar = () => {
  sidebarCollapsed.value = !sidebarCollapsed.value
  localStorage.setItem('sidebarCollapsed', sidebarCollapsed.value.toString())
}

const handleLogout = () => {
  authService.logout()
  router.push('/login')
}

const fetchClients = async () => {
  try {
    const token = authService.getToken()
    const response = await fetch('http://localhost:5000/api/Client?page=1&pageSize=100', {
      headers: {
        'Authorization': `Bearer ${token}`
      }
    })

    if (response.ok) {
      const data = await response.json()
      clients.value = data.data
    }
  } catch (error) {
    console.error('Failed to fetch clients:', error)
  }
}

const openNewInvoiceModal = () => {
  showNewInvoiceModal.value = true
}

const closeNewInvoiceModal = () => {
  showNewInvoiceModal.value = false
}

const saveNewInvoice = async (data: { clientId: number, items: any[], templateId?: string }) => {
  try {
    const token = authService.getToken()
    const selectedClient = clients.value.find(c => c.id === data.clientId)
    
    if (!selectedClient) {
      alert('Selected client not found')
      return
    }

    const invoice = {
      clientName: selectedClient.name,
      clientSurname: selectedClient.surname,
      clientAddress: selectedClient.address || '',
      clientCellphone: selectedClient.cellphone,
      templateId: data.templateId,
      items: data.items.map(item => ({
        description: item.description,
        amount: item.amount,
        pricePerUnit: item.pricePerUnit
      }))
    }

    const response = await fetch('http://localhost:5000/api/Invoice', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${token}`
      },
      body: JSON.stringify(invoice)
    })

    if (response.ok) {
      closeNewInvoiceModal()
      // Emit event or refresh if on invoices page
      if (route.path === '/invoices') {
        window.location.reload()
      }
    } else {
      const error = await response.json()
      alert(error.message || 'Failed to create invoice')
    }
  } catch (error) {
    console.error('Failed to save invoice:', error)
    alert('Failed to save invoice')
  }
}
</script>

<template>
  <div class="min-h-screen bg-gray-50">
    <!-- Header -->
    <header
      class="fixed top-0 left-0 right-0 z-50 bg-white border-b border-gray-200 transition-all duration-300 ease-in-out"
      :class="headerCollapsed ? '-translate-y-full' : 'translate-y-0'"
    >
      <div class="flex items-center justify-between px-6 h-16">
        <div class="flex items-center gap-4">
          <Button
            variant="ghost"
            size="sm"
            @click="toggleSidebar"
            class="lg:hidden"
          >
            <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6h16M4 12h16M4 18h16"/>
            </svg>
          </Button>
          <h1 class="text-xl font-semibold text-gray-900">Application Dashboard</h1>
          <Button
            @click="openNewInvoiceModal"
            variant="default"
          >
            <svg class="w-5 h-5 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4"/>
            </svg>
            New Invoice
          </Button>
        </div>
        
        <div class="flex items-center gap-4">
          <div class="hidden md:flex items-center gap-2 text-sm text-gray-600">
            <ServerStatus />
            <span>Welcome,</span>
            <span class="font-medium text-gray-900">{{ username || 'User' }}</span>
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

    <!-- Sidebar -->
    <aside
      class="fixed left-0 top-16 bottom-0 bg-white border-r border-gray-200 transition-all duration-300 ease-in-out z-40"
      :class="sidebarCollapsed ? 'w-16' : 'w-64'"
    >
      <div class="flex flex-col h-full">
        <div class="hidden lg:flex items-center justify-between px-4 py-4">
          <h2
            v-if="!sidebarCollapsed"
            class="text-sm font-semibold text-gray-700 transition-opacity duration-200"
          >
            Navigation
          </h2>
          <Button
            variant="ghost"
            size="sm"
            @click="toggleSidebar"
            class=" transition-colors duration-50 active:bg-transparent"
          >
            <svg
              class="w-5 h-5 transition-transform duration-300"
              :class="sidebarCollapsed ? 'rotate-180' : ''"
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
                sidebarCollapsed ? 'justify-left' : '',
                route.path === '/dashboard' ? 'text-gray-900 bg-gray-100' : 'text-gray-700'
              ]"
            >
              <svg class="w-5 h-5 flex-shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 12l2-2m0 0l7-7 7 7M5 10v10a1 1 0 001 1h3m10-11l2 2m-2-2v10a1 1 0 01-1 1h-3m-6 0a1 1 0 001-1v-4a1 1 0 011-1h2a1 1 0 011 1v4a1 1 0 001 1m-6 0h6"/>
              </svg>
              <span v-if="!sidebarCollapsed" class="transition-opacity duration-200">Dashboard</span>
            </router-link>

            <router-link
              to="/clients"
              class="flex items-center gap-3 px-3 py-2 text-sm font-medium rounded-lg hover:bg-gray-100 transition-colors"
              :class="[
                sidebarCollapsed ? 'justify-left' : '',
                route.path === '/clients' ? 'text-gray-900 bg-gray-100' : 'text-gray-700'
              ]"
            >
              <svg class="w-5 h-5 flex-shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0zm6 3a2 2 0 11-4 0 2 2 0 014 0zM7 10a2 2 0 11-4 0 2 2 0 014 0z"/>
              </svg>
              <span v-if="!sidebarCollapsed" class="transition-opacity duration-200">Clients</span>
            </router-link>

            <router-link
              to="/invoices"
              class="flex items-center gap-3 px-3 py-2 text-sm font-medium rounded-lg hover:bg-gray-100 transition-colors"
              :class="[
                sidebarCollapsed ? 'justify-left' : '',
                route.path === '/invoices' ? 'text-gray-900 bg-gray-100' : 'text-gray-700'
              ]"
            >
              <svg class="w-5 h-5 flex-shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"/>
              </svg>
              <span v-if="!sidebarCollapsed" class="transition-opacity duration-200">Invoices</span>
            </router-link>
          </nav>
        </ScrollArea>

        <div class="p-4 border-t border-gray-200">
          <div v-if="!sidebarCollapsed" class="text-xs text-gray-500 text-center">
            v{{ packageJson.version }}
          </div>
        </div>
      </div>
    </aside>

    <!-- Main Content -->
    <main
      class="transition-all duration-300 ease-in-out pt-16 pl-16"
      :class="sidebarCollapsed ? 'lg:pl-16' : 'lg:pl-64'"
    >
      <slot />
    </main>

    <!-- New Invoice Modal -->
    <NewInvoiceModal
      :show="showNewInvoiceModal"
      :clients="clients"
      @close="closeNewInvoiceModal"
      @save="saveNewInvoice"
    />
  </div>
</template>
