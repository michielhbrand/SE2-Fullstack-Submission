<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter, Button, Label } from '../ui/index'
import { templateApi, TemplateType, quoteApi, workflowApi } from '@/services/api'
import { useOrganizationStore } from '@/stores/organization'
import { toast } from 'vue-sonner'
import SearchableDropdown, { type DropdownItem } from '../SearchableDropdown.vue'
import DocumentLineItems, { type LineItem } from '../DocumentLineItems.vue'

interface TemplateOption {
  id: number
  name: string
}

interface Props {
  show: boolean
  quoteId: number | null
  workflowId: number
}

interface Emits {
  (e: 'close'): void
  (e: 'saved'): void
}

const props = defineProps<Props>()
const emit = defineEmits<Emits>()

const organizationStore = useOrganizationStore()

const saving = ref(false)
const loadingQuote = ref(false)
const loadingTemplates = ref(false)

// Form state — populated from the existing quote on open
const clientId = ref<number>(0)
const clientName = ref<string>('')
const selectedTemplateId = ref<number | null>(null)
const vatInclusive = ref(true)
const items = ref<LineItem[]>([{ description: '', amount: 1, pricePerUnit: 0 }])
const note = ref('')
const templates = ref<TemplateOption[]>([])
const formErrors = ref<Record<string, string>>({})

const loadQuote = async () => {
  if (!props.quoteId) return
  loadingQuote.value = true
  try {
    const quote = await quoteApi.getQuote(props.quoteId)
    clientId.value = quote.clientId ?? 0
    clientName.value = quote.client?.name ?? `Client #${quote.clientId}`
    selectedTemplateId.value = quote.templateId ?? null
    vatInclusive.value = quote.vatInclusive ?? true
    items.value = (quote.items ?? []).map((item: any) => ({
      description: item.description ?? '',
      amount: item.quantity ?? 1,
      pricePerUnit: item.unitPrice ?? 0,
    }))
    if (items.value.length === 0) {
      items.value = [{ description: '', amount: 1, pricePerUnit: 0 }]
    }
  } catch (err) {
    toast.error('Failed to load quote data')
  } finally {
    loadingQuote.value = false
  }
}

const loadTemplates = async () => {
  loadingTemplates.value = true
  try {
    const orgId = organizationStore.currentOrganizationId
    if (!orgId) return
    const list = await templateApi.getTemplatesByType(orgId, TemplateType.Quote)
    templates.value = (list || []).map((t: any) => ({
      id: t.id as number,
      name: `${t.name} v${t.version}`,
    }))
  } catch {
    templates.value = []
  } finally {
    loadingTemplates.value = false
  }
}

const templateItems = computed<DropdownItem[]>(() =>
  templates.value.map(t => ({
    id: t.id,
    label: t.name,
    primaryText: t.name,
  }))
)

const totalAmount = computed(() =>
  items.value.reduce((sum, item) => sum + item.amount * item.pricePerUnit, 0)
)

const orgVatRatePercent = computed(() => (organizationStore.currentOrganization as any)?.vatRate ?? 15)

const vatAmount = computed(() =>
  vatInclusive.value ? 0 : totalAmount.value * (orgVatRatePercent.value / 100)
)

const totalWithVat = computed(() => totalAmount.value + vatAmount.value)

const validate = (): boolean => {
  formErrors.value = {}
  items.value.forEach((item, i) => {
    if (!item.description.trim()) formErrors.value[`item_${i}_description`] = 'Description is required'
    if (item.amount < 1) formErrors.value[`item_${i}_amount`] = 'Amount must be at least 1'
    if (item.pricePerUnit <= 0) formErrors.value[`item_${i}_price`] = 'Price must be greater than 0'
  })
  return Object.keys(formErrors.value).length === 0
}

const handleSave = async () => {
  if (!validate() || !props.quoteId) return
  saving.value = true
  try {
    // 1. Update the quote — triggers PDF regeneration via Kafka
    await quoteApi.updateQuote(props.quoteId, {
      clientId: clientId.value,
      notificationSent: false,
      templateId: selectedTemplateId.value ?? undefined,
      vatInclusive: vatInclusive.value,
      items: items.value.map(item => ({
        description: item.description,
        quantity: item.amount,
        unitPrice: item.pricePerUnit,
      })),
    })

    // 2. Record the QuoteModified event in the workflow
    await workflowApi.addEvent(props.workflowId, {
      eventType: 'QuoteModified',
      description: note.value.trim() || undefined,
    })

    toast.success('Quote updated. A new PDF is being generated.')
    emit('saved')
    handleClose()
  } catch (err: any) {
    const msg = err?.response?.data?.detail || err?.response?.data?.message
    toast.error(msg || 'Failed to save quote changes')
  } finally {
    saving.value = false
  }
}

