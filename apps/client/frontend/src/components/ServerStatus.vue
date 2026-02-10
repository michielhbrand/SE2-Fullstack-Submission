<script setup lang="ts">
import { ref, onMounted, onUnmounted } from 'vue'
import { healthApi } from '../services/api'
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from './ui/index'

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
  <TooltipProvider v-if="isOnline !== null">
    <Tooltip>
      <TooltipTrigger as-child>
        <svg
          class="w-3 h-3 fill-current transition-colors cursor-default"
          :class="isOnline ? 'text-green-500' : 'text-orange-500'"
          viewBox="0 0 20 20"
        >
          <circle cx="10" cy="10" r="8" />
        </svg>
      </TooltipTrigger>
      <TooltipContent>
        {{ isOnline ? 'Server Online' : 'Server Offline' }}
      </TooltipContent>
    </Tooltip>
  </TooltipProvider>
</template>
