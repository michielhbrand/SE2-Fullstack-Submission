<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter, Button, Input, Label } from '../ui/index'

interface Props {
  show: boolean
  loading?: boolean
}

interface Emits {
  (e: 'close'): void
  (e: 'confirm', payByDays: number): void
}

const props = defineProps<Props>()
const emit = defineEmits<Emits>()

const payByDays = ref<number>(30)
const error = ref('')

const computedPayByDate = computed(() => {
  const days = payByDays.value || 30
  const date = new Date()
  date.setDate(date.getDate() + days)
  return date.toLocaleDateString(undefined, { year: 'numeric', month: 'short', day: 'numeric' })
})

const validate = (): boolean => {
  error.value = ''
  if (!payByDays.value || payByDays.value < 1 || payByDays.value > 365) {
    error.value = 'Pay by days must be between 1 and 365'
    return false
  }
  return true
}

const handleConfirm = () => {
  if (!validate()) return
  emit('confirm', payByDays.value)
}

const handleClose = () => {
  emit('close')
}

watch(() => props.show, (newVal) => {
  if (newVal) {
    payByDays.value = 30
    error.value = ''
  }
})
</script>

<template>
  <Dialog :open="show" @update:open="(open) => !open && handleClose()">
    <DialogContent class="sm:max-w-md" :prevent-close="true">
      <DialogHeader>
        <DialogTitle>Convert to Invoice</DialogTitle>
      </DialogHeader>

      <div class="space-y-4 py-4">
        <p class="text-sm text-gray-600">
          This will convert the approved quote into an invoice. Please specify the payment terms.
        </p>

        <div>
          <Label for="convert-pay-by-days">Days until payment is due *</Label>
          <div class="flex items-center gap-3 mt-1">
            <Input
              id="convert-pay-by-days"
              v-model.number="payByDays"
              type="number"
              min="1"
              max="365"
              placeholder="30"
              class="w-32"
              :class="error ? 'border-red-500' : ''"
              @keyup.enter="handleConfirm"
            />
            <span class="text-sm text-gray-500">
              Due date: {{ computedPayByDate }}
            </span>
          </div>
          <p v-if="error" class="mt-1 text-sm text-red-600">{{ error }}</p>
        </div>
      </div>

      <DialogFooter>
        <Button @click="handleClose" variant="outline" :disabled="loading">Cancel</Button>
        <Button @click="handleConfirm" variant="default" :disabled="loading">
          <svg v-if="loading" class="animate-spin -ml-1 mr-2 h-4 w-4" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
            <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
            <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
          </svg>
          Convert to Invoice
        </Button>
      </DialogFooter>
    </DialogContent>
  </Dialog>
</template>
