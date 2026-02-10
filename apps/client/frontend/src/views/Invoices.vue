<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { invoiceApi } from '../services/api'
import { Button, Spinner, Skeleton, Badge, Table, TableHeader, TableBody, TableHead, TableRow, TableCell, Pagination, PaginationContent, PaginationItem, PaginationPrevious, PaginationNext } from '../components/ui/index'
import Layout from '../components/Layout.vue'
import { toast } from 'vue-sonner'

const loading = ref(true)
const invoices = ref<any[]>([])
const currentPage = ref(1)
const pageSize = ref(10)
const totalPages = ref(0)
const totalCount = ref(0)
const previewingPdf = ref<number | null>(null)

onMounted(async () => {
  await fetchInvoices()
})

const fetchInvoices = async () => {
  loading.value = true
  try {
    const response = await invoiceApi.getInvoices(currentPage.value, pageSize.value)
    
    invoices.value = response.data || []
    totalPages.value = response.pagination?.totalPages || 0
    totalCount.value = response.pagination?.totalCount || 0
  } catch (error) {
    console.error('Failed to fetch invoices:', error)
    toast.error('Failed to fetch invoices')
  } finally {
    loading.value = false
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

const previewPdf = async (invoiceId: number) => {
  try {
    previewingPdf.value = invoiceId
    const response = await invoiceApi.getPdfUrl(invoiceId)
    const pdfUrl = response.url
    
    // Open PDF in new tab
    if (pdfUrl) {
      window.open(pdfUrl, '_blank')
    }
  } catch (error: any) {
    console.error('Failed to preview PDF:', error)
    if (error.response?.status === 404) {
      toast.error('PDF not yet generated for this invoice')
    } else {
      toast.error('Failed to load PDF preview')
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
          <h2 class="text-3xl font-bold text-gray-900">Invoices</h2>
          <p class="mt-2 text-gray-600">View and manage all invoices</p>
        </div>

        <!-- Invoices Table -->
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
                  <TableHead>Client</TableHead>
                  <TableHead>Contact</TableHead>
                  <TableHead>Items</TableHead>
                  <TableHead>Total</TableHead>
                  <TableHead>Date</TableHead>
                  <TableHead>Status</TableHead>
                  <TableHead>PDF</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                <TableRow v-for="invoice in invoices" :key="invoice.id">
                  <TableCell class="font-medium">{{ invoice.client?.name }} {{ invoice.client?.surname }}</TableCell>
                  <TableCell>{{ invoice.client?.cellphone }}</TableCell>
                  <TableCell>{{ invoice.items?.length || 0 }} item(s)</TableCell>
                  <TableCell class="font-medium">R {{ getTotalAmount(invoice).toFixed(2) }}</TableCell>
                  <TableCell>{{ new Date(invoice.dateCreated).toLocaleDateString() }}</TableCell>
                  <TableCell>
                    <Badge :variant="invoice.notificationSent ? 'default' : 'secondary'">
                      {{ invoice.notificationSent ? 'Sent' : 'Pending' }}
                    </Badge>
                  </TableCell>
                  <TableCell>
                    <button
                      v-if="invoice.pdfStorageKey"
                      @click="previewPdf(invoice.id)"
                      :disabled="previewingPdf === invoice.id"
                      class="text-blue-600 hover:text-blue-800 disabled:text-gray-400 disabled:cursor-not-allowed transition-colors"
                      title="Preview PDF"
                    >
                      <svg
                        v-if="previewingPdf === invoice.id"
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
                  </TableCell>
                </TableRow>
                <TableRow v-if="invoices.length === 0">
                  <TableCell colspan="7" class="text-center text-gray-500">
                    No invoices found
                  </TableCell>
                </TableRow>
              </TableBody>
            </Table>
          </div>
          
          <!-- Pagination -->
          <div v-if="totalPages > 1" class="px-6 py-4 border-t border-gray-200 flex items-center justify-between">
            <div class="text-sm text-gray-700">
              Showing {{ ((currentPage - 1) * pageSize) + 1 }} to {{ Math.min(currentPage * pageSize, totalCount) }} of {{ totalCount }} results
            </div>
            <Pagination
              :total="totalCount"
              :sibling-count="1"
              :page="currentPage"
              :items-per-page="pageSize"
              @update:page="goToPage"
            >
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
  </Layout>
</template>
