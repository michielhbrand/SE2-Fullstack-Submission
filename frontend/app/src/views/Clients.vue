<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import apiClient from '../services/api'
import { Button, Spinner } from '../components/ui/index'
import Layout from '../components/Layout.vue'
import NewClientModal from '../components/modals/NewClientModal.vue'
import EditClientModal from '../components/modals/EditClientModal.vue'

const loading = ref(true)
const showNewClientModal = ref(false)
const showEditClientModal = ref(false)
const clients = ref<any[]>([])
const currentPage = ref(1)
const pageSize = ref(10)
const totalPages = ref(0)
const totalCount = ref(0)
const searchQuery = ref('')

// Edit client data
const editClientData = ref<any>(null)

onMounted(async () => {
  await fetchClients()
})

const fetchClients = async () => {
  loading.value = true
  try {
    const params: any = {
      page: currentPage.value,
      pageSize: pageSize.value
    }
    
    if (searchQuery.value) {
      params.search = searchQuery.value
    }

    const response = await apiClient.get('/api/Client', { params })
    
    clients.value = response.data.data
    totalPages.value = response.data.pagination.totalPages
    totalCount.value = response.data.pagination.totalCount
  } catch (error) {
    console.error('Failed to fetch clients:', error)
  } finally {
    loading.value = false
  }
}

const openNewClientModal = () => {
  showNewClientModal.value = true
}

const closeNewClientModal = () => {
  showNewClientModal.value = false
}

const openEditClientModal = (client: any) => {
  editClientData.value = {
    id: client.id,
    name: client.name,
    surname: client.surname,
    email: client.email,
    cellphone: client.cellphone,
    address: client.address || '',
    company: client.company || ''
  }
  showEditClientModal.value = true
}

const closeEditClientModal = () => {
  showEditClientModal.value = false
  editClientData.value = null
}

const saveNewClient = async (client: any) => {
  try {
    await apiClient.post('/api/Client', client)
    closeNewClientModal()
    await fetchClients()
  } catch (error: any) {
    console.error('Failed to save client:', error)
    alert(error.response?.data?.message || 'Failed to save client')
  }
}

const saveEditClient = async (client: any) => {
  try {
    await apiClient.put(`/api/Client/${client.id}`, client)
    closeEditClientModal()
    await fetchClients()
  } catch (error: any) {
    console.error('Failed to update client:', error)
    alert(error.response?.data?.message || 'Failed to update client')
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
          <!-- Loading Spinner -->
          <div v-if="loading" class="flex items-center justify-center py-12">
            <Spinner size="lg" />
          </div>
          
          <div v-else class="overflow-x-auto">
            <table class="min-w-full divide-y divide-gray-200">
              <thead class="bg-gray-50">
                <tr>
                  <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Name</th>
                  <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Email</th>
                  <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Cellphone</th>
                  <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Company</th>
                  <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Created</th>
                  <th class="px-0 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Actions</th>
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
                  <td class="px-6 py-4 whitespace-nowrap">
                    <button
                      @click="openEditClientModal(client)"
                      variant="default"
                      title="Edit client"
                    >
                      <v-icon icon="mdi-file-edit-outline"></v-icon>
                    </button>
                  </td>
                </tr>
                <tr v-if="clients.length === 0">
                  <td colspan="6" class="px-6 py-12 text-center text-gray-500">
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

    <!-- Modals -->
    <NewClientModal
      :show="showNewClientModal"
      @close="closeNewClientModal"
      @save="saveNewClient"
    />

    <EditClientModal
      :show="showEditClientModal"
      :client="editClientData"
      @close="closeEditClientModal"
      @save="saveEditClient"
    />
  </Layout>
</template>
