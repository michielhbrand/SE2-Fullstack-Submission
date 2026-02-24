<script setup lang="ts">
import { ref, watch } from 'vue'
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogDescription, DialogFooter, Button, Textarea } from '../ui/index'

interface Props {
  show: boolean
  eventLabel: string
  loading?: boolean
}

interface Emits {
  (e: 'close'): void
  (e: 'confirm', description: string): void
}

const props = defineProps<Props>()
const emit = defineEmits<Emits>()

const description = ref('')

watch(() => props.show, (newVal) => {
  if (newVal) {
    description.value = ''
  }
})

const handleConfirm = () => {
  emit('confirm', description.value)
}
</script>

<template>
  <Dialog :open="show" @update:open="!$event && $emit('close')">
    <DialogContent class="sm:max-w-md">
      <DialogHeader>
        <DialogTitle>{{ eventLabel }}</DialogTitle>
        <DialogDescription>
          Add an optional note about this action.
        </DialogDescription>
      </DialogHeader>
      <div class="py-2">
        <Textarea
          v-model="description"
          placeholder="Add a note about this action..."
          :rows="3"
        />
      </div>
      <DialogFooter class="gap-2 sm:gap-0">
        <Button variant="outline" @click="$emit('close')">
          Cancel
        </Button>
        <Button
          :disabled="loading"
          @click="handleConfirm"
        >
          <svg v-if="loading" class="animate-spin -ml-1 mr-2 h-4 w-4" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
            <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4" />
            <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z" />
          </svg>
          Confirm
        </Button>
      </DialogFooter>
    </DialogContent>
  </Dialog>
</template>
