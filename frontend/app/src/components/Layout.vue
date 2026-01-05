<script setup lang="ts">
import { ref, onMounted, onUnmounted, computed } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { authService } from '../services/auth'
import { Button, Separator, ScrollArea } from '../components/ui/index'
import packageJson from '../../package.json'

const router = useRouter()
const route = useRoute()
const username = ref('')
const sidebarCollapsed = ref(false)
const headerCollapsed = ref(false)
const lastScrollY = ref(0)
const showNewInvoiceModal = ref(false)
const clients = ref<any[]>([])
const selectedClientId = ref<number | null>(null)
const invoiceItems = ref<any[]>([{ description: '', amount: 1, pricePerUnit: 0 }])
const formErrors = ref<Record<string, string>>({})

onMounted(async () => {
  const userInfo = await authService.getUserInfo()
  if (userInfo) {
    username.value = userInfo.username
  }
  
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
  selectedClientId.value = null
  invoiceItems.value = [{ description: '', amount: 1, pricePerUnit: 0 }]
  formErrors.value = {}
  showNewInvoiceModal.value = true
}

const closeNewInvoiceModal = () => {
  showNewInvoiceModal.value = false
}

const addInvoiceItem = () => {
  invoiceItems.value.push({ description: '', amount: 1, pricePerUnit: 0 })
}

const removeInvoiceItem = (index: number) => {
  if (invoiceItems.value.length > 1) {
    invoiceItems.value.splice(index, 1)
  }
}

const validateInvoiceForm = () => {
  formErrors.value = {}
  
  if (!selectedClientId.value) {
    formErrors.value.client = 'Please select a client'
  }
  
  invoiceItems.value.forEach((item, index) => {
    if (!item.description.trim()) {
      formErrors.value[`item_${index}_description`] = 'Description is required'
    }
    if (item.amount < 1) {
      formErrors.value[`item_${index}_amount`] = 'Amount must be at least 1'
    }
    if (item.pricePerUnit <= 0) {
      formErrors.value[`item_${index}_price`] = 'Price must be greater than 0'
    }
  })
  
  return Object.keys(formErrors.value).length === 0
}

const saveNewInvoice = async () => {
  if (!validateInvoiceForm()) {
    return
  }

  try {
    const token = authService.getToken()
    const selectedClient = clients.value.find(c => c.id === selectedClientId.value)
    
    if (!selectedClient) {
      alert('Selected client not found')
      return
    }

    const invoice = {
      clientName: selectedClient.name,
      clientSurname: selectedClient.surname,
      clientAddress: selectedClient.address || '',
      clientCellphone: selectedClient.cellphone,
      items: invoiceItems.value.map(item => ({
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

const totalInvoiceAmount = computed(() => {
  return invoiceItems.value.reduce((sum, item) => sum + (item.amount * item.pricePerUnit), 0)
})

const filteredClients = computed(() => {
  return clients.value
})
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
        </div>
        
        <div class="flex items-center gap-4">
          <Button
            @click="openNewInvoiceModal"
            class="bg-blue-600 hover:bg-blue-700 text-white hidden sm:flex"
          >
            <svg class="w-5 h-5 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4"/>
            </svg>
            New Invoice
          </Button>
          <div class="hidden md:flex items-center gap-2 text-sm text-gray-600">
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
        <div class="flex items-center justify-between px-4 py-4">
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
            class="hidden lg:flex transition-colors duration-50 active:bg-transparent"
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
                sidebarCollapsed ? 'justify-center' : '',
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
                sidebarCollapsed ? 'justify-center' : '',
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
                sidebarCollapsed ? 'justify-center' : '',
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
      class="transition-all duration-300 ease-in-out pt-16"
      :class="sidebarCollapsed ? 'lg:pl-16' : 'lg:pl-64'"
    >
      <slot />
    </main>

    <!-- New Invoice Modal -->
    <div
      v-if="showNewInvoiceModal"
      class="fixed inset-0 z-[60] overflow-y-auto"
    >
      <div class="flex items-center justify-center min-h-screen px-4 pt-4 pb-20 text-center sm:block sm:p-0">
        <div class="fixed inset-0 transition-opacity bg-gray-500/50" @click="closeNewInvoiceModal"></div>
        
        <!-- Spacer for centering -->
        <span class="hidden sm:inline-block sm:align-middle sm:h-screen" aria-hidden="true">&#8203;</span>

        <div class="relative inline-block align-bottom bg-white rounded-lg text-left overflow-hidden shadow-xl transform transition-all sm:my-8 sm:align-middle sm:max-w-2xl sm:w-full">
          <div class="bg-white px-4 pt-5 pb-4 sm:p-6 sm:pb-4 max-h-[80vh] overflow-y-auto">
            <div class="flex items-center justify-between mb-4">
              <h3 class="text-lg font-medium text-gray-900">New Invoice</h3>
              <button @click="closeNewInvoiceModal" class="text-gray-400 hover:text-gray-500">
                <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/>
                </svg>
              </button>
            </div>

            <div class="space-y-4">
              <!-- Client Selection -->
              <div>
                <label class="block text-sm font-medium text-gray-700 mb-1">Client *</label>
                <select
                  v-model="selectedClientId"
                  class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
                  :class="formErrors.client ? 'border-red-500' : ''"
                >
                  <option :value="null">Select a client...</option>
                  <option v-for="client in filteredClients" :key="client.id" :value="client.id">
                    {{ client.name }} {{ client.surname }} ({{ client.email }})
                  </option>
                </select>
                <p v-if="formErrors.client" class="mt-1 text-sm text-red-600">{{ formErrors.client }}</p>
              </div>

              <!-- Invoice Items -->
              <div>
                <div class="flex items-center justify-between mb-2">
                  <label class="block text-sm font-medium text-gray-700">Invoice Items *</label>
                  <Button @click="addInvoiceItem" size="sm" variant="outline">
                    <svg class="w-4 h-4 mr-1" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4"/>
                    </svg>
                    Add Item
                  </Button>
                </div>

                <div v-for="(item, index) in invoiceItems" :key="index" class="mb-3 p-3 border border-gray-200 rounded-lg">
                  <div class="flex items-start justify-between mb-2">
                    <span class="text-sm font-medium text-gray-700">Item {{ index + 1 }}</span>
                    <button
                      v-if="invoiceItems.length > 1"
                      @click="removeInvoiceItem(index)"
                      class="text-red-600 hover:text-red-700"
                    >
                      <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"/>
                      </svg>
                    </button>
                  </div>

                  <div class="space-y-2">
                    <div>
                      <input
                        v-model="item.description"
                        type="text"
                        placeholder="Description"
                        class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
                        :class="formErrors[`item_${index}_description`] ? 'border-red-500' : ''"
                      />
                      <p v-if="formErrors[`item_${index}_description`]" class="mt-1 text-sm text-red-600">
                        {{ formErrors[`item_${index}_description`] }}
                      </p>
                    </div>

                    <div class="grid grid-cols-2 gap-2">
                      <div>
                        <input
                          v-model.number="item.amount"
                          type="number"
                          min="1"
                          placeholder="Amount"
                          class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
                          :class="formErrors[`item_${index}_amount`] ? 'border-red-500' : ''"
                        />
                        <p v-if="formErrors[`item_${index}_amount`]" class="mt-1 text-sm text-red-600">
                          {{ formErrors[`item_${index}_amount`] }}
                        </p>
                      </div>

                      <div>
                        <input
                          v-model.number="item.pricePerUnit"
                          type="number"
                          min="0"
                          step="0.01"
                          placeholder="Price per unit"
                          class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
                          :class="formErrors[`item_${index}_price`] ? 'border-red-500' : ''"
                        />
                        <p v-if="formErrors[`item_${index}_price`]" class="mt-1 text-sm text-red-600">
                          {{ formErrors[`item_${index}_price`] }}
                        </p>
                      </div>
                    </div>

                    <div class="text-sm text-gray-600">
                      Subtotal: R {{ (item.amount * item.pricePerUnit).toFixed(2) }}
                    </div>
                  </div>
                </div>
              </div>

              <!-- Total -->
              <div class="pt-3 border-t border-gray-200">
                <div class="flex justify-between items-center">
                  <span class="text-lg font-semibold text-gray-900">Total:</span>
                  <span class="text-lg font-bold text-blue-600">R {{ totalInvoiceAmount.toFixed(2) }}</span>
                </div>
              </div>
            </div>
          </div>

          <div class="bg-gray-50 px-4 py-3 sm:px-6 sm:flex sm:flex-row-reverse gap-2">
            <Button @click="saveNewInvoice" class="bg-blue-600 hover:bg-blue-700 text-white">
              Create Invoice
            </Button>
            <Button @click="closeNewInvoiceModal" variant="outline">
              Cancel
            </Button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
