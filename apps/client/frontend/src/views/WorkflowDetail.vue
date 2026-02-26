<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { workflowApi, quoteApi, invoiceApi } from '../services/api'
import { getStatusLabel, getStatusColor, getEventLabel, getEventPdfType } from '../utils/workflow'
import { Button, Spinner, Skeleton, Badge } from '../components/ui/index'
import Layout from '../components/Layout.vue'
import CancelWorkflowModal from '../components/modals/CancelWorkflowModal.vue'
import WorkflowEventModal from '../components/modals/WorkflowEventModal.vue'
import ConvertToInvoiceModal from '../components/modals/ConvertToInvoiceModal.vue'
import EditQuoteModal from '../components/modals/EditQuoteModal.vue'
import { toast } from 'vue-sonner'

const route = useRoute()
const router = useRouter()

const loading = ref(true)
const refreshing = ref(false)
const workflow = ref<any>(null)
const actionLoading = ref(false)

// Event action dialog state
const showEventDialog = ref(false)
const selectedEventType = ref('')

// Cancel confirmation dialog state
const showCancelDialog = ref(false)

// Convert to invoice dialog state
const showConvertToInvoiceDialog = ref(false)

// Edit quote dialog state (for rejected workflow revision)
const showEditQuoteDialog = ref(false)

const workflowId = computed(() => Number(route.params.id))

onMounted(async () => {
  await fetchWorkflow()
})

const fetchWorkflow = async () => {
  loading.value = true
  try {
    workflow.value = await workflowApi.getWorkflow(workflowId.value)
  } catch (error: any) {
    console.error('Failed to fetch workflow:', error)
    if (error.response?.status === 404) {
      toast.error('Workflow not found')
      router.push('/workflows')
    } else {
      toast.error('Failed to load workflow')
    }
  } finally {
    loading.value = false
  }
}

const refreshWorkflow = async () => {
  refreshing.value = true
  try {
    workflow.value = await workflowApi.getWorkflow(workflowId.value)
  } catch (error: any) {
    console.error('Failed to refresh workflow:', error)
    toast.error('Failed to refresh workflow')
  } finally {
    refreshing.value = false
  }
}

// Determine which actions are available based on current status
const availableActions = computed(() => {
  if (!workflow.value) return []
  const status = workflow.value.status
  const type = workflow.value.type

  const actions: Array<{ eventType: string; label: string; icon: string; color: string; requiresDescription?: boolean }> = []

  const transitionMap: Record<string, Array<{ eventType: string; label: string; icon: string; color: string; requiresDescription?: boolean }>> = {
    Draft: [
      { eventType: 'SentForApproval', label: 'Send for Approval', icon: 'mdi-send', color: 'amber' },
      { eventType: 'SentForPayment', label: 'Send for Payment', icon: 'mdi-cash-fast', color: 'purple' },
    ],
    PendingApproval: [
      { eventType: 'ResentForApproval', label: 'Resend for Approval', icon: 'mdi-send-clock', color: 'amber' },
      { eventType: 'Approved', label: 'Approve', icon: 'mdi-check-circle', color: 'green' },
      { eventType: 'Rejected', label: 'Reject', icon: 'mdi-close-circle', color: 'red', requiresDescription: true },
    ],
    Rejected: [],
    Approved: [
      { eventType: 'ConvertedToInvoice', label: 'Convert to Invoice', icon: 'mdi-file-replace', color: 'blue' },
    ],
    InvoiceCreated: [
      { eventType: 'SentForPayment', label: 'Send for Payment', icon: 'mdi-cash-fast', color: 'purple' },
    ],
    SentForPayment: [
      { eventType: 'ResentForPayment', label: 'Resend for Payment', icon: 'mdi-cash-fast', color: 'purple' },
      { eventType: 'MarkedAsPaid', label: 'Mark as Paid', icon: 'mdi-cash-check', color: 'emerald' },
    ],
  }

  // Filter Draft actions based on type
  if (status === 'Draft') {
    if (type === 'QuoteFirst') {
      actions.push({ eventType: 'SentForApproval', label: 'Send for Approval', icon: 'mdi-send', color: 'amber' })
    } else {
      actions.push({ eventType: 'SentForPayment', label: 'Send for Payment', icon: 'mdi-cash-fast', color: 'purple' })
    }
  } else if (transitionMap[status]) {
    actions.push(...transitionMap[status])
  }

  // For Rejected status: once the quote has been edited, allow resending for approval
  if (status === 'Rejected' && hasModifiedAfterRejection.value) {
    actions.push({ eventType: 'ResentForApproval', label: 'Resend for Approval', icon: 'mdi-send', color: 'amber' })
  }

  return actions
})

