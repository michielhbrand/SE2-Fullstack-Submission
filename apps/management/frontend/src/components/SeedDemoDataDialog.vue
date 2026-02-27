<script setup lang="ts">
import Dialog from "./ui/Dialog.vue";
import Button from "./ui/Button.vue";

interface Props {
  open?: boolean;
  organizationName: string;
  isLoading?: boolean;
}

const props = withDefaults(defineProps<Props>(), {
  open: false,
  isLoading: false,
});

const emit = defineEmits<{
  "update:open": [value: boolean];
  confirm: [];
}>();

const handleClose = () => {
  if (!props.isLoading) {
    emit("update:open", false);
  }
};

const handleConfirm = () => {
  emit("confirm");
};
</script>

<template>
  <Dialog
    :open="open"
    title="Generate Demo Data"
    description="This will generate a realistic dataset for demonstration purposes"
    max-width="sm"
    @update:open="handleClose"
  >
    <div class="space-y-4">
      <div class="flex items-start gap-3">
        <div
          class="shrink-0 w-10 h-10 rounded-full bg-blue-50 flex items-center justify-center"
        >
          <svg
            class="h-6 w-6 text-blue-600"
            fill="none"
            stroke="currentColor"
            viewBox="0 0 24 24"
          >
            <path
              stroke-linecap="round"
              stroke-linejoin="round"
              stroke-width="2"
              d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
            />
          </svg>
        </div>
        <div class="flex-1">
          <p class="text-sm font-medium text-gray-900 mb-2">
            This will create the following demo data for
            <span class="font-semibold">{{ organizationName }}</span
            >:
          </p>
          <ul class="text-sm text-gray-600 space-y-1 list-disc list-inside">
            <li>10 clients (6 companies, 4 individuals)</li>
            <li>
              8 paid workflows showing revenue growth over 6 months (~R825k
              total)
            </li>
            <li>3 overdue invoices (45, 20, and 60 days overdue)</li>
            <li>
              6 in-progress workflows covering all statuses: Draft, Pending
              Approval, Approved, Rejected, Invoice Created, Sent for Payment
            </li>
            <li>2 terminal workflows (Cancelled and Terminated)</li>
          </ul>
        </div>
      </div>

      <div
        class="flex items-start gap-2 rounded-md bg-amber-50 border border-amber-200 p-3"
      >
        <svg
          class="h-4 w-4 text-amber-600 mt-0.5 shrink-0"
          fill="none"
          stroke="currentColor"
          viewBox="0 0 24 24"
        >
          <path
            stroke-linecap="round"
            stroke-linejoin="round"
            stroke-width="2"
            d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z"
          />
        </svg>
        <p class="text-xs text-amber-700">
          This action adds data to the existing organization. Run it only once
          per demo environment. Existing clients, invoices, or workflows will
          not be removed.
        </p>
      </div>
    </div>

    <template #footer>
      <div class="flex justify-end gap-3">
        <Button variant="outline" @click="handleClose" :disabled="isLoading">
          Cancel
        </Button>
        <Button @click="handleConfirm" :disabled="isLoading">
          <span v-if="isLoading" class="flex items-center gap-2">
            <div
              class="inline-block h-4 w-4 animate-spin rounded-full border-2 border-solid border-white border-r-transparent"
            ></div>
            Generating...
          </span>
          <span v-else>Generate Demo Data</span>
        </Button>
      </div>
    </template>
  </Dialog>
</template>
