<script setup lang="ts">
import { ref } from "vue";
import Dialog from "./ui/Dialog.vue";
import Button from "./ui/Button.vue";
import type { OrganizationMemberResponse } from "../api/generated/api-client";

interface Props {
  open?: boolean;
  member: OrganizationMemberResponse | null;
}

const props = withDefaults(defineProps<Props>(), {
  open: false,
});

const emit = defineEmits<{
  "update:open": [value: boolean];
  confirm: [];
}>();

const isLoading = ref(false);

const handleClose = () => {
  if (!isLoading.value) {
    emit("update:open", false);
  }
};

const handleConfirm = () => {
  isLoading.value = true;
  emit("confirm");
};

const memberEmail = () => {
  if (!props.member) return "";
  const email = props.member.Email || "";
  return email || "this member";
};
</script>

<template>
  <Dialog
    :open="open"
    title="Remove Member"
    description="Remove this member from the organization"
    max-width="sm"
    @update:open="handleClose"
  >
    <div class="space-y-4">
      <div class="flex items-start gap-3">
        <div
          class="flex-shrink-0 w-10 h-10 rounded-full bg-blue-50 flex items-center justify-center"
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
              d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z"
            />
          </svg>
        </div>
        <div class="flex-1">
          <p class="text-sm text-gray-700">
            Remove
            <span class="font-semibold">{{ memberEmail() }}</span> from this
            organization?
          </p>
          <p class="text-sm text-gray-500 mt-2">
            They will no longer have access to organization resources. You can
            always add them back later if needed.
          </p>
        </div>
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
            Removing...
          </span>
          <span v-else>Remove Member</span>
        </Button>
      </div>
    </template>
  </Dialog>
</template>