const isTerminalStatus = computed(() => {
  if (!workflow.value) return false
  return ['Paid', 'Cancelled', 'Terminated'].includes(workflow.value.status)
})

const canCancel = computed(() => {
  if (!workflow.value) return false
  return !isTerminalStatus.value
})

// True once the quote has been edited (QuoteModified event) after the most recent rejection
const hasModifiedAfterRejection = computed(() => {
  if (!workflow.value?.events?.length) return false
  const lastRejectedAt = workflow.value.events
    .filter((e: any) => e.eventType === 'Rejected')
    .map((e: any) => new Date(e.occurredAt).getTime())
    .sort((a: number, b: number) => b - a)[0]
  if (!lastRejectedAt) return false
  return workflow.value.events.some(
    (e: any) => e.eventType === 'QuoteModified' && new Date(e.occurredAt).getTime() > lastRejectedAt
  )
})

// Execute a workflow action
const executeAction = async (eventType: string, requiresDescription = false) => {
  // Intercept ConvertedToInvoice to show pay-by-days dialog
  if (eventType === 'ConvertedToInvoice') {
    showConvertToInvoiceDialog.value = true
    return
  }

  if (requiresDescription) {
    selectedEventType.value = eventType
    showEventDialog.value = true
    return
  }

  actionLoading.value = true
  try {
    workflow.value = await workflowApi.addEvent(workflowId.value, { eventType })
    toast.success(`Action "${getEventLabel(eventType)}" completed`)
  } catch (error: any) {
    console.error('Failed to execute action:', error)
    const message = error.response?.data?.message || error.response?.data?.Message || 'Action failed'
    toast.error(message)
  } finally {
    actionLoading.value = false
  }
}

const submitEventWithDescription = async (description: string) => {
  actionLoading.value = true
  showEventDialog.value = false
  try {
    workflow.value = await workflowApi.addEvent(workflowId.value, {
      eventType: selectedEventType.value,
      description: description || undefined,
    })
    toast.success(`Action "${getEventLabel(selectedEventType.value)}" completed`)
  } catch (error: any) {
    console.error('Failed to execute action:', error)
    const message = error.response?.data?.message || error.response?.data?.Message || 'Action failed'
    toast.error(message)
  } finally {
    actionLoading.value = false
  }
}

const confirmConvertToInvoice = async (payByDays: number) => {
  actionLoading.value = true
  showConvertToInvoiceDialog.value = false
  try {
    workflow.value = await workflowApi.addEvent(workflowId.value, {
      eventType: 'ConvertedToInvoice',
      payByDays,
    })
    toast.success('Quote converted to invoice successfully')
  } catch (error: any) {
    console.error('Failed to convert to invoice:', error)
    const message = error.response?.data?.message || error.response?.data?.Message || 'Failed to convert to invoice'
    toast.error(message)
  } finally {
    actionLoading.value = false
  }
}

const cancelWorkflow = () => {
  showCancelDialog.value = true
}

const onQuoteSaved = async () => {
  // Refresh the workflow so the timeline and actions reflect the new QuoteModified event
  await refreshWorkflow()
}

const confirmCancelWorkflow = async () => {
  showCancelDialog.value = false
  actionLoading.value = true
  try {
    workflow.value = await workflowApi.cancelWorkflow(workflowId.value)
    toast.success('Workflow cancelled')
  } catch (error: any) {
    console.error('Failed to cancel workflow:', error)
    toast.error(error.response?.data?.message || 'Failed to cancel workflow')
  } finally {
    actionLoading.value = false
  }
}


// Status/event helpers are imported from utils/workflow.ts

const getTypeLabel = (type: string): string => {
  return type === 'QuoteFirst' ? 'Quote → Invoice' : 'Invoice Only'
}

const getEventIcon = (eventType: string): string => {
  const icons: Record<string, string> = {
    QuoteCreated: 'mdi-file-document-plus',
    SentForApproval: 'mdi-send',
    Approved: 'mdi-check-circle',
    Rejected: 'mdi-close-circle',
    QuoteModified: 'mdi-pencil',
    ResentForApproval: 'mdi-send-clock',
    ResentForPayment: 'mdi-cash-fast',
    ConvertedToInvoice: 'mdi-file-replace',
    InvoiceCreated: 'mdi-file-document-plus',
    SentForPayment: 'mdi-cash-fast',
    MarkedAsPaid: 'mdi-cash-check',
    Cancelled: 'mdi-cancel',
    Terminated: 'mdi-stop-circle',
  }
  return icons[eventType] || 'mdi-circle'
}

