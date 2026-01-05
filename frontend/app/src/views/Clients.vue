<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { authService } from '../services/auth'
import { Button } from '../components/ui/index'
import Layout from '../components/Layout.vue'

const loading = ref(true)
const showNewClientModal = ref(false)
const clients = ref<any[]>([])
const currentPage = ref(1)
const pageSize = ref(10)
const totalPages = ref(0)
const totalCount = ref(0)
const searchQuery = ref('')

// New client form
const newClient = ref({
  name: '',
  surname: '',
  email: '',
  cellphone: '',
  address: '',
  company: ''
})

const formErrors = ref<Record<string, string>>({})

onMounted(async () => {
  loading.value = false
  await fetchClients()
})

const fetchClients = async () => {
  try {
    const token = authService.getToken()
    const params = new URLSearchParams({
      page: currentPage.value.toString(),
      pageSize: pageSize.value.toString()
    })
    
    if (searchQuery.value) {
      params.append('search', searchQuery.value)
    }

    const response = await fetch(`http://localhost:5000/api/Client?${params}`, {
      headers: {
        'Authorization': `Bearer ${token}`
      }
    })

    if (response.ok) {
      const data = await response.json()
      clients.value = data.data
      totalPages.value = data.pagination.totalPages
      totalCount.value = data.pagination.totalCount
    }
  } catch (error) {
    console.error('Failed to fetch clients:', error)
  }
}

const openNewClientModal = () => {
  newClient.value = {
    name: '',
    surname: '',
    email: '',
    cellphone: '',
    address: '',
    company: ''
  }
  formErrors.value = {}
  showNewClientModal.value = true
}

const closeNewClientModal = () => {
  showNewClientModal.value = false
}

const validateForm = () => {
  formErrors.value = {}
  
  if (!newClient.value.name.trim()) {
    formErrors.value.name = 'Name is required'
  }
  
  if (!newClient.value.surname.trim()) {
    formErrors.value.surname = 'Surname is required'
  }
  
  if (!newClient.value.email.trim()) {
    formErrors.value.email = 'Email is required'
  } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(newClient.value.email)) {
    formErrors.value.email = 'Invalid email format'
  }
  
  if (!newClient.value.cellphone.trim()) {
    formErrors.value.cellphone = 'Cellphone is required'
  }
  
  return Object.keys(formErrors.value).length === 0
}

