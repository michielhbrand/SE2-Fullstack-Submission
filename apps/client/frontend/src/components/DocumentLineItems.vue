<script setup lang="ts">
import { Button, Input, Label } from './ui/index'

export interface LineItem {
  description: string
  amount: number
  pricePerUnit: number
}

interface Props {
  modelValue: LineItem[]
  errors: Record<string, string>
  label: string
  /** Prefix for input element IDs to avoid collisions when multiple instances exist */
  idPrefix: string
}

const props = defineProps<Props>()
const emit = defineEmits<{
  (e: 'update:modelValue', value: LineItem[]): void
}>()

const addItem = () => {
  emit('update:modelValue', [...props.modelValue, { description: '', amount: 1, pricePerUnit: 0 }])
}

const removeItem = (index: number) => {
  const updated = props.modelValue.filter((_, i) => i !== index)
  emit('update:modelValue', updated)
}

const updateItem = (index: number, field: keyof LineItem, value: string | number) => {
  const updated = props.modelValue.map((item, i) =>
    i === index ? { ...item, [field]: value } : item
  )
  emit('update:modelValue', updated)
}
</script>

<template>
  <div>
    <div class="flex items-center justify-between mb-2">
      <label class="block text-sm font-medium text-gray-700">{{ label }} *</label>
      <Button @click="addItem" size="sm" variant="outline">
        <svg class="w-4 h-4 mr-1" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4"/>
        </svg>
        Add Item
      </Button>
    </div>

    <div
      v-for="(item, index) in modelValue"
      :key="index"
      class="mb-3 p-3 border border-gray-200 rounded-lg"
    >
      <div class="flex items-start justify-between mb-2">
        <span class="text-sm font-medium text-gray-700">Item {{ index + 1 }}</span>
        <button
          v-if="modelValue.length > 1"
          type="button"
          @click="removeItem(index)"
          class="text-red-600 hover:text-red-700"
        >
          <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"/>
          </svg>
        </button>
      </div>

      <div class="space-y-2">
        <div>
          <Label :for="`${idPrefix}-desc-${index}`">Description</Label>
          <Input
            :id="`${idPrefix}-desc-${index}`"
            :value="item.description"
            @input="updateItem(index, 'description', ($event.target as HTMLInputElement).value)"
            type="text"
            placeholder="Description"
            :class="errors[`item_${index}_description`] ? 'border-red-500' : ''"
          />
          <p v-if="errors[`item_${index}_description`]" class="mt-1 text-sm text-red-600">
            {{ errors[`item_${index}_description`] }}
          </p>
        </div>

        <div class="grid grid-cols-2 gap-2">
          <div>
            <Label :for="`${idPrefix}-amount-${index}`">Amount</Label>
            <Input
              :id="`${idPrefix}-amount-${index}`"
              :value="item.amount"
              @input="updateItem(index, 'amount', Number(($event.target as HTMLInputElement).value))"
              type="number"
              min="1"
              placeholder="Amount"
              :class="errors[`item_${index}_amount`] ? 'border-red-500' : ''"
            />
            <p v-if="errors[`item_${index}_amount`]" class="mt-1 text-sm text-red-600">
              {{ errors[`item_${index}_amount`] }}
            </p>
          </div>

          <div>
            <Label :for="`${idPrefix}-price-${index}`">Price per unit</Label>
            <Input
              :id="`${idPrefix}-price-${index}`"
              :value="item.pricePerUnit"
              @input="updateItem(index, 'pricePerUnit', Number(($event.target as HTMLInputElement).value))"
              type="number"
              min="0"
              step="0.01"
              placeholder="Price per unit"
              :class="errors[`item_${index}_price`] ? 'border-red-500' : ''"
            />
            <p v-if="errors[`item_${index}_price`]" class="mt-1 text-sm text-red-600">
              {{ errors[`item_${index}_price`] }}
            </p>
          </div>
        </div>

        <div class="text-sm text-gray-600">
          Subtotal: R {{ (item.amount * item.pricePerUnit).toFixed(2) }}
        </div>
      </div>
    </div>
  </div>
</template>
