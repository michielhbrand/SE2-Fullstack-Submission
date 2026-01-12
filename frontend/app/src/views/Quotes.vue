<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { quoteApi } from '../services/api'
import { Button, Spinner } from '../components/ui/index'
import Layout from '../components/Layout.vue'

const loading = ref(true)
const quotes = ref<any[]>([])
const currentPage = ref(1)
const pageSize = ref(10)
const totalPages = ref(0)
const totalCount = ref(0)
const previewingPdf = ref<number | null>(null)

onMounted(async () => {
  await fetchQuotes()
})

const fetchQuotes = async () => {
  loading.value = true
  try {
    const response = await quoteApi.getQuotes(currentPage.value, pageSize.value)
    
    quotes.value = response.data || []
    totalPages.value = response.pagination?.totalPages || 0
    totalCount.value = response.pagination?.totalCount || 0
  } catch (error) {
    console.error('Failed to fetch quotes:', error)
  } finally {
    loading.value = false
  }
}

const goToPage = async (page: number) => {
  currentPage.value = page
  await fetchQuotes()
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

const getTotalAmount = (quote: any) => {
  return quote.items?.reduce((sum: number, item: any) =>
    sum + (item.amount * item.pricePerUnit), 0) || 0
}

const previewPdf = async (quoteId: number) => {
  try {
    previewingPdf.value = quoteId
    const response = await quoteApi.getPdfUrl(quoteId)
    const pdfUrl = response.url
    
    // Open PDF in new tab
    if (pdfUrl) {
      window.open(pdfUrl, '_blank')
    }
  } catch (error: any) {
    console.error('Failed to preview PDF:', error)
    if (error.response?.status === 404) {
      alert('PDF not yet generated for this quote')
    } else {
      alert('Failed to load PDF preview')
    }
  } finally {
    previewingPdf.value = null
  }
}
</script>

<template>
  <Layout>
    <div class="p-6 lg:p-8">
      <div class="max-w-7xl mx-auto">
        <!-- Header -->
        <div class="mb-8">
          <h2 class="text-3xl font-bold text-gray-900">Quotes</h2>
          <p class="mt-2 text-gray-600">View and manage all quotes</p>
        </div>

        <!-- Quotes Table -->
        <div class="bg-white rounded-lg shadow overflow-hidden">
          <!-- Loading Spinner -->
          <div v-if="loading" class="flex items-center justify-center py-12">
            <Spinner size="lg" />
          </div>
          
          <div v-else class="overflow-x-auto">
            <table class="min-w-full divide-y divide-gray-200">
              <thead class="bg-gray-50">
                <tr>
                  <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Client</th>
                  <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Contact</th>
                  <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Items</th>
                  <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Total</th>
                  <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Date</th>
                  <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Status</th>
                  <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">PDF</th>
                </tr>
              </thead>
              <tbody class="bg-white divide-y divide-gray-200">
                <tr v-for="quote in quotes" :key="quote.id" class="hover:bg-gray-50">
                  <td class="px-6 py-4 whitespace-nowrap">
                    <div class="text-sm font-medium text-gray-900">{{ quote.client?.name }} {{ quote.client?.surname }}</div>
                  </td>
                  <td class="px-6 py-4 whitespace-nowrap">
                    <div class="text-sm text-gray-900">{{ quote.client?.cellphone }}</div>
                  </td>
                  <td class="px-6 py-4 whitespace-nowrap">
                    <div class="text-sm text-gray-900">{{ quote.items?.length || 0 }} item(s)</div>
                  </td>
                  <td class="px-6 py-4 whitespace-nowrap">
                    <div class="text-sm font-medium text-gray-900">R {{ getTotalAmount(quote).toFixed(2) }}</div>
                  </td>
                  <td class="px-6 py-4 whitespace-nowrap">
                    <div class="text-sm text-gray-900">{{ new Date(quote.dateCreated).toLocaleDateString() }}</div>
                  </td>
                  <td class="px-6 py-4 whitespace-nowrap">
                    <span
                      class="px-2 inline-flex text-xs leading-5 font-semibold rounded-full"
                      :class="quote.notificationSent ? 'bg-green-100 text-green-800' : 'bg-yellow-100 text-yellow-800'"
                    >
                      {{ quote.notificationSent ? 'Sent' : 'Pending' }}
                    </span>
                  </td>
                  <td class="px-6 py-4 whitespace-nowrap">
                    <button
                      v-if="quote.pdfStorageKey"
                      @click="previewPdf(quote.id)"
                      :disabled="previewingPdf === quote.id"
                      class="text-blue-600 hover:text-blue-800 disabled:text-gray-400 disabled:cursor-not-allowed transition-colors"
                      title="Preview PDF"
                    >
                      <svg
                        v-if="previewingPdf === quote.id"
                        class="animate-spin h-5 w-5"
                        xmlns="http://www.w3.org/2000/svg"
                        fill="none"
                        viewBox="0 0 24 24"
                      >
                        <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                        <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                      </svg>
                      <svg
                        v-else
                        xmlns="http://www.w3.org/2000/svg"
                        class="h-5 w-5"
                        viewBox="0 0 20 20"
                        fill="currentColor"
                      >
                        <path d="M10 12a2 2 0 100-4 2 2 0 000 4z" />
                        <path fill-rule="evenodd" d="M.458 10C1.732 5.943 5.522 3 10 3s8.268 2.943 9.542 7c-1.274 4.057-5.064 7-9.542 7S1.732 14.057.458 10zM14 10a4 4 0 11-8 0 4 4 0 018 0z" clip-rule="evenodd" />
                      </svg>
                    </button>
                    <span v-else class="text-gray-400 text-sm">N/A</span>
                  </td>
                </tr>
                <tr v-if="quotes.length === 0">
                  <td colspan="7" class="px-6 py-12 text-center text-gray-500">
                    No quotes found
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
