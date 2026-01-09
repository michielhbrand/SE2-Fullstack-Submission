<script setup lang="ts">
import { ref, computed, watch, onMounted, onUnmounted } from 'vue'
import { Button } from '../ui/index'
import apiClient from '@/services/api'

interface QuoteItem {
  description: string
  amount: number
  pricePerUnit: number
}

interface Client {
  id: number
  name: string
  surname: string
  email: string
  cellphone: string
  address?: string
  company?: string
}

interface Props {
  show: boolean
  clients: Client[]
}

interface Emits {
  (e: 'close'): void
  (e: 'save', data: { clientId: number, items: QuoteItem[], templateId?: string }): void
}

const props = defineProps<Props>()
const emit = defineEmits<Emits>()

const selectedClientId = ref<number | null>(null)
const selectedTemplateId = ref<string>('')
const quoteItems = ref<QuoteItem[]>([{ description: '', amount: 1, pricePerUnit: 0 }])
const formErrors = ref<Record<string, string>>({})
const templates = ref<string[]>([])
const loadingTemplates = ref(false)
const templateSearchQuery = ref('')
const isTemplateDropdownOpen = ref(false)
const templateDropdownRef = ref<HTMLDivElement | null>(null)
const templateSearchInputRef = ref<HTMLInputElement | null>(null)

const clientSearchQuery = ref('')
const isClientDropdownOpen = ref(false)
const clientDropdownRef = ref<HTMLDivElement | null>(null)
const clientSearchInputRef = ref<HTMLInputElement | null>(null)

const fetchTemplates = async () => {
  loadingTemplates.value = true
  try {
    const response = await apiClient.get('/api/quote/templates')
    templates.value = response.data || []
    // Set default template if available
    if (templates.value.length > 0 && !selectedTemplateId.value) {
      selectedTemplateId.value = templates.value[0] || ''
    }
  } catch (error) {
    console.error('Error fetching templates:', error)
    templates.value = []
  } finally {
    loadingTemplates.value = false
  }
}

const addQuoteItem = () => {
  quoteItems.value.push({ description: '', amount: 1, pricePerUnit: 0 })
}

const removeQuoteItem = (index: number) => {
  if (quoteItems.value.length > 1) {
    quoteItems.value.splice(index, 1)
  }
}

const validateForm = () => {
  formErrors.value = {}
  
  if (!selectedClientId.value) {
    formErrors.value.client = 'Please select a client'
  }

  if (!selectedTemplateId.value) {
    formErrors.value.template = 'Please select a template'
  }
  
  quoteItems.value.forEach((item, index) => {
    if (!item.description.trim()) {
      formErrors.value[`item_${index}_description`] = 'Description is required'
    }
    if (item.amount < 1) {
      formErrors.value[`item_${index}_amount`] = 'Amount must be at least 1'
    }
    if (item.pricePerUnit <= 0) {
      formErrors.value[`item_${index}_price`] = 'Price must be greater than 0'
    }
  })
  
  return Object.keys(formErrors.value).length === 0
}

const handleSave = () => {
  if (!validateForm() || !selectedClientId.value) {
    return
  }
  
  emit('save', {
    clientId: selectedClientId.value,
    items: quoteItems.value,
    templateId: selectedTemplateId.value || undefined
  })
}

const handleClose = () => {
  emit('close')
}

const totalQuoteAmount = computed(() => {
  return quoteItems.value.reduce((sum, item) => sum + (item.amount * item.pricePerUnit), 0)
})

const filteredClients = computed(() => {
  if (!clientSearchQuery.value) {
    return props.clients
  }
  const query = clientSearchQuery.value.toLowerCase()
  return props.clients.filter(client =>
    client.name.toLowerCase().includes(query) ||
    client.surname.toLowerCase().includes(query) ||
    client.email.toLowerCase().includes(query) ||
    (client.company && client.company.toLowerCase().includes(query))
  )
})

