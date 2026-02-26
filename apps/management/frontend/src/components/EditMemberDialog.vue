<script setup lang="ts">
import { ref, reactive, watch } from "vue";
import Dialog from "./ui/Dialog.vue";
import Input from "./ui/Input.vue";
import Label from "./ui/Label.vue";
import Button from "./ui/Button.vue";
import { toast } from "vue-sonner";
import { apiClient } from "../api/client";
import type {
  UpdateUserRequest,
  OrganizationMemberResponse,
} from "../api/generated/api-client";
import { getErrorMessage } from "../lib/error-utils";

interface Props {
  open?: boolean;
  member: OrganizationMemberResponse | null;
}

const props = withDefaults(defineProps<Props>(), {
  open: false,
});

const emit = defineEmits<{
  "update:open": [value: boolean];
  success: [];
}>();

const isSubmitting = ref(false);

const formData = reactive<UpdateUserRequest>({
  FirstName: "",
  LastName: "",
  Active: true,
  Role: "OrgUser",
});

const resetForm = () => {
  if (props.member) {
    formData.FirstName = props.member.FirstName ?? "";
    formData.LastName = props.member.LastName ?? "";
    formData.Active = props.member.Active ?? true;
    formData.Role = props.member.Role ?? "OrgUser";
  } else {
    formData.FirstName = "";
    formData.LastName = "";
    formData.Active = true;
    formData.Role = "OrgUser";
  }
};

const handleClose = () => {
  if (!isSubmitting.value) {
    emit("update:open", false);
  }
};

const handleSubmit = async () => {
  if (!formData.FirstName?.trim()) {
    toast.error("First name is required");
    return;
  }

  if (!formData.LastName?.trim()) {
    toast.error("Last name is required");
    return;
  }

  if (!props.member?.UserId) {
    toast.error("Invalid member data");
    return;
  }

  isSubmitting.value = true;

  try {
    await apiClient.updateUser(props.member.UserId, formData);
    toast.success("Member updated successfully");
    emit("success");
    emit("update:open", false);
  } catch (error: any) {
    toast.error(getErrorMessage(error, "Failed to update member"));
  } finally {
    isSubmitting.value = false;
  }
};

// Reset form when dialog is opened or member changes
watch(
  () => [props.open, props.member],
  ([newOpen]) => {
    if (newOpen) {
      resetForm();
    }
  },
  { deep: true },
);
</script>

<template>
  <Dialog
    :open="open"
    title="Edit Member"
    description="Update member information."
    max-width="lg"
    @update:open="(value) => emit('update:open', value)"
    @close="handleClose"
  >
    <form @submit.prevent="handleSubmit" class="space-y-4">
      <div>
        <Label for="email">Email</Label>
        <Input
          id="email"
          :model-value="member?.Email ?? ''"
          type="email"
          disabled
          class="bg-gray-50"
        />
        <p class="text-xs text-gray-500 mt-1">Email cannot be changed</p>
      </div>

      <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
        <div>
          <Label for="firstName">
            First Name <span class="text-red-500">*</span>
          </Label>
          <Input
            id="firstName"
            :model-value="formData.FirstName ?? ''"
            @update:model-value="
              (value) => (formData.FirstName = value as string)
            "
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
            @update:model-value="
              (value) => (formData.LastName = value as string)
            "
            type="text"
            placeholder="Enter last name"
            :disabled="isSubmitting"
            required
          />
        </div>
      </div>

      <div>
        <Label for="active">Status</Label>
        <select
          id="active"
          v-model="formData.Active"
          class="w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
          :disabled="isSubmitting"
        >
          <option :value="true">Active</option>
          <option :value="false">Inactive</option>
        </select>
        <p class="text-xs text-gray-500 mt-1">
          Inactive users cannot access the system
        </p>
      </div>

      <div>
        <Label for="role"> Role <span class="text-red-500">*</span> </Label>
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
          OrgUser: Regular member | OrgAdmin: Administrative privileges
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
          <span v-if="isSubmitting">Updating...</span>
          <span v-else>Update Member</span>
        </Button>
      </div>
    </template>
  </Dialog>
</template>