const getEventDotColor = (eventType: string): string => {
  const colors: Record<string, string> = {
    QuoteCreated: 'blue',
    SentForApproval: 'amber',
    Approved: 'green',
    Rejected: 'red',
    QuoteModified: 'blue',
    ResentForApproval: 'amber',
    ResentForPayment: 'purple',
    ConvertedToInvoice: 'indigo',
    InvoiceCreated: 'blue',
    SentForPayment: 'purple',
    MarkedAsPaid: 'green',
    Cancelled: 'orange',
    Terminated: 'red',
  }
  return colors[eventType] || 'grey'
}

const formatDateTime = (dateStr: string): string => {
  const date = new Date(dateStr)
  return date.toLocaleString(undefined, {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  })
}

// PDF links in timeline (getEventPdfType imported from utils/workflow.ts)
const loadingPdf = ref<string | null>(null)

const openPdf = async (type: 'quote' | 'invoice', id: number) => {
  const key = `${type}-${id}`
  if (loadingPdf.value === key) return
  loadingPdf.value = key
  try {
    const response = type === 'quote'
      ? await quoteApi.getPdfUrl(id)
      : await invoiceApi.getPdfUrl(id)
    if (response.url) window.open(response.url, '_blank')
    else toast.error('PDF not available yet')
  } catch (error: any) {
    if (error.response?.status === 404) toast.error('PDF not yet generated for this document')
    else toast.error('Failed to load PDF')
  } finally {
    loadingPdf.value = null
  }
}

const formatRelativeTime = (dateStr: string): string => {
  const date = new Date(dateStr)
  const now = new Date()
  const diffMs = now.getTime() - date.getTime()
  const diffMins = Math.floor(diffMs / 60000)
  const diffHours = Math.floor(diffMs / 3600000)
  const diffDays = Math.floor(diffMs / 86400000)

  if (diffMins < 1) return 'Just now'
  if (diffMins < 60) return `${diffMins}m ago`
  if (diffHours < 24) return `${diffHours}h ago`
  if (diffDays < 7) return `${diffDays}d ago`
  return date.toLocaleDateString()
}
</script>

