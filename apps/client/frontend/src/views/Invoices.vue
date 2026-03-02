<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { invoiceApi } from '../services/api'
import { usePagination } from '../composables/usePagination'
import { Skeleton, Table, TableHeader, TableBody, TableHead, TableRow, TableCell, Pagination, PaginationContent, PaginationItem, PaginationPrevious, PaginationNext } from '../components/ui/index'
import Layout from '../components/Layout.vue'
import TemplatePdfPreviewModal from '../components/modals/TemplatePdfPreviewModal.vue'
import { toast } from 'vue-sonner'
import { useOrganizationStore } from '../stores/organization'
import { useOrganizationContext } from '../composables/useOrganizationContext'

const organizationStore = useOrganizationStore()
const { ensureOrganizationContext } = useOrganizationContext()

const loading = ref(true)
const invoices = ref<any[]>([])
const statusFilter = ref<string>('')
const { page: currentPage, pageSize, total: totalCount, totalPages, paginationPages } = usePagination(10)
const pdfModalOpen = ref(false)
const pdfPreviewUrl = ref<string | null>(null)
const pdfPreviewLoading = ref(false)
const pdfPreviewTitle = ref<string | null>(null)
const search = ref<string>('')

let searchTimeout: ReturnType<typeof setTimeout> | null = null

onMounted(async () => {
  await ensureOrganizationContext()
  await fetchInvoices()
})

const fetchInvoices = async () => {
  loading.value = true
  try {
    const orgId = organizationStore.currentOrganizationId
    if (!orgId) return
    const response = await invoiceApi.getInvoices(
      orgId, currentPage.value, pageSize.value,
      statusFilter.value || undefined,
      search.value || undefined
    )
    invoices.value = response.data || []
    totalCount.value = response.pagination?.totalCount || 0
  } catch (error) {
    toast.error('Failed to fetch invoices')
  } finally {
    loading.value = false
  }
}

const setStatusFilter = async (value: string) => {
  statusFilter.value = value
  currentPage.value = 1
  await fetchInvoices()
}

const onSearchInput = () => {
  if (searchTimeout) clearTimeout(searchTimeout)
  searchTimeout = setTimeout(async () => {
    currentPage.value = 1
    await fetchInvoices()
  }, 300)
}

const statusFilterOptions = [
  { value: '', label: 'All' },
  { value: 'NotPaid', label: 'Not Paid' },
  { value: 'Overdue', label: 'Overdue' },
  { value: 'Paid', label: 'Paid' },
]

const paymentStatusBadge = (status: string) => {
  switch (status) {
    case 'Paid':    return { label: 'Paid',     classes: 'bg-emerald-100 text-emerald-800' }
    case 'Overdue': return { label: 'Overdue',  classes: 'bg-red-100 text-red-700' }
    default:        return { label: 'Not Paid', classes: 'bg-gray-100 text-gray-600' }
  }
}

const goToPage = async (page: number) => {
  currentPage.value = page
  await fetchInvoices()
}

const onPageSizeChange = async () => {
  currentPage.value = 1
  await fetchInvoices()
}

const getTotalAmount = (invoice: any) =>
  invoice.items?.reduce((sum: number, item: any) =>
    sum + (item.total || (item.quantity * item.unitPrice) || 0), 0) || 0

const previewPdf = async (invoice: any) => {
  pdfModalOpen.value = true
  pdfPreviewLoading.value = true
  pdfPreviewUrl.value = null
  pdfPreviewTitle.value = `Invoice — ${invoice.client?.name ?? `#${invoice.id}`}`
  try {
    const response = await invoiceApi.getPdfUrl(invoice.id)
    if (response.url) {
      pdfPreviewUrl.value = response.url
    } else {
      toast.error('PDF not yet generated for this invoice')
      pdfModalOpen.value = false
    }
  } catch (error: any) {
    if (error.response?.status === 404) toast.error('PDF not yet generated for this invoice')
    else toast.error('Failed to load PDF preview')
    pdfModalOpen.value = false
  } finally {
    pdfPreviewLoading.value = false
  }
}

const closePdfModal = () => {
  pdfModalOpen.value = false
  pdfPreviewUrl.value = null
  pdfPreviewTitle.value = null
}
</script>

<template>
  <Layout>
    <div class="p-6 lg:p-8">
      <div class="max-w-7xl mx-auto">
        <!-- Header -->
        <div class="mb-6 flex items-center justify-between">
          <div>
            <h2 class="text-3xl font-bold text-gray-900">Invoices</h2>
            <p class="mt-2 text-gray-600">View and manage all invoices</p>
          </div>
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

        <!-- Search + Filter bar -->
        <div class="mb-4 flex items-center gap-3 flex-wrap">
          <!-- Search box -->
          <div class="relative flex-1 min-w-[200px] max-w-sm">
            <svg class="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-gray-400 pointer-events-none" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-4.35-4.35M17 11A6 6 0 105 11a6 6 0 0012 0z"/>
            </svg>
            <input
              v-model="search"
              @input="onSearchInput"
              type="text"
              placeholder="Search by client name..."
              class="w-full pl-9 pr-3 py-1.5 text-sm border border-gray-300 rounded-md bg-white focus:outline-none focus:ring-1 focus:ring-gray-400"
            />
          </div>

          <!-- Status filter button group -->
          <div class="flex rounded-md border border-gray-300 overflow-hidden">
            <button
              v-for="opt in statusFilterOptions"
              :key="opt.value"
              @click="setStatusFilter(opt.value)"
              :disabled="loading"
              :class="[
                'px-3 py-1.5 text-sm font-medium transition-colors border-r last:border-r-0 border-gray-300 disabled:opacity-40 disabled:cursor-not-allowed',
                statusFilter === opt.value
                  ? 'bg-gray-900 text-white'
                  : 'bg-white text-gray-600 hover:bg-gray-50'
              ]"
            >
              {{ opt.label }}
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
                    <span
                      :class="['inline-flex items-center px-2 py-0.5 rounded text-xs font-medium', paymentStatusBadge(invoice.paymentStatus).classes]"
                    >
                      {{ paymentStatusBadge(invoice.paymentStatus).label }}
                    </span>
                  </TableCell>
                  <TableCell>
                    <button
                      v-if="invoice.pdfStorageKey"
                      @click="previewPdf(invoice)"
                      :disabled="pdfPreviewLoading"
                      class="text-blue-600 hover:text-blue-800 disabled:text-gray-400 disabled:cursor-not-allowed transition-colors"
                      title="Preview PDF"
                    >
                      <svg xmlns="http://www.w3.org/2000/svg" class="h-5 w-5" viewBox="0 0 20 20" fill="currentColor">
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

    <TemplatePdfPreviewModal
      :show="pdfModalOpen"
      :preview-url="pdfPreviewUrl"
      :template-name="pdfPreviewTitle"
      :loading="pdfPreviewLoading"
      @close="closePdfModal"
    />
  </Layout>
</template>
