<script setup lang="ts">
import { ref, onMounted, onUnmounted, computed } from 'vue'
import { useRouter } from 'vue-router'
import { workflowApi } from '../services/api'
import { useOrganizationStore } from '../stores/organization'
import { useOrganizationContext } from '../composables/useOrganizationContext'
import { Button, Skeleton, Input, Table, TableHeader, TableBody, TableHead, TableRow, TableCell, Pagination, PaginationContent, PaginationItem, PaginationPrevious, PaginationNext } from '../components/ui/index'
import Layout from '../components/Layout.vue'
import { toast } from 'vue-sonner'

const router = useRouter()
const organizationStore = useOrganizationStore()
const { ensureOrganizationContext } = useOrganizationContext()

const loading = ref(true)
const workflows = ref<any[]>([])
const currentPage = ref(1)
const pageSize = ref(10)
const totalPages = ref(0)
const totalCount = ref(0)

// Search & filter
const searchQuery = ref('')
const selectedStatuses = ref<string[]>([])
const isStatusDropdownOpen = ref(false)
const statusFilterRef = ref<HTMLDivElement | null>(null)

const allStatuses = [
  { value: 'Draft',           label: 'Draft',             colorClass: 'bg-gray-100 text-gray-800' },
  { value: 'PendingApproval', label: 'Pending Approval',  colorClass: 'bg-yellow-100 text-yellow-800' },
  { value: 'Approved',        label: 'Approved',          colorClass: 'bg-green-100 text-green-800' },
  { value: 'Rejected',        label: 'Rejected',          colorClass: 'bg-red-100 text-red-800' },
  { value: 'InvoiceCreated',  label: 'Invoice Created',   colorClass: 'bg-blue-100 text-blue-800' },
  { value: 'SentForPayment',  label: 'Sent for Payment',  colorClass: 'bg-purple-100 text-purple-800' },
  { value: 'Paid',            label: 'Paid',              colorClass: 'bg-emerald-100 text-emerald-800' },
  { value: 'Cancelled',       label: 'Cancelled',         colorClass: 'bg-orange-100 text-orange-800' },
  { value: 'Terminated',      label: 'Terminated',        colorClass: 'bg-red-200 text-red-900' },
]

onMounted(async () => {
  document.addEventListener('click', handleStatusClickOutside)
  await fetchWorkflows()
})

onUnmounted(() => {
  document.removeEventListener('click', handleStatusClickOutside)
})

const handleStatusClickOutside = (e: MouseEvent) => {
  if (statusFilterRef.value && !statusFilterRef.value.contains(e.target as Node)) {
    isStatusDropdownOpen.value = false
  }
}

const fetchWorkflows = async () => {
  loading.value = true
  try {
    const orgId = await ensureOrganizationContext()
    if (!orgId) {
      toast.error('No organization selected')
      loading.value = false
      return
    }
    const response = await workflowApi.getWorkflows(
      orgId,
      currentPage.value,
      pageSize.value,
      searchQuery.value || undefined,
      selectedStatuses.value.length > 0 ? selectedStatuses.value : undefined
    )
    workflows.value = response.data || []
    totalPages.value = response.pagination?.totalPages || 0
    totalCount.value = response.pagination?.totalCount || 0
  } catch (error) {
    toast.error('Failed to fetch workflows')
  } finally {
    loading.value = false
  }
}

const handleSearch = async () => {
  currentPage.value = 1
  await fetchWorkflows()
}

const toggleStatus = async (status: string) => {
  const idx = selectedStatuses.value.indexOf(status)
  if (idx >= 0) selectedStatuses.value.splice(idx, 1)
  else selectedStatuses.value.push(status)
  currentPage.value = 1
  await fetchWorkflows()
}

const clearStatusFilter = async () => {
  selectedStatuses.value = []
  isStatusDropdownOpen.value = false
  currentPage.value = 1
  await fetchWorkflows()
}

const goToPage = async (page: number) => {
  currentPage.value = page
  await fetchWorkflows()
}

const onPageSizeChange = async () => {
  currentPage.value = 1
  await fetchWorkflows()
}

const paginationPages = computed(() => {
  const pages: number[] = []
  const maxVisible = 5
  let start = Math.max(1, currentPage.value - Math.floor(maxVisible / 2))
  let end = Math.min(totalPages.value, start + maxVisible - 1)
  if (end - start < maxVisible - 1) start = Math.max(1, end - maxVisible + 1)
  for (let i = start; i <= end; i++) pages.push(i)
  return pages
})

const viewWorkflow = (id: number) => {
  router.push(`/workflows/${id}`)
}

const getStatusColor = (status: string): string => {
  return allStatuses.find(s => s.value === status)?.colorClass || 'bg-gray-100 text-gray-800'
}

const getStatusLabel = (status: string): string => {
  return allStatuses.find(s => s.value === status)?.label || status
}

const getTypeLabel = (type: string): string =>
  type === 'QuoteFirst' ? 'Quote → Invoice' : 'Invoice Only'

const getTypeBadgeClass = (type: string): string =>
  type === 'QuoteFirst' ? 'bg-indigo-100 text-indigo-800' : 'bg-cyan-100 text-cyan-800'
</script>