const saveNewClient = async () => {
  if (!validateForm()) {
    return
  }

  try {
    const token = authService.getToken()
    const response = await fetch('http://localhost:5000/api/Client', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${token}`
      },
      body: JSON.stringify(newClient.value)
    })

    if (response.ok) {
      closeNewClientModal()
      await fetchClients()
    } else {
      const error = await response.json()
      alert(error.message || 'Failed to create client')
    }
  } catch (error) {
    console.error('Failed to save client:', error)
    alert('Failed to save client')
  }
}

const handleSearch = async () => {
  currentPage.value = 1
  await fetchClients()
}

const goToPage = async (page: number) => {
  currentPage.value = page
  await fetchClients()
}

const paginationPages = computed(() => {
  const pages = []
  const maxVisible = 5
  let start = Math.max(1, currentPage.value - Math.floor(maxVisible / 2))
  let end = Math.min(totalPages.value, start + maxVisible - 1)
  
  if (end - start < maxVisible - 1) {
    start = Math.max(1, end - maxVisible + 1)
  }
  
  for (let i = start; i <= end; i++) {
    pages.push(i)
  }
  
  return pages
})
</script>

<template>
  <Layout>
    <div class="p-6 lg:p-8">
      <div class="max-w-7xl mx-auto">
        <!-- Header -->
        <div class="mb-8 flex items-center justify-between">
          <div>
            <h2 class="text-3xl font-bold text-gray-900">Clients</h2>
            <p class="mt-2 text-gray-600">Manage your client database</p>
          </div>
          <Button @click="openNewClientModal" variant="outline">
            <svg class="w-5 h-5 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4"/>
            </svg>
            New Client
          </Button>
        </div>

        <!-- Search Bar -->
        <div class="mb-6">
          <div class="flex gap-2">
            <input
              v-model="searchQuery"
              @keyup.enter="handleSearch"
              type="text"
              placeholder="Search clients by name, email, or company..."
              class="flex-1 px-4 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
            <Button @click="handleSearch" variant="outline" class="text-gray-600">
              <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z"/>
              </svg>
            </Button>
          </div>
        </div>

        <!-- Clients Table -->
        <div class="bg-white rounded-lg shadow overflow-hidden">
          <div class="overflow-x-auto">
            <table class="min-w-full divide-y divide-gray-200">
              <thead class="bg-gray-50">
                <tr>
                  <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Name</th>
                  <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Email</th>
                  <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Cellphone</th>
                  <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Company</th>
                  <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Created</th>
                </tr>
              </thead>
              <tbody class="bg-white divide-y divide-gray-200">
                <tr v-for="client in clients" :key="client.id" class="hover:bg-gray-50">
                  <td class="px-6 py-4 whitespace-nowrap">
                    <div class="text-sm font-medium text-gray-900">{{ client.name }} {{ client.surname }}</div>
                  </td>
                  <td class="px-6 py-4 whitespace-nowrap">
                    <div class="text-sm text-gray-900">{{ client.email }}</div>
                  </td>
                  <td class="px-6 py-4 whitespace-nowrap">
                    <div class="text-sm text-gray-900">{{ client.cellphone }}</div>
                  </td>
                  <td class="px-6 py-4 whitespace-nowrap">
                    <div class="text-sm text-gray-900">{{ client.company || '-' }}</div>
                  </td>
                  <td class="px-6 py-4 whitespace-nowrap">
                    <div class="text-sm text-gray-900">{{ new Date(client.dateCreated).toLocaleDateString() }}</div>
                  </td>
                </tr>
                <tr v-if="clients.length === 0">
                  <td colspan="5" class="px-6 py-12 text-center text-gray-500">
                    No clients found
                  </td>
                </tr>
              </tbody>
            </table>
          </div>

          <!-- Pagination -->
          <div v-if="totalPages > 1" class="px-6 py-4 border-t border-gray-200 flex items-center justify-between">
            <div class="text-sm text-gray-700">
              Showing {{ ((currentPage - 1) * pageSize) + 1 }} to {{ Math.min(currentPage * pageSize, totalCount) }} of {{ totalCount }} results
            </div>
            <div class="flex gap-2">
              <Button
                @click="goToPage(currentPage - 1)"
                :disabled="currentPage === 1"
                variant="outline"
                size="sm"
              >
                Previous
              </Button>
              <Button
                v-for="page in paginationPages"
                :key="page"
                @click="goToPage(page)"
                :variant="page === currentPage ? 'default' : 'outline'"
                size="sm"
              >
                {{ page }}
              </Button>
              <Button
                @click="goToPage(currentPage + 1)"
                :disabled="currentPage === totalPages"
                variant="outline"
                size="sm"
              >
                Next
              </Button>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- New Client Modal -->
    <div
      v-if="showNewClientModal"
      class="fixed inset-0 z-[60] overflow-y-auto"
    >
      <div class="flex items-center justify-center min-h-screen px-4 pt-4 pb-20 text-center sm:block sm:p-0">
        <div class="fixed inset-0 transition-opacity bg-gray-500/50" @click="closeNewClientModal"></div>
        
        <!-- Spacer for centering -->
        <span class="hidden sm:inline-block sm:align-middle sm:h-screen" aria-hidden="true">&#8203;</span>

        <div class="relative inline-block align-bottom bg-white rounded-lg text-left overflow-hidden shadow-xl transform transition-all sm:my-8 sm:align-middle sm:max-w-lg sm:w-full">
          <div class="bg-white px-4 pt-5 pb-4 sm:p-6 sm:pb-4">
            <div class="flex items-center justify-between mb-4">
              <h3 class="text-lg font-medium text-gray-900">New Client</h3>
              <button @click="closeNewClientModal" class="text-gray-400 hover:text-gray-500">
                <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/>
                </svg>
              </button>
            </div>

            <div class="space-y-4">
              <div>
                <label class="block text-sm font-medium text-gray-700 mb-1">Name *</label>
                <input
                  v-model="newClient.name"
                  type="text"
                  class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
                  :class="formErrors.name ? 'border-red-500' : ''"
                />
                <p v-if="formErrors.name" class="mt-1 text-sm text-red-600">{{ formErrors.name }}</p>
              </div>

              <div>
                <label class="block text-sm font-medium text-gray-700 mb-1">Surname *</label>
                <input
                  v-model="newClient.surname"
                  type="text"
                  class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
                  :class="formErrors.surname ? 'border-red-500' : ''"
                />
                <p v-if="formErrors.surname" class="mt-1 text-sm text-red-600">{{ formErrors.surname }}</p>
              </div>

              <div>
                <label class="block text-sm font-medium text-gray-700 mb-1">Email *</label>
                <input
                  v-model="newClient.email"
                  type="email"
                  class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
                  :class="formErrors.email ? 'border-red-500' : ''"
                />
                <p v-if="formErrors.email" class="mt-1 text-sm text-red-600">{{ formErrors.email }}</p>
              </div>

              <div>
                <label class="block text-sm font-medium text-gray-700 mb-1">Cellphone *</label>
                <input
                  v-model="newClient.cellphone"
                  type="tel"
                  class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
                  :class="formErrors.cellphone ? 'border-red-500' : ''"
                />
                <p v-if="formErrors.cellphone" class="mt-1 text-sm text-red-600">{{ formErrors.cellphone }}</p>
              </div>

              <div>
                <label class="block text-sm font-medium text-gray-700 mb-1">Address</label>
                <textarea
                  v-model="newClient.address"
                  rows="2"
                  class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
                ></textarea>
              </div>

              <div>
                <label class="block text-sm font-medium text-gray-700 mb-1">Company</label>
                <input
                  v-model="newClient.company"
                  type="text"
                  class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
                />
              </div>
            </div>
          </div>

          <div class="bg-gray-50 px-4 py-3 sm:px-6 sm:flex sm:flex-row-reverse gap-2">
            <Button @click="saveNewClient" class="bg-blue-600 hover:bg-blue-700 text-white">
              Save Client
            </Button>
            <Button @click="closeNewClientModal" variant="outline">
              Cancel
            </Button>
          </div>
        </div>
      </div>
    </div>
  </Layout>
</template>
