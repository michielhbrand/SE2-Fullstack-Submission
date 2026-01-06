<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import apiClient from '../services/api'
import { Button } from '../components/ui/index'
import Layout from '../components/Layout.vue'

const loading = ref(true)
const invoices = ref<any[]>([])
const currentPage = ref(1)
const pageSize = ref(10)
const totalPages = ref(0)
const totalCount = ref(0)

onMounted(async () => {
  loading.value = false
  await fetchInvoices()
})

const fetchInvoices = async () => {
  try {
    const params = {
      page: currentPage.value,
      pageSize: pageSize.value
    }

    const response = await apiClient.get('/api/Invoice', { params })
    
    invoices.value = response.data.data
    totalPages.value = response.data.pagination.totalPages
    totalCount.value = response.data.pagination.totalCount
  } catch (error) {
    console.error('Failed to fetch invoices:', error)
  }
}

const goToPage = async (page: number) => {
  currentPage.value = page
  await fetchInvoices()
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

const getTotalAmount = (invoice: any) => {
  return invoice.items?.reduce((sum: number, item: any) => 
    sum + (item.amount * item.pricePerUnit), 0) || 0
}
</script>

<template>
  <Layout>
    <div class="p-6 lg:p-8">
      <div class="max-w-7xl mx-auto">
        <!-- Header -->
        <div class="mb-8">
          <h2 class="text-3xl font-bold text-gray-900">Invoices</h2>
          <p class="mt-2 text-gray-600">View and manage all invoices</p>
        </div>

        <!-- Invoices Table -->
        <div class="bg-white rounded-lg shadow overflow-hidden">
          <div class="overflow-x-auto">
            <table class="min-w-full divide-y divide-gray-200">
              <thead class="bg-gray-50">
                <tr>
                  <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Client</th>
                  <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Contact</th>
                  <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Items</th>
                  <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Total</th>
                  <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Date</th>
                  <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Status</th>
                </tr>
              </thead>
              <tbody class="bg-white divide-y divide-gray-200">
                <tr v-for="invoice in invoices" :key="invoice.id" class="hover:bg-gray-50">
                  <td class="px-6 py-4 whitespace-nowrap">
                    <div class="text-sm font-medium text-gray-900">{{ invoice.clientName }} {{ invoice.clientSurname }}</div>
                  </td>
                  <td class="px-6 py-4 whitespace-nowrap">
                    <div class="text-sm text-gray-900">{{ invoice.clientCellphone }}</div>
                  </td>
                  <td class="px-6 py-4 whitespace-nowrap">
                    <div class="text-sm text-gray-900">{{ invoice.items?.length || 0 }} item(s)</div>
                  </td>
                  <td class="px-6 py-4 whitespace-nowrap">
                    <div class="text-sm font-medium text-gray-900">R {{ getTotalAmount(invoice).toFixed(2) }}</div>
                  </td>
                  <td class="px-6 py-4 whitespace-nowrap">
                    <div class="text-sm text-gray-900">{{ new Date(invoice.dateCreated).toLocaleDateString() }}</div>
                  </td>
                  <td class="px-6 py-4 whitespace-nowrap">
                    <span
                      class="px-2 inline-flex text-xs leading-5 font-semibold rounded-full"
                      :class="invoice.notificationSent ? 'bg-green-100 text-green-800' : 'bg-yellow-100 text-yellow-800'"
                    >
                      {{ invoice.notificationSent ? 'Sent' : 'Pending' }}
                    </span>
                  </td>
                </tr>
                <tr v-if="invoices.length === 0">
                  <td colspan="6" class="px-6 py-12 text-center text-gray-500">
                    No invoices found
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
  </Layout>
</template>