const filteredTemplates = computed(() => {
  if (!templateSearchQuery.value) {
    return templates.value
  }
  return templates.value.filter(template =>
    template.toLowerCase().includes(templateSearchQuery.value.toLowerCase())
  )
})

const toggleClientDropdown = () => {
  isClientDropdownOpen.value = !isClientDropdownOpen.value
  if (isClientDropdownOpen.value) {
    // Focus search input when dropdown opens
    setTimeout(() => {
      clientSearchInputRef.value?.focus()
    }, 50)
  }
}

const selectClient = (clientId: number) => {
  selectedClientId.value = clientId
  isClientDropdownOpen.value = false
  clientSearchQuery.value = ''
}

const toggleTemplateDropdown = () => {
  isTemplateDropdownOpen.value = !isTemplateDropdownOpen.value
  if (isTemplateDropdownOpen.value) {
    // Focus search input when dropdown opens
    setTimeout(() => {
      templateSearchInputRef.value?.focus()
    }, 50)
  }
}

const selectTemplate = (template: string) => {
  selectedTemplateId.value = template
  isTemplateDropdownOpen.value = false
  templateSearchQuery.value = ''
}

const handleClickOutside = (event: MouseEvent) => {
  if (templateDropdownRef.value && !templateDropdownRef.value.contains(event.target as Node)) {
    isTemplateDropdownOpen.value = false
  }
  if (clientDropdownRef.value && !clientDropdownRef.value.contains(event.target as Node)) {
    isClientDropdownOpen.value = false
  }
}

// Reset form when modal opens
const resetForm = () => {
  selectedClientId.value = null
  selectedTemplateId.value = templates.value.length > 0 ? (templates.value[0] || '') : ''
  quoteItems.value = [{ description: '', amount: 1, pricePerUnit: 0 }]
  formErrors.value = {}
  templateSearchQuery.value = ''
  isTemplateDropdownOpen.value = false
  clientSearchQuery.value = ''
  isClientDropdownOpen.value = false
}

const getSelectedClientDisplay = computed(() => {
  if (!selectedClientId.value) return 'Select a client...'
  const client = props.clients.find(c => c.id === selectedClientId.value)
  if (!client) return 'Select a client...'
  return `${client.name} ${client.surname} (${client.email})`
})

// Watch for show prop changes to reset form
watch(() => props.show, (newVal) => {
  if (newVal) {
    resetForm()
    fetchTemplates()
  }
})

onMounted(() => {
  fetchTemplates()
  document.addEventListener('click', handleClickOutside)
})

onUnmounted(() => {
  document.removeEventListener('click', handleClickOutside)
})
</script>

