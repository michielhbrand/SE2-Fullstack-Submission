<script setup lang="ts">
import { Button, Spinner } from '../ui/index'

interface Props {
  show: boolean
  previewUrl: string | null
  templateName: string | null
  loading: boolean
}

interface Emits {
  (e: 'close'): void
}

defineProps<Props>()
const emit = defineEmits<Emits>()
</script>

<template>
  <div
    v-if="show"
    class="fixed inset-0 z-50 flex items-center justify-center bg-black bg-opacity-50 p-4"
    @click.self="emit('close')"
  >
    <div class="bg-white rounded-lg shadow-xl w-full max-w-6xl h-[90vh] flex flex-col">
      <!-- Header -->
      <div class="flex items-center justify-between px-6 py-4 border-b border-gray-200">
        <div>
          <h2 class="text-xl font-semibold text-gray-900">Template Preview</h2>
          <p class="text-sm text-gray-500 mt-1">{{ templateName }}</p>
        </div>
        <Button @click="emit('close')" variant="ghost" size="sm" class="text-gray-400 hover:text-gray-600">
          <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/>
          </svg>
        </Button>
      </div>

      <!-- Content -->
      <div class="flex-1 overflow-hidden relative">
        <div v-if="loading" class="absolute inset-0 flex items-center justify-center bg-gray-50">
          <Spinner class="w-8 h-8" />
        </div>
        <iframe
          v-else-if="previewUrl"
          :src="previewUrl"
          class="w-full h-full border-0"
          title="Template Preview"
        />
      </div>

      <!-- Footer -->
      <div class="flex items-center justify-end gap-3 px-6 py-4 border-t border-gray-200 bg-gray-50">
        <Button @click="emit('close')" variant="outline">Close</Button>
        <a v-if="previewUrl" :href="previewUrl" download target="_blank">
          <Button variant="default">
            <svg class="w-4 h-4 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-4l-4 4m0 0l-4-4m4 4V4"/>
            </svg>
            Download
          </Button>
        </a>
      </div>
    </div>
  </div>
</template>
