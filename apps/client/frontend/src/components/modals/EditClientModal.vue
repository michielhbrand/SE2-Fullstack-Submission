<script setup lang="ts">
import { ref, watch } from 'vue'
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter, Button } from '../ui/index'
import { toast } from 'vue-sonner'

interface ClientForm {
  id: number
  name: string
  surname: string
  email: string
  cellphone: string
  address: string
  company: string
}

interface Props {
  show: boolean
  client: ClientForm | null
}

interface Emits {
  (e: 'close'): void
  (e: 'save', client: ClientForm): void
}

const props = defineProps<Props>()
const emit = defineEmits<Emits>()

const editClient = ref<ClientForm>({
  id: 0,
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
  
  if (!editClient.value.name.trim()) {
    formErrors.value.name = 'Name is required'
  }
  
  if (!editClient.value.surname.trim()) {
    formErrors.value.surname = 'Surname is required'
  }
  
  if (!editClient.value.email.trim()) {
    formErrors.value.email = 'Email is required'
  } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(editClient.value.email)) {
    formErrors.value.email = 'Invalid email format'
  }
  
  if (!editClient.value.cellphone.trim()) {
    formErrors.value.cellphone = 'Cellphone is required'
  }
  
  return Object.keys(formErrors.value).length === 0
}

const handleSave = () => {
  if (!validateForm()) {
    toast.error('Please fix the validation errors before submitting')
    return
  }
  emit('save', editClient.value)
}

const handleClose = () => {
  emit('close')
}

// Watch for client prop changes to populate form
watch(() => props.client, (newClient) => {
  if (newClient) {
    editClient.value = {
      id: newClient.id,
      name: newClient.name,
      surname: newClient.surname,
      email: newClient.email,
      cellphone: newClient.cellphone,
      address: newClient.address || '',
      company: newClient.company || ''
    }
    formErrors.value = {}
  }
}, { immediate: true })
</script>

<template>
  <Dialog :open="show" @update:open="(open) => !open && handleClose()">
    <DialogContent class="sm:max-w-lg">
      <DialogHeader>
        <DialogTitle>Edit Client</DialogTitle>
      </DialogHeader>

      <div class="space-y-4">
        <div>
          <label class="block text-sm font-medium text-gray-700 mb-1">Name *</label>
          <input
            v-model="editClient.name"
            type="text"
            class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
            :class="formErrors.name ? 'border-red-500' : ''"
          />
          <p v-if="formErrors.name" class="mt-1 text-sm text-red-600">{{ formErrors.name }}</p>
        </div>

        <div>
          <label class="block text-sm font-medium text-gray-700 mb-1">Surname *</label>
          <input
            v-model="editClient.surname"
            type="text"
            class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
            :class="formErrors.surname ? 'border-red-500' : ''"
          />
          <p v-if="formErrors.surname" class="mt-1 text-sm text-red-600">{{ formErrors.surname }}</p>
        </div>

        <div>
          <label class="block text-sm font-medium text-gray-700 mb-1">Email *</label>
          <input
            v-model="editClient.email"
            type="email"
            class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
            :class="formErrors.email ? 'border-red-500' : ''"
          />
          <p v-if="formErrors.email" class="mt-1 text-sm text-red-600">{{ formErrors.email }}</p>
        </div>

        <div>
          <label class="block text-sm font-medium text-gray-700 mb-1">Cellphone *</label>
          <input
            v-model="editClient.cellphone"
            type="tel"
            class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
            :class="formErrors.cellphone ? 'border-red-500' : ''"
          />
          <p v-if="formErrors.cellphone" class="mt-1 text-sm text-red-600">{{ formErrors.cellphone }}</p>
        </div>

        <div>
          <label class="block text-sm font-medium text-gray-700 mb-1">Address</label>
          <textarea
            v-model="editClient.address"
            rows="2"
            class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
          ></textarea>
        </div>

        <div>
          <label class="block text-sm font-medium text-gray-700 mb-1">Company</label>
          <input
            v-model="editClient.company"
            type="text"
            class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
          />
        </div>
      </div>

      <DialogFooter>
        <Button @click="handleClose" variant="outline">Cancel</Button>
        <Button @click="handleSave" variant="default">Update Client</Button>
      </DialogFooter>
    </DialogContent>
  </Dialog>
</template>
