<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { clientApi } from '../services/api'
import { useUIStore } from '../stores/ui'
import { useOrganizationStore } from '../stores/organization'
import { useOrganizationContext } from '../composables/useOrganizationContext'
import { Button, Skeleton, Input, Table, TableHeader, TableBody, TableHead, TableRow, TableCell, Pagination, PaginationContent, PaginationItem, PaginationPrevious, PaginationNext } from '../components/ui/index'
import Layout from '../components/Layout.vue'
import NewClientModal from '../components/modals/NewClientModal.vue'
import EditClientModal from '../components/modals/EditClientModal.vue'

// UI Store for modal management
const uiStore = useUIStore()
const organizationStore = useOrganizationStore()
const { ensureOrganizationContext } = useOrganizationContext()

const loading = ref(true)
const clients = ref<any[]>([])
const currentPage = ref(1)
const pageSize = ref(10)
const totalPages = ref(0)
const totalCount = ref(0)
const searchQuery = ref('')


onMounted(async () => {
  await ensureOrganizationContext()
  await fetchClients()
})

const fetchClients = async () => {
  loading.value = true
  try {
    const orgId = organizationStore.currentOrganizationId
    if (!orgId) return
    const response = await clientApi.getClients(
      orgId,
      currentPage.value,
      pageSize.value,
      searchQuery.value || undefined
    )
    
    clients.value = response.data || []
    totalPages.value = response.pagination?.totalPages || 0
    totalCount.value = response.pagination?.totalCount || 0
  } catch (error) {
    uiStore.showError('Failed to load clients')
  } finally {
    loading.value = false
  }
}

const openNewClientModal = () => {
  uiStore.openNewClientModal()
}

const openEditClientModal = (client: any) => {
  const clientData = {
    id: client.id,
    name: client.name,
    email: client.email,
    cellphone: client.cellphone,
    address: client.address || '',
    isCompany: client.isCompany || false,
    vatNumber: client.vatNumber || ''
  }
  uiStore.openEditClientModal(clientData)
}

const saveNewClient = async (client: any) => {
  try {
    const orgId = organizationStore.currentOrganizationId
    await clientApi.createClient(client, orgId ?? undefined)
    uiStore.closeNewClientModal()
    uiStore.showSuccess('Client created successfully')
    await fetchClients()
  } catch (error: any) {
    uiStore.showError(error.response?.data?.message || 'Failed to save client')
  }
}

const saveEditClient = async (client: any) => {
  try {
    await clientApi.updateClient(client.id, client)
    uiStore.closeEditClientModal()
    uiStore.showSuccess('Client updated successfully')
    await fetchClients()
  } catch (error: any) {
    uiStore.showError(error.response?.data?.message || 'Failed to update client')
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

const onPageSizeChange = async () => {
  currentPage.value = 1
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
          <div class="flex items-center gap-3">
            <button
              @click="fetchClients"
              :disabled="loading"
              title="Refresh"
              class="text-gray-600 hover:text-gray-900 disabled:opacity-40 disabled:cursor-not-allowed transition-colors"
            >
              <svg class="h-5 w-5" :class="{ 'animate-spin': loading }" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15"/>
              </svg>
            </button>
            <Button @click="openNewClientModal" variant="outline">
              <svg class="w-5 h-5 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4"/>
              </svg>
              New Client
            </Button>
          </div>
        </div>

        <!-- Search Bar -->
        <div class="mb-6">
          <div class="flex gap-2">
            <Input
              v-model="searchQuery"
              @keyup.enter="handleSearch"
              type="text"
              placeholder="Search clients by name, email, or VAT number..."
              class="flex-1"
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
          <!-- Loading Skeleton -->
          <div v-if="loading" class="p-6 space-y-3">
            <Skeleton class="h-12 w-full" />
            <Skeleton class="h-12 w-full" />
            <Skeleton class="h-12 w-full" />
            <Skeleton class="h-12 w-full" />
            <Skeleton class="h-12 w-full" />
          </div>
          
          <div v-else class="overflow-x-auto">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Name</TableHead>
                  <TableHead>Type</TableHead>
                  <TableHead>Email</TableHead>
                  <TableHead>Cellphone</TableHead>
                  <TableHead>VAT Number</TableHead>
                  <TableHead>Created</TableHead>
                  <TableHead>Actions</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                <TableRow v-for="client in clients" :key="client.id">
                  <TableCell class="font-medium">{{ client.name }}</TableCell>
                  <TableCell>
                    <span :class="client.isCompany ? 'bg-blue-100 text-blue-800' : 'bg-green-100 text-green-800'" class="inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium">
                      {{ client.isCompany ? 'Company' : 'Person' }}
                    </span>
                  </TableCell>
                  <TableCell>{{ client.email }}</TableCell>
                  <TableCell>{{ client.cellphone }}</TableCell>
                  <TableCell>{{ client.vatNumber || '-' }}</TableCell>
                  <TableCell>{{ new Date(client.dateCreated).toLocaleDateString() }}</TableCell>
                  <TableCell>
                    <button
                      @click="openEditClientModal(client)"
                      class="text-blue-600 hover:text-blue-800"
                      title="Edit client"
                    >
                      <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z"/>
                      </svg>
                    </button>
                  </TableCell>
                </TableRow>
                <TableRow v-if="clients.length === 0">
                  <TableCell colspan="7" class="text-center text-gray-500">
                    No clients found
                  </TableCell>
                </TableRow>
              </TableBody>
            </Table>
          </div>
          
          <!-- Footer -->
          <div class="px-6 py-3 border-t border-gray-200 bg-gray-50 flex items-center justify-between gap-4">
            <div class="flex items-center gap-4 text-sm text-gray-600">
              <span>{{ totalCount }} record{{ totalCount !== 1 ? 's' : '' }}</span>
              <div class="flex items-center gap-2">
                <span>Rows per page:</span>
                <select
                  v-model.number="pageSize"
                  @change="onPageSizeChange"
                  class="border border-gray-300 rounded px-2 py-1 text-sm bg-white focus:outline-none focus:ring-1 focus:ring-gray-400"
                >
                  <option :value="10">10</option>
                  <option :value="25">25</option>
                  <option :value="50">50</option>
                </select>
              </div>
            </div>
            <Pagination v-if="totalPages > 0" :total="totalCount" :sibling-count="1" :page="currentPage" :items-per-page="pageSize" @update:page="goToPage">
              <PaginationContent>
                <PaginationPrevious />
                <PaginationItem v-for="page in paginationPages" :key="page" :value="page" />
                <PaginationNext />
              </PaginationContent>
            </Pagination>
          </div>
        </div>
      </div>
    </div>

    <!-- Modals -->
    <NewClientModal
      :show="uiStore.showNewClientModal"
      @close="uiStore.closeNewClientModal"
      @save="saveNewClient"
    />

    <EditClientModal
      :show="uiStore.showEditClientModal"
      :client="uiStore.editClientData"
      @close="uiStore.closeEditClientModal"
      @save="saveEditClient"
    />
  </Layout>
</template>
