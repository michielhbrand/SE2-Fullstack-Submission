<script setup lang="ts">
import { ref, computed, watch, onMounted } from 'vue'
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter, Button } from '../ui/index'
import { templateApi, TemplateType } from '@/services/api'
import { useOrganizationStore } from '@/stores/organization'
import { toast } from 'vue-sonner'
import SearchableDropdown, { type DropdownItem } from '../SearchableDropdown.vue'
import DocumentLineItems, { type LineItem } from '../DocumentLineItems.vue'

interface Client {
  id: number
  name: string
  email: string
  cellphone: string
  address?: string
  isCompany?: boolean
  vatNumber?: string
}

interface TemplateOption {
  id: number
  name: string
  version: number
}

interface Props {
  show: boolean
  clients: Client[]
}

interface Emits {
  (e: 'close'): void
  (e: 'save', data: { clientId: number, items: LineItem[], templateId?: number, vatInclusive: boolean }): void
}

const props = defineProps<Props>()
const emit = defineEmits<Emits>()

const organizationStore = useOrganizationStore()

const selectedClientId = ref<number | null>(null)
const selectedTemplateId = ref<number | null>(null)
const vatInclusive = ref(true)
const quoteItems = ref<LineItem[]>([{ description: '', amount: 1, pricePerUnit: 0 }])
const formErrors = ref<Record<string, string>>({})
const templates = ref<TemplateOption[]>([])
const loadingTemplates = ref(false)

const fetchTemplates = async () => {
  loadingTemplates.value = true
  try {
    const orgId = organizationStore.currentOrganizationId
    if (!orgId) { templates.value = []; return }
    const templateList = await templateApi.getTemplatesByType(orgId, TemplateType.Quote)
    templates.value = (templateList || []).map((t: any) => ({
      id: t.id as number,
      name: `${t.name} v${t.version}`,
      version: t.version as number
    }))
    if (templates.value.length > 0 && !selectedTemplateId.value) {
      selectedTemplateId.value = templates.value[0]!.id
    }
  } catch (error) {
    toast.error('Failed to load templates')
    templates.value = []
  } finally {
    loadingTemplates.value = false
  }
}

const validateForm = () => {
  formErrors.value = {}
  if (!selectedClientId.value) formErrors.value.client = 'Please select a client'
  if (!selectedTemplateId.value) formErrors.value.template = 'Please select a template'
  quoteItems.value.forEach((item, index) => {
    if (!item.description.trim()) formErrors.value[`item_${index}_description`] = 'Description is required'
    if (item.amount < 1) formErrors.value[`item_${index}_amount`] = 'Amount must be at least 1'
    if (item.pricePerUnit <= 0) formErrors.value[`item_${index}_price`] = 'Price must be greater than 0'
  })
  return Object.keys(formErrors.value).length === 0
}

const handleSave = () => {
  if (!validateForm() || !selectedClientId.value) {
    toast.error('Please fix the validation errors before submitting')
    return
  }
  emit('save', {
    clientId: selectedClientId.value,
    items: quoteItems.value,
    templateId: selectedTemplateId.value ?? undefined,
    vatInclusive: vatInclusive.value
  })
}

const handleClose = () => emit('close')

const totalQuoteAmount = computed(() =>
  quoteItems.value.reduce((sum, item) => sum + (item.amount * item.pricePerUnit), 0)
)

const orgVatRatePercent = computed(() => (organizationStore.currentOrganization as any)?.vatRate ?? 15)

const vatAmount = computed(() =>
  vatInclusive.value ? 0 : totalQuoteAmount.value * (orgVatRatePercent.value / 100)
)

const totalWithVat = computed(() => totalQuoteAmount.value + vatAmount.value)

const clientItems = computed<DropdownItem[]>(() =>
  props.clients.map(c => ({
    id: c.id,
    label: `${c.name} (${c.email})`,
    primaryText: c.name,
    secondaryText: c.email,
    badge: c.isCompany ? 'Company' : undefined,
  }))
)

