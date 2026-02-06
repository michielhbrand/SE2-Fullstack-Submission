<script setup lang="ts">
import { ref, reactive, watch } from "vue";
import Dialog from "./ui/Dialog.vue";
import Input from "./ui/Input.vue";
import Label from "./ui/Label.vue";
import Button from "./ui/Button.vue";
import { toast } from "vue-sonner";
import { apiClient } from "../api/client";
import type { CreateOrganizationMemberRequest } from "../api/generated/api-client";
import { getErrorMessage } from "../lib/error-utils";

interface Props {
  open?: boolean;
  organizationId: number;
}

const props = withDefaults(defineProps<Props>(), {
  open: false,
});

const emit = defineEmits<{
  "update:open": [value: boolean];
  success: [];
}>();

const isSubmitting = ref(false);

const formData = reactive<CreateOrganizationMemberRequest>({
  Email: "",
  FirstName: "",
  LastName: "",
  Role: "OrgUser",
});

const resetForm = () => {
  formData.Email = "";
  formData.FirstName = "";
  formData.LastName = "";
  formData.Role = "OrgUser";
};

const handleClose = () => {
  if (!isSubmitting.value) {
    emit("update:open", false);
    resetForm();
  }
};

const handleSubmit = async () => {
  if (!formData.Email?.trim()) {
    toast.error("Email is required");
    return;
  }

  if (!formData.FirstName?.trim()) {
    toast.error("First name is required");
    return;
  }

  if (!formData.LastName?.trim()) {
    toast.error("Last name is required");
    return;
  }

  if (!formData.Role?.trim()) {
    toast.error("Role is required");
    return;
  }

  isSubmitting.value = true;

  try {
    await apiClient.addUserToOrganization(props.organizationId, formData);
    toast.success("Member added successfully");
    emit("success");
    emit("update:open", false);
    resetForm();
  } catch (error: any) {
    console.error("Failed to add member:", error);
    toast.error(getErrorMessage(error, "Failed to add member"));
  } finally {
    isSubmitting.value = false;
  }
};

// Reset form when dialog is opened
watch(
  () => props.open,
  (newValue) => {
    if (newValue) {
      resetForm();
    }
  },
);
</script>

<template>
  <Dialog
    :open="open"
    title="Add Organization Member"
    description="Add a new member to this organization. If the user doesn't exist, they will be created automatically."
    max-width="lg"
    @update:open="(value) => emit('update:open', value)"
    @close="handleClose"
  >
    <form @submit.prevent="handleSubmit" class="space-y-4">
      <div>
        <Label for="email">
          Email <span class="text-red-500">*</span>
        </Label>
        <Input
          id="email"
          v-model="formData.Email"
          type="email"
          placeholder="user@example.com"
          :disabled="isSubmitting"
          required
        />
        <p class="text-xs text-gray-500 mt-1">
          If this email doesn't exist, a new user will be created with this email as their password.
        </p>
      </div>

      <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
        <div>
          <Label for="firstName">
            First Name <span class="text-red-500">*</span>
          </Label>
          <Input
            id="firstName"
            :model-value="formData.FirstName ?? ''"
            @update:model-value="(value) => (formData.FirstName = value as string)"
            type="text"
            placeholder="Enter first name"
            :disabled="isSubmitting"
            required
          />
        </div>

        <div>
          <Label for="lastName">
            Last Name <span class="text-red-500">*</span>
          </Label>
          <Input
            id="lastName"
            :model-value="formData.LastName ?? ''"
            @update:model-value="(value) => (formData.LastName = value as string)"
            type="text"
            placeholder="Enter last name"
            :disabled="isSubmitting"
            required
          />
        </div>
      </div>

      <div>
        <Label for="role">
          Role <span class="text-red-500">*</span>
        </Label>
        <select
          id="role"
          v-model="formData.Role"
          class="w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
          :disabled="isSubmitting"
          required
        >
          <option value="OrgUser">Organization User</option>
          <option value="OrgAdmin">Organization Admin</option>
        </select>
        <p class="text-xs text-gray-500 mt-1">
          orgUser: Regular member | orgAdmin: Administrative privileges
        </p>
      </div>
    </form>

    <template #footer>
      <div class="flex justify-end gap-3">
        <Button
          variant="outline"
          @click="handleClose"
          :disabled="isSubmitting"
          type="button"
        >
          Cancel
        </Button>
        <Button @click="handleSubmit" :disabled="isSubmitting" type="submit">
          <span v-if="isSubmitting">Adding...</span>
          <span v-else>Add Member</span>
        </Button>
      </div>
    </template>
  </Dialog>
</template>
