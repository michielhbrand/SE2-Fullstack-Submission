<script setup lang="ts">
import { ref, watch, computed } from "vue";
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter, Button, Spinner, Checkbox, Label, Input, Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "../ui/index";
import { toast } from "vue-sonner";
import type { UserInfo } from "../../stores/auth";
import type { UserRole } from "../../api/generated/api-client";

interface Props {
  show: boolean;
  user: UserInfo | null;
  currentUserId: string | null;
  loading?: boolean;
}

interface Emits {
  (e: "close"): void;
  (e: "save", userData: { firstName: string; lastName: string; role: UserRole; active: boolean }): void;
}

const props = withDefaults(defineProps<Props>(), {
  loading: false,
});
const emit = defineEmits<Emits>();

const formData = ref({
  firstName: "",
  lastName: "",
  role: "orgUser" as UserRole,
  active: true,
});

const formErrors = ref<Record<string, string>>({});

const isCurrentUser = computed(() => {
  return props.user?.id === props.currentUserId;
});

const isAdmin = computed(() => {
  return props.user?.roles?.includes("orgAdmin") || props.user?.roles?.includes("systemAdmin") || false;
});

const canChangeRole = computed(() => {
  // Cannot demote yourself if you're an admin
  return !(isCurrentUser.value && isAdmin.value);
});

const validateForm = () => {
  formErrors.value = {};

  if (!formData.value.firstName.trim()) {
    formErrors.value.firstName = "First name is required";
  }

  if (!formData.value.lastName.trim()) {
    formErrors.value.lastName = "Last name is required";
  }

  if (!formData.value.role) {
    formErrors.value.role = "Role is required";
  }

  // Prevent self-demotion
  if (isCurrentUser.value && isAdmin.value && formData.value.role === "orgUser") {
    formErrors.value.role = "You cannot demote yourself";
  }

  return Object.keys(formErrors.value).length === 0;
};

const handleSave = () => {
  if (!validateForm()) {
    toast.error("Please fix the validation errors before submitting");
    return;
  }

  emit("save", {
    firstName: formData.value.firstName,
    lastName: formData.value.lastName,
    role: formData.value.role,
    active: formData.value.active,
  });
};

const handleClose = () => {
  emit("close");
};

const getUserRole = (user: UserInfo): string => {
  if (user.roles?.includes("systemAdmin")) return "systemAdmin";
  if (user.roles?.includes("orgAdmin")) return "orgAdmin";
  return "orgUser";
};

// Reset form when modal opens or user changes
watch(
  () => [props.show, props.user] as const,
  ([newShow, newUser]) => {
    if (newShow && newUser && typeof newUser !== 'boolean') {
      formData.value = {
        firstName: newUser.firstName || "",
        lastName: newUser.lastName || "",
        role: getUserRole(newUser),
        active: newUser.enabled || true,
      };
      formErrors.value = {};
    }
  },
  { immediate: true },
);
</script>

<template>
  <Dialog :open="show" @update:open="(open) => !open && handleClose()">
    <DialogContent class="sm:max-w-lg">
      <DialogHeader>
        <DialogTitle>Edit User Details</DialogTitle>
      </DialogHeader>

      <div v-if="user" class="space-y-4">
        <div class="bg-gray-50 p-3 rounded-lg">
          <p class="text-sm text-gray-600">
            <strong>Username:</strong> {{ user.username || user.email }}
          </p>
          <p class="text-sm text-gray-600">
            <strong>Email:</strong> {{ user.email }}
          </p>
        </div>

        <div class="grid grid-cols-2 gap-4">
          <div>
            <Label for="edit-firstName">First Name *</Label>
            <Input
              id="edit-firstName"
              v-model="formData.firstName"
              type="text"
              :class="formErrors.firstName ? 'border-red-500' : ''"
              placeholder="First name"
            />
            <p v-if="formErrors.firstName" class="mt-1 text-sm text-red-600">
              {{ formErrors.firstName }}
            </p>
          </div>

          <div>
            <Label for="edit-lastName">Last Name *</Label>
            <Input
              id="edit-lastName"
              v-model="formData.lastName"
              type="text"
              :class="formErrors.lastName ? 'border-red-500' : ''"
              placeholder="Last name"
            />
            <p v-if="formErrors.lastName" class="mt-1 text-sm text-red-600">
              {{ formErrors.lastName }}
            </p>
          </div>
        </div>

        <div>
          <Label for="edit-role">Role *</Label>
          <Select v-model="formData.role" :disabled="!canChangeRole">
            <SelectTrigger id="edit-role" :class="formErrors.role ? 'border-red-500' : ''">
              <SelectValue placeholder="Select role" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="orgUser">Organization User</SelectItem>
              <SelectItem value="orgAdmin">Organization Admin</SelectItem>
            </SelectContent>
          </Select>
          <p v-if="formErrors.role" class="mt-1 text-sm text-red-600">
            {{ formErrors.role }}
          </p>
          <p v-if="!canChangeRole" class="mt-1 text-xs text-gray-500">
            You cannot change your own role
          </p>
        </div>

        <div>
          <div class="flex items-center space-x-2">
            <Checkbox id="active-user" v-model:checked="formData.active" />
            <Label for="active-user" class="text-sm font-medium text-gray-700 cursor-pointer">
              Active User
            </Label>
          </div>
          <p class="mt-1 text-xs text-gray-500">
            Inactive users cannot log in to the system
          </p>
        </div>
      </div>

      <DialogFooter>
        <Button @click="handleClose" :disabled="loading" variant="outline">
          Cancel
        </Button>
        <Button @click="handleSave" :disabled="loading" variant="default">
          <Spinner v-if="loading" class="h-4 w-4 mr-2" />
          {{ loading ? "Saving..." : "Save Changes" }}
        </Button>
      </DialogFooter>
    </DialogContent>
  </Dialog>
</template>
