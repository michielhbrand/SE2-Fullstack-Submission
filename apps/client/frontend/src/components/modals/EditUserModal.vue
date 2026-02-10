<script setup lang="ts">
import { ref, watch, computed } from "vue";
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter, Button, Spinner } from "../ui/index";
import { toast } from "vue-sonner";
import type { UserInfo } from "../../stores/auth";

interface Props {
  show: boolean;
  user: UserInfo | null;
  currentUserId: string | null;
  loading?: boolean;
}

interface Emits {
  (e: "close"): void;
  (e: "save", userData: { firstName: string; lastName: string; role: string; active: boolean }): void;
}

const props = withDefaults(defineProps<Props>(), {
  loading: false,
});
const emit = defineEmits<Emits>();

const formData = ref({
  firstName: "",
  lastName: "",
  role: "orgUser",
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
            <label class="block text-sm font-medium text-gray-700 mb-1"
              >First Name *</label
            >
            <input
              v-model="formData.firstName"
              type="text"
              class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
              :class="formErrors.firstName ? 'border-red-500' : ''"
              placeholder="First name"
            />
            <p v-if="formErrors.firstName" class="mt-1 text-sm text-red-600">
              {{ formErrors.firstName }}
            </p>
          </div>

          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1"
              >Last Name *</label
            >
            <input
              v-model="formData.lastName"
              type="text"
              class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
              :class="formErrors.lastName ? 'border-red-500' : ''"
              placeholder="Last name"
            />
            <p v-if="formErrors.lastName" class="mt-1 text-sm text-red-600">
              {{ formErrors.lastName }}
            </p>
          </div>
        </div>

        <div>
          <label class="block text-sm font-medium text-gray-700 mb-1"
            >Role *</label
          >
          <select
            v-model="formData.role"
            :disabled="!canChangeRole"
            class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 disabled:bg-gray-100 disabled:cursor-not-allowed"
            :class="formErrors.role ? 'border-red-500' : ''"
          >
            <option value="orgUser">Organization User</option>
            <option value="orgAdmin">Organization Admin</option>
          </select>
          <p v-if="formErrors.role" class="mt-1 text-sm text-red-600">
            {{ formErrors.role }}
          </p>
          <p v-if="!canChangeRole" class="mt-1 text-xs text-gray-500">
            You cannot change your own role
          </p>
        </div>

        <div>
          <label class="flex items-center space-x-2 cursor-pointer">
            <input
              v-model="formData.active"
              type="checkbox"
              class="w-4 h-4 text-blue-600 border-gray-300 rounded focus:ring-blue-500"
            />
            <span class="text-sm font-medium text-gray-700">Active User</span>
          </label>
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
