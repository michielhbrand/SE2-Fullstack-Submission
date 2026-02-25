<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { useRouter } from 'vue-router'
import { workflowApi } from '../services/api'
import { useOrganizationStore } from '../stores/organization'
import { useOrganizationContext } from '../composables/useOrganizationContext'
import { Button, Skeleton, Table, TableHeader, TableBody, TableHead, TableRow, TableCell, Pagination, PaginationContent, PaginationItem, PaginationPrevious, PaginationNext } from '../components/ui/index'
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

onMounted(async () => {
  await fetchWorkflows()
})


const fetchWorkflows = async () => {
  loading.value = true
  try {
    const orgId = await ensureOrganizationContext()
    if (!orgId) {
      toast.error('No organization selected')
      loading.value = false
      return
    }
    const response = await workflowApi.getWorkflows(orgId, currentPage.value, pageSize.value)
    workflows.value = response.data || []
    totalPages.value = response.pagination?.totalPages || 0
    totalCount.value = response.pagination?.totalCount || 0
  } catch (error) {
    console.error('Failed to fetch workflows:', error)
    toast.error('Failed to fetch workflows')
  } finally {
    loading.value = false
  }
}

const goToPage = async (page: number) => {
  currentPage.value = page
  await fetchWorkflows()
}

const paginationPages = computed(() => {
  const pages: number[] = []
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

const viewWorkflow = (id: number) => {
  router.push(`/workflows/${id}`)
}

// Status badge color mapping
const getStatusColor = (status: string): string => {
  const colors: Record<string, string> = {
    Draft: 'bg-gray-100 text-gray-800',
    PendingApproval: 'bg-yellow-100 text-yellow-800',
    Approved: 'bg-green-100 text-green-800',
    Rejected: 'bg-red-100 text-red-800',
    InvoiceCreated: 'bg-blue-100 text-blue-800',
    SentForPayment: 'bg-purple-100 text-purple-800',
    Paid: 'bg-emerald-100 text-emerald-800',
    Cancelled: 'bg-orange-100 text-orange-800',
    Terminated: 'bg-red-200 text-red-900',
  }
  return colors[status] || 'bg-gray-100 text-gray-800'
}

// Human-readable status labels
const getStatusLabel = (status: string): string => {
  const labels: Record<string, string> = {
    Draft: 'Draft',
    PendingApproval: 'Pending Approval',
    Approved: 'Approved',
    Rejected: 'Rejected',
    InvoiceCreated: 'Invoice Created',
    SentForPayment: 'Sent for Payment',
    Paid: 'Paid',
    Cancelled: 'Cancelled',
    Terminated: 'Terminated',
  }
  return labels[status] || status
}

// Human-readable type labels
const getTypeLabel = (type: string): string => {
  return type === 'QuoteFirst' ? 'Quote → Invoice' : 'Invoice Only'
}

const getTypeBadgeClass = (type: string): string => {
  return type === 'QuoteFirst'
    ? 'bg-indigo-100 text-indigo-800'
    : 'bg-cyan-100 text-cyan-800'
}
</script>

<template>
  <Layout>
    <div class="p-6 lg:p-8">
      <div class="max-w-7xl mx-auto">
        <!-- Header -->
        <div class="mb-8">
          <h2 class="text-3xl font-bold text-gray-900">Workflows</h2>
          <p class="mt-2 text-gray-600">Track the lifecycle of your quotes and invoices</p>
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
                  <TableHead>Actions</TableHead>
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
                    <span
                      class="inline-flex items-center rounded-md px-2 py-1 text-xs font-medium"
                      :class="getTypeBadgeClass(workflow.type)"
                    >
                      {{ getTypeLabel(workflow.type) }}
                    </span>
                  </TableCell>
                  <TableCell class="font-medium">{{ workflow.clientName || `Client #${workflow.clientId}` }}</TableCell>
                  <TableCell>
                    <span
                      class="inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-semibold"
                      :class="getStatusColor(workflow.status)"
                    >
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
                  <TableCell class="text-sm text-gray-600">
                    {{ new Date(workflow.createdAt).toLocaleDateString() }}
                  </TableCell>
                  <TableCell class="text-sm text-gray-600">
                    {{ workflow.updatedAt ? new Date(workflow.updatedAt).toLocaleDateString() : '—' }}
                  </TableCell>
                  <TableCell>
                    <Button
                      variant="ghost"
                      size="sm"
                      @click.stop="viewWorkflow(workflow.id)"
                      class="text-blue-600 hover:text-blue-800"
                    >
                      <svg class="w-4 h-4 mr-1" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" />
                      </svg>
                      View
                    </Button>
                  </TableCell>
                </TableRow>
                <TableRow v-if="workflows.length === 0">
                  <TableCell colspan="9" class="text-center text-gray-500 py-8">
                    <div class="flex flex-col items-center gap-2">
                      <svg class="w-12 h-12 text-gray-300" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2" />
                      </svg>
                      <p class="text-lg font-medium">No workflows found</p>
                      <p class="text-sm">Workflows are created automatically when you create quotes or invoices.</p>
                    </div>
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
