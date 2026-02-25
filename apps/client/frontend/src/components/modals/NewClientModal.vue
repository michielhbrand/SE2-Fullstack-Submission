<script setup lang="ts">
import { ref, watch } from 'vue'
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter, Button, Textarea, Input, Label } from '../ui/index'
import { Checkbox } from '../ui/checkbox'
import { toast } from 'vue-sonner'

interface ClientForm {
  name: string
  email: string
  cellphone: string
  address: string
  isCompany: boolean
  vatNumber: string
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
  email: '',
  cellphone: '',
  address: '',
  isCompany: false,
  vatNumber: ''
})

const formErrors = ref<Record<string, string>>({})

const validateForm = () => {
  formErrors.value = {}
  
  if (!client.value.name.trim()) {
    formErrors.value.name = 'Full name / company name is required'
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
    email: '',
    cellphone: '',
    address: '',
    isCompany: false,
    vatNumber: ''
  }
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
  <Dialog :open="show" @update:open="(open) => !open && handleClose()">
    <DialogContent class="sm:max-w-lg">
      <DialogHeader>
        <DialogTitle>New Client</DialogTitle>
      </DialogHeader>

      <div class="space-y-4">
        <div class="flex items-center space-x-2">
          <Checkbox
            id="isCompany"
            :checked="client.isCompany"
            @update:checked="(val: boolean) => client.isCompany = val"
          />
          <Label for="isCompany">This client is a company</Label>
        </div>

        <div>
          <Label for="name">Full Name / Company Name *</Label>
          <Input
            id="name"
            v-model="client.name"
            type="text"
            placeholder="Enter full name or company name"
            :class="formErrors.name ? 'border-red-500' : ''"
          />
          <p v-if="formErrors.name" class="mt-1 text-sm text-red-600">{{ formErrors.name }}</p>
        </div>

        <div>
          <Label for="email">Email *</Label>
          <Input
            id="email"
            v-model="client.email"
            type="email"
            :class="formErrors.email ? 'border-red-500' : ''"
          />
          <p v-if="formErrors.email" class="mt-1 text-sm text-red-600">{{ formErrors.email }}</p>
        </div>

        <div>
          <Label for="cellphone">Cellphone *</Label>
          <Input
            id="cellphone"
            v-model="client.cellphone"
            type="tel"
            :class="formErrors.cellphone ? 'border-red-500' : ''"
          />
          <p v-if="formErrors.cellphone" class="mt-1 text-sm text-red-600">{{ formErrors.cellphone }}</p>
        </div>

        <div>
          <Label for="address">Address</Label>
          <Textarea
            id="address"
            v-model="client.address"
            rows="2"
            placeholder="Enter address"
          />
        </div>

        <div>
          <Label for="vatNumber">VAT Number</Label>
          <Input
            id="vatNumber"
            v-model="client.vatNumber"
            type="text"
            placeholder="Enter VAT number (optional)"
          />
        </div>
      </div>

      <DialogFooter>
        <Button @click="handleClose" variant="outline">Cancel</Button>
        <Button @click="handleSave" variant="default">Save Client</Button>
      </DialogFooter>
    </DialogContent>
  </Dialog>
</template>
