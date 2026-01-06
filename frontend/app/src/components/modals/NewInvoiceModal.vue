<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import { Button } from '../ui/index'

interface InvoiceItem {
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
  (e: 'save', data: { clientId: number, items: InvoiceItem[] }): void
}

const props = defineProps<Props>()
const emit = defineEmits<Emits>()

const selectedClientId = ref<number | null>(null)
const invoiceItems = ref<InvoiceItem[]>([{ description: '', amount: 1, pricePerUnit: 0 }])
const formErrors = ref<Record<string, string>>({})

const addInvoiceItem = () => {
  invoiceItems.value.push({ description: '', amount: 1, pricePerUnit: 0 })
}

const removeInvoiceItem = (index: number) => {
  if (invoiceItems.value.length > 1) {
    invoiceItems.value.splice(index, 1)
  }
}

const validateForm = () => {
  formErrors.value = {}
  
  if (!selectedClientId.value) {
    formErrors.value.client = 'Please select a client'
  }
  
  invoiceItems.value.forEach((item, index) => {
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
    items: invoiceItems.value
  })
}

const handleClose = () => {
  emit('close')
}

const totalInvoiceAmount = computed(() => {
  return invoiceItems.value.reduce((sum, item) => sum + (item.amount * item.pricePerUnit), 0)
})

const filteredClients = computed(() => {
  return props.clients
})

// Reset form when modal opens
const resetForm = () => {
  selectedClientId.value = null
  invoiceItems.value = [{ description: '', amount: 1, pricePerUnit: 0 }]
  formErrors.value = {}
}

// Watch for show prop changes to reset form
watch(() => props.show, (newVal) => {
  if (newVal) {
    resetForm()
  }
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
            <h3 class="text-lg font-medium text-gray-900">New Invoice</h3>
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
              <select
                v-model="selectedClientId"
                class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
                :class="formErrors.client ? 'border-red-500' : ''"
              >
                <option :value="null">Select a client...</option>
                <option v-for="client in filteredClients" :key="client.id" :value="client.id">
                  {{ client.name }} {{ client.surname }} ({{ client.email }})
                </option>
              </select>
              <p v-if="formErrors.client" class="mt-1 text-sm text-red-600">{{ formErrors.client }}</p>
            </div>

            <!-- Invoice Items -->
            <div>
              <div class="flex items-center justify-between mb-2">
                <label class="block text-sm font-medium text-gray-700">Invoice Items *</label>
                <Button @click="addInvoiceItem" size="sm" variant="outline">
                  <svg class="w-4 h-4 mr-1" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4"/>
                  </svg>
                  Add Item
                </Button>
              </div>

              <div v-for="(item, index) in invoiceItems" :key="index" class="mb-3 p-3 border border-gray-200 rounded-lg">
                <div class="flex items-start justify-between mb-2">
                  <span class="text-sm font-medium text-gray-700">Item {{ index + 1 }}</span>
                  <button
                    v-if="invoiceItems.length > 1"
                    @click="removeInvoiceItem(index)"
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
                <span class="text-lg font-bold text-blue-600">R {{ totalInvoiceAmount.toFixed(2) }}</span>
              </div>
            </div>
          </div>
        </div>

        <div class="bg-gray-50 px-4 py-3 sm:px-6 sm:flex sm:flex-row-reverse gap-2">
          <Button @click="handleSave" variant="default">
            Create Invoice
          </Button>
          <Button @click="handleClose" variant="outline">
            Cancel
          </Button>
        </div>
      </div>
    </div>
  </div>
</template>