<template>
  <Layout>
    <div class="p-6 lg:p-8">
      <div class="max-w-5xl mx-auto">
        <!-- Back button -->
        <div class="mb-6">
          <Button
            variant="ghost"
            size="sm"
            @click="router.push('/workflows')"
            class="text-gray-600 hover:text-gray-900"
          >
            <svg class="w-4 h-4 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7" />
            </svg>
            Back to Workflows
          </Button>
        </div>

        <!-- Loading -->
        <div v-if="loading" class="space-y-4">
          <Skeleton class="h-32 w-full" />
          <Skeleton class="h-64 w-full" />
        </div>

        <template v-else-if="workflow">
          <!-- Header Card -->
          <div class="bg-white rounded-lg shadow mb-6">
            <div class="p-6">
              <div class="flex flex-col md:flex-row md:items-start md:justify-between gap-4">
                <div>
                  <div class="flex items-center gap-3 mb-2">
                    <h2 class="text-2xl font-bold text-gray-900">Workflow #{{ workflow.id }}</h2>
                    <span
                      class="inline-flex items-center rounded-full px-3 py-1 text-sm font-semibold"
                      :class="getStatusColor(workflow.status)"
                    >
                      {{ getStatusLabel(workflow.status) }}
                    </span>
                  </div>
                  <p class="text-gray-600">
                    {{ getTypeLabel(workflow.type) }}
                    <span class="mx-2">·</span>
                    Client: <span class="font-medium">{{ workflow.clientName || `#${workflow.clientId}` }}</span>
                    <template v-if="workflow.clientEmail">
                      <span class="mx-2">·</span>
                      <span class="text-gray-500">{{ workflow.clientEmail }}</span>
                    </template>
                  </p>
                </div>

                <!-- Linked documents -->
                <div class="flex flex-wrap gap-3">
                  <div v-if="workflow.quoteId" class="flex items-center gap-2 bg-indigo-50 rounded-lg px-3 py-2">
                    <svg class="w-4 h-4 text-indigo-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
                    </svg>
                    <span class="text-sm font-medium text-indigo-700">Quote #{{ workflow.quoteId }}</span>
                  </div>
                  <div v-if="workflow.invoiceId" class="flex items-center gap-2 bg-blue-50 rounded-lg px-3 py-2">
                    <svg class="w-4 h-4 text-blue-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
                    </svg>
                    <span class="text-sm font-medium text-blue-700">Invoice #{{ workflow.invoiceId }}</span>
                  </div>
                </div>
              </div>

              <!-- Metadata -->
              <div class="mt-4 flex flex-wrap gap-4 text-sm text-gray-500">
                <span>Created: {{ formatDateTime(workflow.createdAt) }}</span>
                <span v-if="workflow.updatedAt">Updated: {{ formatDateTime(workflow.updatedAt) }}</span>
                <span v-if="workflow.createdBy">By: {{ workflow.createdBy }}</span>
              </div>
            </div>
          </div>

          <!-- Actions Card -->
          <div v-if="availableActions.length > 0 || canCancel || workflow.status === 'Rejected'" class="bg-white rounded-lg shadow mb-6">
            <div class="p-6">
              <div class="flex items-center justify-between mb-4">
                <h3 class="text-lg font-semibold text-gray-900">Available Actions</h3>
                <v-btn
                  icon
                  size="small"
                  variant="text"
                  :loading="refreshing"
                  @click="refreshWorkflow"
                  title="Refresh workflow"
                >
                  <v-icon size="20">mdi-refresh</v-icon>
                </v-btn>
              </div>
              <div class="flex flex-wrap gap-3">

                <!-- Edit Quote — shown whenever workflow is Rejected (with or without prior modification) -->
                <Button
                  v-if="workflow.status === 'Rejected'"
                  variant="outline"
                  class="border-blue-300 text-blue-700 hover:bg-blue-50"
                  :disabled="actionLoading"
                  @click="showEditQuoteDialog = true"
                >
                  <svg class="w-4 h-4 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z"/>
                  </svg>
                  Edit Quote
                </Button>

                <!-- Workflow progression actions (ResentForApproval appears here once quote is modified) -->
                <v-btn
                  v-for="action in availableActions"
                  :key="action.eventType"
                  :color="action.color"
                  :loading="actionLoading"
                  variant="flat"
                  @click="executeAction(action.eventType, action.requiresDescription)"
                >
                  <v-icon start>{{ action.icon }}</v-icon>
                  {{ action.label }}
                </v-btn>

                <v-divider v-if="(availableActions.length > 0 || workflow.status === 'Rejected') && canCancel" vertical class="mx-2" />

                <!-- Cancel -->
                <v-btn
                  v-if="canCancel"
                  color="orange"
                  variant="outlined"
                  :loading="actionLoading"
                  @click="cancelWorkflow"
                >
                  <v-icon start>mdi-cancel</v-icon>
                  Cancel
                </v-btn>
              </div>

              <!-- Hint when rejected but not yet edited -->
              <p
                v-if="workflow.status === 'Rejected' && !hasModifiedAfterRejection"
                class="mt-3 text-sm text-amber-700 bg-amber-50 rounded-lg px-3 py-2"
              >
                Edit the quote to address the client's feedback, then "Resend for Approval" will become available.
              </p>
            </div>
          </div>

          <!-- Terminal status banner -->
          <div v-if="isTerminalStatus" class="mb-6 rounded-lg border px-4 py-3" :class="{
            'bg-emerald-50 border-emerald-200 text-emerald-800': workflow.status === 'Paid',
            'bg-orange-50 border-orange-200 text-orange-800': workflow.status === 'Cancelled',
            'bg-red-50 border-red-200 text-red-800': workflow.status === 'Terminated',
          }">
            <div class="flex items-center gap-2">
              <v-icon size="20">
                {{ workflow.status === 'Paid' ? 'mdi-check-circle' : workflow.status === 'Cancelled' ? 'mdi-cancel' : 'mdi-stop-circle' }}
              </v-icon>
              <span class="font-medium">
                This workflow has been {{ workflow.status.toLowerCase() }}. No further actions are available.
              </span>
            </div>
          </div>

          <!-- Timeline Card -->
          <div class="bg-white rounded-lg shadow">
            <div class="p-6">
              <h3 class="text-lg font-semibold text-gray-900 mb-6">Timeline</h3>

              <div v-if="workflow.events && workflow.events.length > 0">
                <v-timeline side="end" density="comfortable">
                  <v-timeline-item
                    v-for="event in workflow.events"
                    :key="event.id"
                    :dot-color="getEventDotColor(event.eventType)"
                    :icon="getEventIcon(event.eventType)"
                    size="small"
                  >
                    <template v-slot:opposite>
                      <div class="text-sm text-gray-500">
                        {{ formatRelativeTime(event.occurredAt) }}
                      </div>
                    </template>

                    <div class="bg-gray-50 rounded-lg p-4 ml-2">
                      <div class="flex items-center gap-2 mb-1">
                        <span class="font-semibold text-gray-900">{{ getEventLabel(event.eventType) }}</span>
                      </div>
                      <div class="text-sm text-gray-500">
                        {{ formatDateTime(event.occurredAt) }}
                        <span v-if="event.performedBy" class="ml-2">
                          by <span class="font-medium">{{ event.performedBy }}</span>
                        </span>
                      </div>
                      <p v-if="event.description" class="mt-2 text-sm text-gray-700 bg-white rounded p-2 border border-gray-200">
                        {{ event.description }}
                      </p>

                      <!-- PDF link for quote-related events -->
                      <button
                        v-if="getEventPdfType(event.eventType) === 'quote' && workflow.quoteId"
                        @click="openPdf('quote', workflow.quoteId)"
                        :disabled="loadingPdf === `quote-${workflow.quoteId}`"
                        class="mt-2 inline-flex items-center gap-1.5 text-xs font-medium text-indigo-600 hover:text-indigo-800 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
                      >
                        <svg v-if="loadingPdf === `quote-${workflow.quoteId}`" class="animate-spin w-3.5 h-3.5" fill="none" viewBox="0 0 24 24">
                          <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"/>
                          <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v8H4z"/>
                        </svg>
                        <svg v-else class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M7 21h10a2 2 0 002-2V9.414a1 1 0 00-.293-.707l-5.414-5.414A1 1 0 0012.586 3H7a2 2 0 00-2 2v14a2 2 0 002 2z"/>
                        </svg>
                        View Quote PDF
                      </button>

                      <!-- PDF link for invoice-related events -->
                      <button
                        v-if="getEventPdfType(event.eventType) === 'invoice' && workflow.invoiceId"
                        @click="openPdf('invoice', workflow.invoiceId)"
                        :disabled="loadingPdf === `invoice-${workflow.invoiceId}`"
                        class="mt-2 inline-flex items-center gap-1.5 text-xs font-medium text-blue-600 hover:text-blue-800 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
                      >
                        <svg v-if="loadingPdf === `invoice-${workflow.invoiceId}`" class="animate-spin w-3.5 h-3.5" fill="none" viewBox="0 0 24 24">
                          <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"/>
                          <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v8H4z"/>
                        </svg>
                        <svg v-else class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M7 21h10a2 2 0 002-2V9.414a1 1 0 00-.293-.707l-5.414-5.414A1 1 0 0012.586 3H7a2 2 0 00-2 2v14a2 2 0 002 2z"/>
                        </svg>
                        View Invoice PDF
                      </button>
                    </div>
                  </v-timeline-item>
                </v-timeline>
              </div>

              <div v-else class="text-center text-gray-500 py-8">
                <v-icon size="48" color="grey-lighten-2">mdi-timeline-clock</v-icon>
                <p class="mt-2">No events recorded yet</p>
              </div>
            </div>
          </div>
        </template>

        <!-- Not found -->
        <div v-else class="text-center py-12">
          <p class="text-gray-500 text-lg">Workflow not found</p>
          <Button variant="default" class="mt-4" @click="router.push('/workflows')">
            Back to Workflows
          </Button>
        </div>
      </div>
    </div>

    <!-- Event description dialog -->
    <WorkflowEventModal
      :show="showEventDialog"
      :event-label="getEventLabel(selectedEventType)"
      :loading="actionLoading"
      @close="showEventDialog = false"
      @confirm="submitEventWithDescription"
    />

    <!-- Cancel workflow confirmation dialog -->
    <CancelWorkflowModal
      :show="showCancelDialog"
      :loading="actionLoading"
      @close="showCancelDialog = false"
      @confirm="confirmCancelWorkflow"
    />

    <!-- Convert to invoice dialog -->
    <ConvertToInvoiceModal
      :show="showConvertToInvoiceDialog"
      :loading="actionLoading"
      @close="showConvertToInvoiceDialog = false"
      @confirm="confirmConvertToInvoice"
    />

    <!-- Edit quote dialog (rejection revision flow) -->
    <EditQuoteModal
      :show="showEditQuoteDialog"
      :quote-id="workflow?.quoteId ?? null"
      :workflow-id="workflowId"
      @close="showEditQuoteDialog = false"
      @saved="onQuoteSaved"
    />
  </Layout>
</template>
