<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { invoiceApi } from '../services/api'
import { Skeleton, Table, TableHeader, TableBody, TableHead, TableRow, TableCell, Pagination, PaginationContent, PaginationItem, PaginationPrevious, PaginationNext, Badge } from '../components/ui/index'
import Layout from '../components/Layout.vue'
import { toast } from 'vue-sonner'
import { useOrganizationStore } from '../stores/organization'
import { useOrganizationContext } from '../composables/useOrganizationContext'

const organizationStore = useOrganizationStore()
const { ensureOrganizationContext } = useOrganizationContext()

const loading = ref(true)
const invoices = ref<any[]>([])
const currentPage = ref(1)
const pageSize = ref(10)
const totalPages = ref(0)
const totalCount = ref(0)
const previewingPdf = ref<number | null>(null)
const overdueOnly = ref(false)

onMounted(async () => {
  await ensureOrganizationContext()
  await fetchInvoices()
})

const fetchInvoices = async () => {
  loading.value = true
  try {
    const orgId = organizationStore.currentOrganizationId
    if (!orgId) return
    const response = await invoiceApi.getInvoices(orgId, currentPage.value, pageSize.value, overdueOnly.value)
    invoices.value = response.data || []
    totalPages.value = response.pagination?.totalPages || 0
    totalCount.value = response.pagination?.totalCount || 0
  } catch (error) {
    toast.error('Failed to fetch invoices')
  } finally {
    loading.value = false
  }
}

const toggleOverdueFilter = async () => {
  overdueOnly.value = !overdueOnly.value
  currentPage.value = 1
  await fetchInvoices()
}

const goToPage = async (page: number) => {
  currentPage.value = page
  await fetchInvoices()
}

const onPageSizeChange = async () => {
  currentPage.value = 1
  await fetchInvoices()
}

const paginationPages = computed(() => {
  const pages = []
  const maxVisible = 5
  let start = Math.max(1, currentPage.value - Math.floor(maxVisible / 2))
  let end = Math.min(totalPages.value, start + maxVisible - 1)
  if (end - start < maxVisible - 1) start = Math.max(1, end - maxVisible + 1)
  for (let i = start; i <= end; i++) pages.push(i)
  return pages
})

const getTotalAmount = (invoice: any) =>
  invoice.items?.reduce((sum: number, item: any) =>
    sum + (item.total || (item.quantity * item.unitPrice) || 0), 0) || 0

const previewPdf = async (invoiceId: number) => {
  try {
    previewingPdf.value = invoiceId
    const response = await invoiceApi.getPdfUrl(invoiceId)
    if (response.url) window.open(response.url, '_blank')
  } catch (error: any) {
    if (error.response?.status === 404) toast.error('PDF not yet generated for this invoice')
    else toast.error('Failed to load PDF preview')
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
        <div class="mb-8 flex items-center justify-between">
          <div>
            <h2 class="text-3xl font-bold text-gray-900">Invoices</h2>
            <p class="mt-2 text-gray-600">View and manage all invoices</p>
          </div>
          <div class="flex items-center gap-3">
            <button
              @click="toggleOverdueFilter"
              :disabled="loading"
              :class="[
                'px-3 py-1.5 rounded-md text-sm font-medium border transition-colors disabled:opacity-40 disabled:cursor-not-allowed',
                overdueOnly
                  ? 'bg-orange-100 text-orange-800 border-orange-300 hover:bg-orange-200'
                  : 'bg-white text-gray-600 border-gray-300 hover:bg-gray-50'
              ]"
              title="Toggle overdue filter"
            >
              Overdue only
            </button>
            <button
              @click="fetchInvoices"
              :disabled="loading"
              title="Refresh"
              class="text-gray-600 hover:text-gray-900 disabled:opacity-40 disabled:cursor-not-allowed transition-colors"
            >
              <svg class="h-5 w-5" :class="{ 'animate-spin': loading }" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15"/>
              </svg>
            </button>
          </div>
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
                  <TableHead>Pay By</TableHead>
                  <TableHead>Status</TableHead>
                  <TableHead>PDF</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                <TableRow v-for="invoice in invoices" :key="invoice.id">
                  <TableCell class="font-medium">{{ invoice.client?.name }}</TableCell>
                  <TableCell>{{ invoice.client?.cellphone }}</TableCell>
                  <TableCell>{{ invoice.items?.length || 0 }} item(s)</TableCell>
                  <TableCell class="font-medium">R {{ getTotalAmount(invoice).toFixed(2) }}</TableCell>
                  <TableCell>{{ new Date(invoice.dateCreated).toLocaleDateString() }}</TableCell>
                  <TableCell>
                    <span v-if="invoice.payByDate" :class="new Date(invoice.payByDate) < new Date() ? 'text-red-600 font-medium' : ''">
                      {{ new Date(invoice.payByDate).toLocaleDateString() }}
                    </span>
                    <span v-else class="text-gray-400">—</span>
                  </TableCell>
                  <TableCell>
                    <Badge v-if="invoice.notificationSent" variant="secondary" class="text-xs">
                      Notified
                    </Badge>
                    <span v-else class="text-gray-400 text-sm">—</span>
                  </TableCell>
                  <TableCell>
                    <button
                      v-if="invoice.pdfStorageKey"
                      @click="previewPdf(invoice.id)"
                      :disabled="previewingPdf === invoice.id"
                      class="text-blue-600 hover:text-blue-800 disabled:text-gray-400 disabled:cursor-not-allowed transition-colors"
                      title="Preview PDF"
                    >
                      <svg v-if="previewingPdf === invoice.id" class="animate-spin h-5 w-5" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                        <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                        <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                      </svg>
                      <svg v-else xmlns="http://www.w3.org/2000/svg" class="h-5 w-5" viewBox="0 0 20 20" fill="currentColor">
                        <path d="M10 12a2 2 0 100-4 2 2 0 000 4z" />
                        <path fill-rule="evenodd" d="M.458 10C1.732 5.943 5.522 3 10 3s8.268 2.943 9.542 7c-1.274 4.057-5.064 7-9.542 7S1.732 14.057.458 10zM14 10a4 4 0 11-8 0 4 4 0 018 0z" clip-rule="evenodd" />
                      </svg>
                    </button>
                    <span v-else class="text-gray-400 text-sm">N/A</span>
                  </TableCell>
                </TableRow>
                <TableRow v-if="invoices.length === 0">
                  <TableCell colspan="8" class="text-center text-gray-500 py-8">No invoices found</TableCell>
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
  </Layout>
</template>
