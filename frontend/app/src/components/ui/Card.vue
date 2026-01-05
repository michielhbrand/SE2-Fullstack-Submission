<script setup lang="ts">
import { computed, type HTMLAttributes } from 'vue'
import { cn } from '@/lib/utils'

interface Props {
  class?: HTMLAttributes['class']
}

const props = defineProps<Props>()
</script>

<template>
  <div :class="cn('rounded-lg border-2 border-gray-200 bg-white p-6', props.class)">
    <slot />
  </div>
</template>
