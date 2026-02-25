<script setup lang="ts">
import { useRouter } from 'vue-router'
import { useOrganizationStore } from '../../stores/organization'
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogDescription } from '../ui/index'

const props = defineProps<{
  show: boolean
}>()

const emit = defineEmits<{
  close: []
}>()

const router = useRouter()
const organizationStore = useOrganizationStore()

const handleSwitchOrganization = async (orgId: number) => {
  await organizationStore.switchOrganization(orgId)
  emit('close')
  // Reload the current page data by navigating to the same route
  router.go(0)
}
</script>

<template>
  <Dialog :open="props.show" @update:open="(val: boolean) => { if (!val) emit('close') }">
    <DialogContent class="sm:max-w-md">
      <DialogHeader>
        <DialogTitle>Switch Organization</DialogTitle>
        <DialogDescription>Select an organization to switch to.</DialogDescription>
      </DialogHeader>
      <div class="space-y-2 py-4">
        <button
          v-for="org in organizationStore.organizations"
          :key="org.id"
          @click="handleSwitchOrganization(org.id!)"
          class="w-full flex items-center gap-3 px-4 py-3 rounded-lg border transition-colors text-left"
          :class="org.id === organizationStore.currentOrganizationId
            ? 'border-blue-500 bg-blue-50 text-blue-900'
            : 'border-gray-200 hover:bg-gray-50 text-gray-700'"
        >
          <svg class="w-5 h-5 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5M9 7h1m-1 4h1m4-4h1m-1 4h1m-5 10v-5a1 1 0 011-1h2a1 1 0 011 1v5m-4 0h4"/>
          </svg>
          <span class="flex-1 font-medium">{{ org.name }}</span>
          <svg v-if="org.id === organizationStore.currentOrganizationId" class="w-5 h-5 text-blue-600 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7"/>
          </svg>
        </button>
      </div>
    </DialogContent>
  </Dialog>
</template>