<template>
  <div
    v-if="show"
    class="fixed inset-0 z-[60] overflow-y-auto"
  >
    <div class="flex items-center justify-center min-h-screen px-4 pt-4 pb-20 text-center sm:block sm:p-0">
      <div class="fixed inset-0 transition-opacity bg-gray-500/50" @click="handleClose"></div>
      
      <!-- Spacer for centering -->
      <span class="hidden sm:inline-block sm:align-middle sm:h-screen" aria-hidden="true">&#8203;</span>

      <div class="relative inline-block align-bottom bg-white rounded-lg text-left overflow-hidden shadow-xl transform transition-all sm:my-8 sm:align-middle sm:max-w-2xl sm:w-full">
        <div class="bg-white px-4 pt-5 pb-4 sm:p-6 sm:pb-4 max-h-[80vh] overflow-y-auto">
          <div class="flex items-center justify-between mb-4">
            <h3 class="text-lg font-medium text-gray-900">New Quote</h3>
            <button @click="handleClose" variant="outline">
              <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/>
              </svg>
            </button>
          </div>

          <div class="space-y-4">
            <!-- Client Selection -->
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1">Client *</label>
              <div class="relative" ref="clientDropdownRef">
                <!-- Dropdown Button -->
                <button
                  type="button"
                  @click="toggleClientDropdown"
                  class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 text-left flex items-center justify-between bg-white"
                  :class="formErrors.client ? 'border-red-500' : ''"
                >
                  <span :class="selectedClientId ? 'text-gray-900' : 'text-gray-500'">
                    {{ getSelectedClientDisplay }}
                  </span>
                  <svg
                    class="w-5 h-5 text-gray-400 transition-transform"
                    :class="{ 'rotate-180': isClientDropdownOpen }"
                    fill="none"
                    stroke="currentColor"
                    viewBox="0 0 24 24"
                  >
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7"/>
                  </svg>
                </button>

                <!-- Dropdown Menu -->
                <div
                  v-if="isClientDropdownOpen"
                  class="absolute z-10 w-full mt-1 bg-white border border-gray-300 rounded-lg shadow-lg max-h-60 overflow-hidden"
                >
                  <!-- Search Input Inside Dropdown -->
                  <div class="p-2 border-b border-gray-200 bg-gray-50">
                    <input
                      ref="clientSearchInputRef"
                      v-model="clientSearchQuery"
                      type="text"
                      placeholder="Search clients..."
                      class="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 text-sm"
                      @click.stop
                    />
                  </div>

                  <!-- Client Options -->
                  <div class="overflow-y-auto max-h-48">
                    <div
                      v-if="filteredClients.length === 0"
                      class="px-3 py-2 text-sm text-gray-500 text-center"
                    >
                      No clients found
                    </div>
                    <button
                      v-for="client in filteredClients"
                      :key="client.id"
                      type="button"
                      @click="selectClient(client.id)"
                      class="w-full px-3 py-2 text-left hover:bg-blue-50 focus:bg-blue-50 focus:outline-none transition-colors text-sm"
                      :class="{ 'bg-blue-100 font-medium': selectedClientId === client.id }"
                    >
                      <div class="font-medium">{{ client.name }} {{ client.surname }}</div>
                      <div class="text-xs text-gray-600">{{ client.email }}</div>
                      <div v-if="client.company" class="text-xs text-gray-500">{{ client.company }}</div>
                    </button>
                  </div>
                </div>
              </div>
              <p v-if="formErrors.client" class="mt-1 text-sm text-red-600">{{ formErrors.client }}</p>
            </div>

            <!-- Template Selection -->
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1">Quote Template *</label>
              <div class="relative" ref="templateDropdownRef">
                <!-- Dropdown Button -->
                <button
                  type="button"
                  @click="toggleTemplateDropdown"
                  :disabled="loadingTemplates"
                  class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 text-left flex items-center justify-between bg-white"
                  :class="formErrors.template ? 'border-red-500' : ''"
                >
                  <span :class="selectedTemplateId ? 'text-gray-900' : 'text-gray-500'">
                    {{ loadingTemplates ? 'Loading templates...' : (selectedTemplateId || 'Select a template...') }}
                  </span>
                  <svg
                    class="w-5 h-5 text-gray-400 transition-transform"
                    :class="{ 'rotate-180': isTemplateDropdownOpen }"
                    fill="none"
                    stroke="currentColor"
                    viewBox="0 0 24 24"
                  >
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7"/>
                  </svg>
                </button>

                <!-- Dropdown Menu -->
                <div
                  v-if="isTemplateDropdownOpen && !loadingTemplates"
                  class="absolute z-10 w-full mt-1 bg-white border border-gray-300 rounded-lg shadow-lg max-h-60 overflow-hidden"
                >
                  <!-- Search Input Inside Dropdown -->
                  <div class="p-2 border-b border-gray-200 bg-gray-50">
                    <input
                      ref="templateSearchInputRef"
                      v-model="templateSearchQuery"
                      type="text"
                      placeholder="Search templates..."
                      class="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 text-sm"
                      @click.stop
                    />
                  </div>

                  <!-- Template Options -->
                  <div class="overflow-y-auto max-h-48">
                    <div
                      v-if="filteredTemplates.length === 0"
                      class="px-3 py-2 text-sm text-gray-500 text-center"
                    >
                      No templates found
                    </div>
                    <button
                      v-for="template in filteredTemplates"
                      :key="template"
                      type="button"
                      @click="selectTemplate(template)"
                      class="w-full px-3 py-2 text-left hover:bg-blue-50 focus:bg-blue-50 focus:outline-none transition-colors text-sm"
                      :class="{ 'bg-blue-100 font-medium': selectedTemplateId === template }"
                    >
                      {{ template }}
                    </button>
                  </div>
                </div>
              </div>
              <p v-if="formErrors.template" class="mt-1 text-sm text-red-600">{{ formErrors.template }}</p>
              <p v-if="!loadingTemplates && templates.length === 0" class="mt-1 text-sm text-yellow-600">
                No templates available. Please contact administrator.
              </p>
            </div>

            <!-- Quote Items -->
            <div>
              <div class="flex items-center justify-between mb-2">
                <label class="block text-sm font-medium text-gray-700">Quote Items *</label>
                <Button @click="addQuoteItem" size="sm" variant="outline">
                  <svg class="w-4 h-4 mr-1" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4"/>
                  </svg>
                  Add Item
                </Button>
              </div>

              <div v-for="(item, index) in quoteItems" :key="index" class="mb-3 p-3 border border-gray-200 rounded-lg">
                <div class="flex items-start justify-between mb-2">
                  <span class="text-sm font-medium text-gray-700">Item {{ index + 1 }}</span>
                  <button
                    v-if="quoteItems.length > 1"
                    @click="removeQuoteItem(index)"
                    class="text-red-600 hover:text-red-700"
                  >
                    <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"/>
                    </svg>
                  </button>
                </div>

                <div class="space-y-2">
                  <div>
                    <input
                      v-model="item.description"
                      type="text"
                      placeholder="Description"
                      class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
                      :class="formErrors[`item_${index}_description`] ? 'border-red-500' : ''"
                    />
                    <p v-if="formErrors[`item_${index}_description`]" class="mt-1 text-sm text-red-600">
                      {{ formErrors[`item_${index}_description`] }}
                    </p>
                  </div>

                  <div class="grid grid-cols-2 gap-2">
                    <div>
                      <input
                        v-model.number="item.amount"
                        type="number"
                        min="1"
                        placeholder="Amount"
                        class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
                        :class="formErrors[`item_${index}_amount`] ? 'border-red-500' : ''"
                      />
                      <p v-if="formErrors[`item_${index}_amount`]" class="mt-1 text-sm text-red-600">
                        {{ formErrors[`item_${index}_amount`] }}
                      </p>
                    </div>

                    <div>
                      <input
                        v-model.number="item.pricePerUnit"
                        type="number"
                        min="0"
                        step="0.01"
                        placeholder="Price per unit"
                        class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
                        :class="formErrors[`item_${index}_price`] ? 'border-red-500' : ''"
                      />
                      <p v-if="formErrors[`item_${index}_price`]" class="mt-1 text-sm text-red-600">
                        {{ formErrors[`item_${index}_price`] }}
                      </p>
                    </div>
                  </div>

                  <div class="text-sm text-gray-600">
                    Subtotal: R {{ (item.amount * item.pricePerUnit).toFixed(2) }}
                  </div>
                </div>
              </div>
            </div>

            <!-- Total -->
            <div class="pt-3 border-t border-gray-200">
              <div class="flex justify-between items-center">
                <span class="text-lg font-semibold text-gray-900">Total:</span>
                <span class="text-lg font-bold text-blue-600">R {{ totalQuoteAmount.toFixed(2) }}</span>
              </div>
            </div>
          </div>
        </div>

        <div class="bg-gray-50 px-4 py-3 sm:px-6 sm:flex sm:flex-row-reverse gap-2">
          <Button @click="handleSave" variant="default">
            Create Quote
          </Button>
          <Button @click="handleClose" variant="outline">
            Cancel
          </Button>
        </div>
      </div>
    </div>
  </div>
</template>
