<script setup lang="ts">
import { ref, watch } from 'vue'
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter, Button, Textarea, Input, Label } from '../ui/index'
import { Checkbox } from '../ui/checkbox'
import { toast } from 'vue-sonner'

interface ClientForm {
  id: number
  name: string
  email: string
  cellphone: string
  address: string
  isCompany: boolean
  vatNumber: string
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
  email: '',
  cellphone: '',
  address: '',
  isCompany: false,
  vatNumber: ''
})

const formErrors = ref<Record<string, string>>({})

const validateForm = () => {
  formErrors.value = {}
  
  if (!editClient.value.name.trim()) {
    formErrors.value.name = 'Full name / company name is required'
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
      email: newClient.email,
      cellphone: newClient.cellphone,
      address: newClient.address || '',
      isCompany: newClient.isCompany || false,
      vatNumber: newClient.vatNumber || ''
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
        <div class="flex items-center space-x-2">
          <Checkbox
            id="edit-isCompany"
            :checked="editClient.isCompany"
            @update:checked="(val: boolean) => editClient.isCompany = val"
          />
          <Label for="edit-isCompany">This client is a company</Label>
        </div>

        <div>
          <Label for="edit-name">Full Name / Company Name *</Label>
          <Input
            id="edit-name"
            v-model="editClient.name"
            type="text"
            placeholder="Enter full name or company name"
            :class="formErrors.name ? 'border-red-500' : ''"
          />
          <p v-if="formErrors.name" class="mt-1 text-sm text-red-600">{{ formErrors.name }}</p>
        </div>

        <div>
          <Label for="edit-email">Email *</Label>
          <Input
            id="edit-email"
            v-model="editClient.email"
            type="email"
            :class="formErrors.email ? 'border-red-500' : ''"
          />
          <p v-if="formErrors.email" class="mt-1 text-sm text-red-600">{{ formErrors.email }}</p>
        </div>

        <div>
          <Label for="edit-cellphone">Cellphone *</Label>
          <Input
            id="edit-cellphone"
            v-model="editClient.cellphone"
            type="tel"
            :class="formErrors.cellphone ? 'border-red-500' : ''"
          />
          <p v-if="formErrors.cellphone" class="mt-1 text-sm text-red-600">{{ formErrors.cellphone }}</p>
        </div>

        <div>
          <Label for="edit-address">Address</Label>
          <Textarea
            id="edit-address"
            v-model="editClient.address"
            rows="2"
            placeholder="Enter address"
          />
        </div>

        <div>
          <Label for="edit-vatNumber">VAT Number</Label>
          <Input
            id="edit-vatNumber"
            v-model="editClient.vatNumber"
            type="text"
            placeholder="Enter VAT number (optional)"
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