<template>
  <Layout>
    <div class="p-6 lg:p-8">
      <div class="max-w-7xl mx-auto">
        <!-- Header -->
        <div class="mb-6 flex items-center justify-between">
          <div>
            <h2 class="text-3xl font-bold text-gray-900">Workflows</h2>
            <p class="mt-2 text-gray-600">Track the lifecycle of your quotes and invoices</p>
          </div>
          <button
            @click="fetchWorkflows"
            :disabled="loading"
            title="Refresh"
            class="text-gray-600 hover:text-gray-900 disabled:opacity-40 disabled:cursor-not-allowed transition-colors"
          >
            <svg class="h-5 w-5" :class="{ 'animate-spin': loading }" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15"/>
            </svg>
          </button>
        </div>

        <!-- Search & Filter toolbar -->
        <div class="mb-4 flex items-center gap-3">
          <!-- Search -->
          <div class="flex gap-2 flex-1">
            <Input
              v-model="searchQuery"
              @keyup.enter="handleSearch"
              type="text"
              placeholder="Search by client name or email..."
              class="flex-1"
            />
            <Button @click="handleSearch" variant="outline" class="text-gray-600">
              <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z"/>
              </svg>
            </Button>
          </div>

          <!-- Status multi-select -->
          <div class="relative" ref="statusFilterRef">
            <button
              type="button"
              @click="isStatusDropdownOpen = !isStatusDropdownOpen"
              class="flex items-center gap-2 px-3 py-2 border border-gray-300 rounded-lg bg-white text-sm hover:bg-gray-50 transition-colors"
              :class="selectedStatuses.length > 0 ? 'border-blue-400 text-blue-700' : 'text-gray-600'"
            >
              <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 4a1 1 0 011-1h16a1 1 0 011 1v2a1 1 0 01-.293.707L13 13.414V19a1 1 0 01-.553.894l-4 2A1 1 0 017 21v-7.586L3.293 6.707A1 1 0 013 6V4z"/>
              </svg>
              Status{{ selectedStatuses.length > 0 ? ` (${selectedStatuses.length})` : '' }}
              <svg class="w-4 h-4 text-gray-400 transition-transform" :class="{ 'rotate-180': isStatusDropdownOpen }" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7"/>
              </svg>
            </button>

            <div
              v-if="isStatusDropdownOpen"
              class="absolute right-0 top-full mt-1 z-20 bg-white border border-gray-200 rounded-lg shadow-lg min-w-52 py-1"
            >
              <label
                v-for="status in allStatuses"
                :key="status.value"
                class="flex items-center gap-3 px-4 py-2 hover:bg-gray-50 cursor-pointer"
                @click.prevent="toggleStatus(status.value)"
              >
                <input
                  type="checkbox"
                  :checked="selectedStatuses.includes(status.value)"
                  class="rounded border-gray-300"
                  @click.stop
                  @change="toggleStatus(status.value)"
                />
                <span
                  class="inline-flex items-center rounded-full px-2 py-0.5 text-xs font-semibold"
                  :class="status.colorClass"
                >
                  {{ status.label }}
                </span>
              </label>
              <div v-if="selectedStatuses.length > 0" class="border-t border-gray-100 mt-1 px-4 py-2">
                <button @click="clearStatusFilter" class="text-xs text-gray-500 hover:text-gray-800">
                  Clear filter
                </button>
              </div>
            </div>
          </div>
        </div>

        <!-- Workflows Table -->
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
                  <TableHead>ID</TableHead>
                  <TableHead>Type</TableHead>
                  <TableHead>Client</TableHead>
                  <TableHead>Status</TableHead>
                  <TableHead>Quote</TableHead>
                  <TableHead>Invoice</TableHead>
                  <TableHead>Created</TableHead>
                  <TableHead>Updated</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                <TableRow
                  v-for="workflow in workflows"
                  :key="workflow.id"
                  class="cursor-pointer hover:bg-gray-50"
                  @click="viewWorkflow(workflow.id)"
                >
                  <TableCell class="font-mono text-sm">#{{ workflow.id }}</TableCell>
                  <TableCell>
                    <span class="inline-flex items-center rounded-md px-2 py-1 text-xs font-medium" :class="getTypeBadgeClass(workflow.type)">
                      {{ getTypeLabel(workflow.type) }}
                    </span>
                  </TableCell>
                  <TableCell class="font-medium">{{ workflow.clientName || `Client #${workflow.clientId}` }}</TableCell>
                  <TableCell>
                    <span class="inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-semibold" :class="getStatusColor(workflow.status)">
                      {{ getStatusLabel(workflow.status) }}
                    </span>
                  </TableCell>
                  <TableCell>
                    <span v-if="workflow.quoteId" class="text-blue-600 font-mono text-sm">#{{ workflow.quoteId }}</span>
                    <span v-else class="text-gray-400 text-sm">—</span>
                  </TableCell>
                  <TableCell>
                    <span v-if="workflow.invoiceId" class="text-blue-600 font-mono text-sm">#{{ workflow.invoiceId }}</span>
                    <span v-else class="text-gray-400 text-sm">—</span>
                  </TableCell>
                  <TableCell class="text-sm text-gray-600">{{ new Date(workflow.createdAt).toLocaleDateString() }}</TableCell>
                  <TableCell class="text-sm text-gray-600">{{ workflow.updatedAt ? new Date(workflow.updatedAt).toLocaleDateString() : '—' }}</TableCell>
                </TableRow>
                <TableRow v-if="workflows.length === 0">
                  <TableCell colspan="8" class="text-center text-gray-500 py-8">
                    <div class="flex flex-col items-center gap-2">
                      <svg class="w-12 h-12 text-gray-300" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2" />
                      </svg>
                      <p class="text-lg font-medium">No workflows found</p>
                      <p class="text-sm">{{ searchQuery || selectedStatuses.length ? 'Try adjusting your search or filters.' : 'Workflows are created automatically when you create quotes or invoices.' }}</p>
                    </div>
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
  </Layout>
</template>