const templateItems = computed<DropdownItem[]>(() =>
  templates.value.map(t => ({
    id: t.id,
    label: t.name,
    primaryText: t.name,
  }))
)

const resetForm = () => {
  selectedClientId.value = null
  selectedTemplateId.value = templates.value.length > 0 ? templates.value[0]!.id : null
  vatInclusive.value = true
  quoteItems.value = [{ description: '', amount: 1, pricePerUnit: 0 }]
  formErrors.value = {}
}

watch(() => props.show, (newVal) => {
  if (newVal) { resetForm(); fetchTemplates() }
})

onMounted(fetchTemplates)
</script>

<template>
  <Dialog :open="show" @update:open="(open) => !open && handleClose()">
    <DialogContent class="sm:max-w-2xl max-h-[90vh] overflow-hidden flex flex-col" :prevent-close="true">
      <DialogHeader>
        <DialogTitle>New Quote</DialogTitle>
      </DialogHeader>

      <div class="space-y-4 overflow-y-auto flex-1 pr-2">
        <!-- Client Selection -->
        <div>
          <label class="block text-sm font-medium text-gray-700 mb-1">Client *</label>
          <SearchableDropdown
            v-model="selectedClientId"
            :items="clientItems"
            placeholder="Select a client..."
            search-placeholder="Search clients..."
            :error="formErrors.client"
            empty-text="No clients found"
          />
        </div>

        <!-- Template Selection -->
        <div>
          <label class="block text-sm font-medium text-gray-700 mb-1">Quote Template *</label>
          <SearchableDropdown
            v-model="selectedTemplateId"
            :items="templateItems"
            placeholder="Select a template..."
            search-placeholder="Search templates..."
            :loading="loadingTemplates"
            loading-text="Loading templates..."
            :error="formErrors.template"
            empty-text="No templates found"
          />
          <p v-if="!loadingTemplates && templates.length === 0" class="mt-1 text-sm text-yellow-600">
            No templates available. Please contact administrator.
          </p>
        </div>

        <!-- Quote Line Items -->
        <DocumentLineItems
          v-model="quoteItems"
          :errors="formErrors"
          label="Quote Items"
          id-prefix="quote-item"
        />

        <!-- VAT Toggle -->
        <div class="flex items-center gap-3 pt-2">
          <label class="relative inline-flex items-center cursor-pointer">
            <input type="checkbox" v-model="vatInclusive" class="sr-only peer" />
            <div class="w-11 h-6 bg-gray-200 peer-focus:outline-none peer-focus:ring-4 peer-focus:ring-blue-300 rounded-full peer peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-gray-300 after:border after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:bg-blue-600"></div>
          </label>
          <span class="text-sm font-medium text-gray-700">
            {{ vatInclusive ? 'Prices include VAT' : 'Prices exclude VAT (15% will be added)' }}
          </span>
        </div>

        <!-- Total -->
        <div class="pt-3 border-t border-gray-200">
          <div class="flex justify-between items-center">
            <span class="text-sm text-gray-600">Subtotal:</span>
            <span class="text-sm text-gray-600">R {{ totalQuoteAmount.toFixed(2) }}</span>
          </div>
          <div v-if="!vatInclusive" class="flex justify-between items-center mt-1">
            <span class="text-sm text-gray-500">VAT ({{ orgVatRatePercent }}%):</span>
            <span class="text-sm text-gray-500">R {{ vatAmount.toFixed(2) }}</span>
          </div>
          <div class="flex justify-between items-center mt-1">
            <span class="text-lg font-semibold text-gray-900">Total:</span>
            <span class="text-lg font-bold text-blue-600">R {{ totalWithVat.toFixed(2) }}</span>
          </div>
          <p class="text-xs text-gray-400 text-right mt-1">
            {{ vatInclusive ? 'All prices include VAT' : 'VAT calculated at 15%' }}
          </p>
        </div>
      </div>

      <DialogFooter>
        <Button @click="handleClose" variant="outline">Cancel</Button>
        <Button @click="handleSave" variant="default">Create Quote</Button>
      </DialogFooter>
    </DialogContent>
  </Dialog>
</template>