const handleClose = () => {
  note.value = ''
  formErrors.value = {}
  emit('close')
}

watch(
  () => props.show,
  async (open) => {
    if (open) {
      await Promise.all([loadQuote(), loadTemplates()])
    }
  }
)
</script>

<template>
  <Dialog :open="show" @update:open="(open) => !open && handleClose()">
    <DialogContent class="sm:max-w-2xl max-h-[90vh] overflow-hidden flex flex-col" :prevent-close="true">
      <DialogHeader>
        <DialogTitle>Edit Quote (Revision)</DialogTitle>
      </DialogHeader>

      <div v-if="loadingQuote" class="flex justify-center py-12">
        <svg class="animate-spin h-8 w-8 text-gray-600" fill="none" viewBox="0 0 24 24">
          <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"/>
          <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v8H4z"/>
        </svg>
      </div>

      <div v-else class="space-y-4 overflow-y-auto flex-1 pr-2">

        <!-- Client (read-only) -->
        <div>
          <label class="block text-sm font-medium text-gray-700 mb-1">Client</label>
          <div class="px-3 py-2 bg-gray-100 rounded-lg text-sm text-gray-700 border border-gray-200">
            {{ clientName }}
          </div>
          <p class="mt-1 text-xs text-gray-400">Client cannot be changed on a revised quote</p>
        </div>

        <!-- Template -->
        <div>
          <label class="block text-sm font-medium text-gray-700 mb-1">Quote Template</label>
          <SearchableDropdown
            v-model="selectedTemplateId"
            :items="templateItems"
            placeholder="Select a template..."
            search-placeholder="Search templates..."
            :loading="loadingTemplates"
            loading-text="Loading templates..."
            empty-text="No templates found"
          />
        </div>

        <!-- Line Items -->
        <DocumentLineItems
          v-model="items"
          :errors="formErrors"
          label="Quote Items"
          id-prefix="edit-quote-item"
        />

        <!-- VAT Toggle -->
        <div class="flex items-center gap-3 pt-2">
          <label class="relative inline-flex items-center cursor-pointer">
            <input type="checkbox" v-model="vatInclusive" class="sr-only peer" />
            <div class="w-11 h-6 bg-gray-200 peer-focus:outline-none peer-focus:ring-4 peer-focus:ring-blue-300 rounded-full peer peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-gray-300 after:border after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:bg-blue-600"></div>
          </label>
          <span class="text-sm font-medium text-gray-700">
            {{ vatInclusive ? 'Prices include VAT' : 'Prices exclude VAT (will be added)' }}
          </span>
        </div>

        <!-- Total -->
        <div class="pt-3 border-t border-gray-200">
          <div class="flex justify-between items-center">
            <span class="text-sm text-gray-600">Subtotal:</span>
            <span class="text-sm text-gray-600">R {{ totalAmount.toFixed(2) }}</span>
          </div>
          <div v-if="!vatInclusive" class="flex justify-between items-center mt-1">
            <span class="text-sm text-gray-500">VAT ({{ orgVatRatePercent }}%):</span>
            <span class="text-sm text-gray-500">R {{ vatAmount.toFixed(2) }}</span>
          </div>
          <div class="flex justify-between items-center mt-1">
            <span class="text-lg font-semibold text-gray-900">Total:</span>
            <span class="text-lg font-bold text-blue-600">R {{ totalWithVat.toFixed(2) }}</span>
          </div>
        </div>

        <!-- Note for team (optional) -->
        <div>
          <Label for="edit-quote-note">Note (optional)</Label>
          <textarea
            id="edit-quote-note"
            v-model="note"
            placeholder="Briefly describe what was changed (e.g. 'Reduced unit price as per client feedback')"
            rows="2"
            class="mt-1 w-full rounded-md border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-gray-900 resize-none"
          />
          <p class="mt-1 text-xs text-gray-400">Recorded in the workflow timeline</p>
        </div>
      </div>

      <DialogFooter>
        <Button @click="handleClose" variant="outline" :disabled="saving">Cancel</Button>
        <Button @click="handleSave" variant="default" :disabled="saving || loadingQuote">
          <svg v-if="saving" class="animate-spin h-4 w-4 mr-2" fill="none" viewBox="0 0 24 24">
            <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"/>
            <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v8H4z"/>
          </svg>
          {{ saving ? 'Saving...' : 'Save Changes' }}
        </Button>
      </DialogFooter>
    </DialogContent>
  </Dialog>
</template>
