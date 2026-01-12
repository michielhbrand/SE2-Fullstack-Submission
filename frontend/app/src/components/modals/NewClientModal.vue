<script setup lang="ts">
import { ref } from 'vue'
import { Button } from '../ui/index'
import { toast } from 'vue-sonner'

interface ClientForm {
  name: string
  surname: string
  email: string
  cellphone: string
  address: string
  company: string
}

interface Props {
  show: boolean
}

interface Emits {
  (e: 'close'): void
  (e: 'save', client: ClientForm): void
}

const props = defineProps<Props>()
const emit = defineEmits<Emits>()

const client = ref<ClientForm>({
  name: '',
  surname: '',
  email: '',
  cellphone: '',
  address: '',
  company: ''
})

const formErrors = ref<Record<string, string>>({})

const validateForm = () => {
  formErrors.value = {}
  
  if (!client.value.name.trim()) {
    formErrors.value.name = 'Name is required'
  }
  
  if (!client.value.surname.trim()) {
    formErrors.value.surname = 'Surname is required'
  }
  
  if (!client.value.email.trim()) {
    formErrors.value.email = 'Email is required'
  } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(client.value.email)) {
    formErrors.value.email = 'Invalid email format'
  }
  
  if (!client.value.cellphone.trim()) {
    formErrors.value.cellphone = 'Cellphone is required'
  }
  
  return Object.keys(formErrors.value).length === 0
}

const handleSave = () => {
  if (!validateForm()) {
    toast.error('Please fix the validation errors before submitting')
    return
  }
  emit('save', client.value)
}

const handleClose = () => {
  emit('close')
}

// Reset form when modal opens
const resetForm = () => {
  client.value = {
    name: '',
    surname: '',
    email: '',
    cellphone: '',
    address: '',
    company: ''
  }
  formErrors.value = {}
}

// Watch for show prop changes to reset form
import { watch } from 'vue'
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

      <div class="relative inline-block align-bottom bg-white rounded-lg text-left overflow-hidden shadow-xl transform transition-all sm:my-8 sm:align-middle sm:max-w-lg sm:w-full">
        <div class="bg-white px-4 pt-5 pb-4 sm:p-6 sm:pb-4">
          <div class="flex items-center justify-between mb-4">
            <h3 class="text-lg font-medium text-gray-900">New Client</h3>
            <button @click="handleClose" variant="outline">
              <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/>
              </svg>
            </button>
          </div>

          <div class="space-y-4">
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1">Name *</label>
              <input
                v-model="client.name"
                type="text"
                class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
                :class="formErrors.name ? 'border-red-500' : ''"
              />
              <p v-if="formErrors.name" class="mt-1 text-sm text-red-600">{{ formErrors.name }}</p>
            </div>

            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1">Surname *</label>
              <input
                v-model="client.surname"
                type="text"
                class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
                :class="formErrors.surname ? 'border-red-500' : ''"
              />
              <p v-if="formErrors.surname" class="mt-1 text-sm text-red-600">{{ formErrors.surname }}</p>
            </div>

            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1">Email *</label>
              <input
                v-model="client.email"
                type="email"
                class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
                :class="formErrors.email ? 'border-red-500' : ''"
              />
              <p v-if="formErrors.email" class="mt-1 text-sm text-red-600">{{ formErrors.email }}</p>
            </div>

            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1">Cellphone *</label>
              <input
                v-model="client.cellphone"
                type="tel"
                class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
                :class="formErrors.cellphone ? 'border-red-500' : ''"
              />
              <p v-if="formErrors.cellphone" class="mt-1 text-sm text-red-600">{{ formErrors.cellphone }}</p>
            </div>

            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1">Address</label>
              <textarea
                v-model="client.address"
                rows="2"
                class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
              ></textarea>
            </div>

            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1">Company</label>
              <input
                v-model="client.company"
                type="text"
                class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
            </div>
          </div>
        </div>

        <div class="bg-gray-50 px-4 py-3 sm:px-6 sm:flex sm:flex-row-reverse gap-2">
          <Button @click="handleSave" variant="default">
            Save Client
          </Button>
          <Button @click="handleClose" variant="outline">
            Cancel
          </Button>
        </div>
      </div>
    </div>
  </div>
</template>
