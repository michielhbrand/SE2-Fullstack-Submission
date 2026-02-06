<script setup lang="ts">
import { ref, onMounted, onUnmounted } from 'vue'
import { healthApi } from '../services/api'

const isOnline = ref<boolean | null>(null)
const checkInterval = ref<number | null>(null)

const checkServerStatus = async () => {
  try {
    await healthApi.getHealth()
    isOnline.value = true
  } catch (error) {
    isOnline.value = false
  }
}

onMounted(() => {
  // Check immediately on mount
  checkServerStatus()
  
  // Check every 10 seconds
  checkInterval.value = window.setInterval(() => {
    checkServerStatus()
  }, 10000)
})

onUnmounted(() => {
  if (checkInterval.value) {
    clearInterval(checkInterval.value)
  }
})
</script>

<template>
  <div v-if="isOnline !== null" class="relative group">
    <!-- Status Icon -->
    <svg
      class="w-3 h-3 fill-current transition-colors"
      :class="isOnline ? 'text-green-500' : 'text-orange-500'"
      viewBox="0 0 20 20"
    >
      <circle cx="10" cy="10" r="8" />
    </svg>
    
    <!-- Tooltip -->
    <div class="absolute left-0 top-full mt-2 px-2 py-1 bg-gray-900 text-white text-xs rounded whitespace-nowrap opacity-0 group-hover:opacity-100 transition-opacity pointer-events-none z-50">
      {{ isOnline ? 'Server Online' : 'Server Offline' }}
    </div>
  </div>
</template>
