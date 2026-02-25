<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from 'vue'

export interface DropdownItem {
  id: number | string
  /** Text shown in the trigger button when this item is selected */
  label: string
  /** Primary text shown in the dropdown list */
  primaryText: string
  /** Optional secondary line in the dropdown list */
  secondaryText?: string
  /** Optional badge text (e.g. "Company") */
  badge?: string
}

interface Props {
  modelValue: number | string | null
  items: DropdownItem[]
  placeholder?: string
  searchPlaceholder?: string
  loading?: boolean
  loadingText?: string
  error?: string
  emptyText?: string
  disabled?: boolean
}

const props = withDefaults(defineProps<Props>(), {
  placeholder: 'Select...',
  searchPlaceholder: 'Search...',
  loading: false,
  loadingText: 'Loading...',
  emptyText: 'No items found',
  disabled: false,
})

const emit = defineEmits<{
  (e: 'update:modelValue', value: number | string | null): void
}>()

const isOpen = ref(false)
const searchQuery = ref('')
const containerRef = ref<HTMLDivElement | null>(null)
const searchInputRef = ref<HTMLInputElement | null>(null)

const selectedItem = computed(() =>
  props.items.find(item => item.id === props.modelValue) ?? null
)

const triggerLabel = computed(() =>
  selectedItem.value ? selectedItem.value.label : props.placeholder
)

const filteredItems = computed(() => {
  if (!searchQuery.value) return props.items
  const q = searchQuery.value.toLowerCase()
  return props.items.filter(item =>
    item.primaryText.toLowerCase().includes(q) ||
    item.secondaryText?.toLowerCase().includes(q)
  )
})

const toggle = () => {
  if (props.disabled || props.loading) return
  isOpen.value = !isOpen.value
  if (isOpen.value) {
    setTimeout(() => searchInputRef.value?.focus(), 50)
  }
}

const select = (item: DropdownItem) => {
  emit('update:modelValue', item.id)
  isOpen.value = false
  searchQuery.value = ''
}

const handleClickOutside = (event: MouseEvent) => {
  if (containerRef.value && !containerRef.value.contains(event.target as Node)) {
    isOpen.value = false
  }
}

onMounted(() => document.addEventListener('click', handleClickOutside))
onUnmounted(() => document.removeEventListener('click', handleClickOutside))
</script>

<template>
  <div ref="containerRef" class="relative">
    <!-- Trigger button -->
    <button
      type="button"
      @click="toggle"
      :disabled="disabled"
      class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 text-left flex items-center justify-between bg-white disabled:opacity-60 disabled:cursor-not-allowed"
      :class="error ? 'border-red-500' : ''"
    >
      <span :class="modelValue ? 'text-gray-900' : 'text-gray-500'">
        {{ loading ? loadingText : triggerLabel }}
      </span>
      <svg
        class="w-5 h-5 text-gray-400 transition-transform"
        :class="{ 'rotate-180': isOpen }"
        fill="none"
        stroke="currentColor"
        viewBox="0 0 24 24"
      >
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7"/>
      </svg>
    </button>

    <!-- Dropdown panel -->
    <div
      v-if="isOpen && !loading"
      class="absolute z-10 w-full mt-1 bg-white border border-gray-300 rounded-lg shadow-lg max-h-60 overflow-hidden"
    >
      <div class="p-2 border-b border-gray-200 bg-gray-50">
        <input
          ref="searchInputRef"
          v-model="searchQuery"
          type="text"
          :placeholder="searchPlaceholder"
          class="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 text-sm"
          @click.stop
        />
      </div>

      <div class="overflow-y-auto max-h-48">
        <div
          v-if="filteredItems.length === 0"
          class="px-3 py-2 text-sm text-gray-500 text-center"
        >
          {{ emptyText }}
        </div>
        <button
          v-for="item in filteredItems"
          :key="item.id"
          type="button"
          @click="select(item)"
          class="w-full px-3 py-2 text-left hover:bg-blue-50 focus:bg-blue-50 focus:outline-none transition-colors text-sm"
          :class="{ 'bg-blue-100 font-medium': modelValue === item.id }"
        >
          <div class="font-medium">{{ item.primaryText }}</div>
          <div v-if="item.secondaryText" class="text-xs text-gray-600">{{ item.secondaryText }}</div>
          <div v-if="item.badge" class="text-xs text-gray-500">{{ item.badge }}</div>
        </button>
      </div>
    </div>

    <!-- Error message -->
    <p v-if="error" class="mt-1 text-sm text-red-600">{{ error }}</p>
  </div>
</template>
