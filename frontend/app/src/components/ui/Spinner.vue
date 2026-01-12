<script setup lang="ts">
import type { HTMLAttributes } from 'vue'
import { LoaderIcon } from 'lucide-vue-next'
import { cn } from '@/lib/utils'
import { computed } from 'vue'

const props = defineProps<{
  class?: HTMLAttributes['class']
  size?: 'sm' | 'md' | 'lg' | 'xl'
}>()

const sizeClasses = computed(() => {
  switch (props.size) {
    case 'sm':
      return 'size-4'
    case 'md':
      return 'size-6'
    case 'lg':
      return 'size-8'
    case 'xl':
      return 'size-12'
    default:
      return 'size-4'
  }
})
</script>

<template>
  <LoaderIcon
    role="status"
    aria-label="Loading"
    :class="cn(sizeClasses, 'animate-spin', props.class)"
  />
</template>
