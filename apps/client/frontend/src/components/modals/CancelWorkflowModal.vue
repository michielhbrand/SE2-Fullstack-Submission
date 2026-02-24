<script setup lang="ts">
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogDescription, DialogFooter, Button } from '../ui/index'

interface Props {
  show: boolean
  loading?: boolean
}

interface Emits {
  (e: 'close'): void
  (e: 'confirm'): void
}

defineProps<Props>()
defineEmits<Emits>()
</script>

<template>
  <Dialog :open="show" @update:open="!$event && $emit('close')">
    <DialogContent class="sm:max-w-md">
      <DialogHeader>
        <DialogTitle>Cancel Workflow</DialogTitle>
        <DialogDescription>
          Are you sure you want to cancel this workflow? This action cannot be undone and will stop all further processing.
        </DialogDescription>
      </DialogHeader>
      <DialogFooter class="gap-2 sm:gap-0">
        <Button variant="outline" @click="$emit('close')">
          Go Back
        </Button>
        <Button
          variant="destructive"
          :disabled="loading"
          @click="$emit('confirm')"
        >
          <svg v-if="loading" class="animate-spin -ml-1 mr-2 h-4 w-4" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
            <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4" />
            <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z" />
          </svg>
          Yes, Cancel Workflow
        </Button>
      </DialogFooter>
    </DialogContent>
  </Dialog>
</template>
