<script setup lang="ts">
import { ref, watch } from "vue";
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter, Button } from "../ui/index";
import { toast } from "vue-sonner";

interface UserForm {
  username: string;
  email: string;
  firstName: string;
  lastName: string;
  password: string;
  confirmPassword: string;
  role: string;
}

interface Props {
  show: boolean;
}

interface Emits {
  (e: "close"): void;
  (e: "save", user: Omit<UserForm, "confirmPassword">): void;
}

const props = defineProps<Props>();
const emit = defineEmits<Emits>();

const user = ref<UserForm>({
  username: "",
  email: "",
  firstName: "",
  lastName: "",
  password: "",
  confirmPassword: "",
  role: "orgUser",
});

const formErrors = ref<Record<string, string>>({});

const validateForm = () => {
  formErrors.value = {};

  if (!user.value.username.trim()) {
    formErrors.value.username = "Username is required";
  } else if (user.value.username.length < 3) {
    formErrors.value.username = "Username must be at least 3 characters";
  }

  if (!user.value.email.trim()) {
    formErrors.value.email = "Email is required";
  } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(user.value.email)) {
    formErrors.value.email = "Invalid email format";
  }

  if (!user.value.firstName.trim()) {
    formErrors.value.firstName = "First name is required";
  }

  if (!user.value.lastName.trim()) {
    formErrors.value.lastName = "Last name is required";
  }

  if (!user.value.password) {
    formErrors.value.password = "Password is required";
  } else if (user.value.password.length < 8) {
    formErrors.value.password = "Password must be at least 8 characters";
  }

  if (!user.value.confirmPassword) {
    formErrors.value.confirmPassword = "Please confirm password";
  } else if (user.value.password !== user.value.confirmPassword) {
    formErrors.value.confirmPassword = "Passwords do not match";
  }

  if (!user.value.role) {
    formErrors.value.role = "Role is required";
  }

  return Object.keys(formErrors.value).length === 0;
};

const handleSave = () => {
  if (!validateForm()) {
    toast.error("Please fix the validation errors before submitting");
    return;
  }

  // Emit without confirmPassword
  const { confirmPassword, ...userToSave } = user.value;
  emit("save", userToSave);
};

const handleClose = () => {
  emit("close");
};

// Reset form when modal opens
const resetForm = () => {
  user.value = {
    username: "",
    email: "",
    firstName: "",
    lastName: "",
    password: "",
    confirmPassword: "",
    role: "orgUser",
  };
  formErrors.value = {};
};

// Watch for show prop changes to reset form
watch(
  () => props.show,
  (newVal) => {
    if (newVal) {
      resetForm();
    }
  },
);
</script>

<template>
  <Dialog :open="show" @update:open="(open) => !open && handleClose()">
    <DialogContent class="sm:max-w-lg">
      <DialogHeader>
        <DialogTitle>Create New User</DialogTitle>
      </DialogHeader>

      <div class="space-y-4">
        <div>
          <label class="block text-sm font-medium text-gray-700 mb-1"
            >Username *</label
          >
          <input
            v-model="user.username"
            type="text"
            class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
            :class="formErrors.username ? 'border-red-500' : ''"
            placeholder="Enter username"
          />
          <p v-if="formErrors.username" class="mt-1 text-sm text-red-600">
            {{ formErrors.username }}
          </p>
        </div>

        <div>
          <label class="block text-sm font-medium text-gray-700 mb-1"
            >Email *</label
          >
          <input
            v-model="user.email"
            type="email"
            class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
            :class="formErrors.email ? 'border-red-500' : ''"
            placeholder="user@example.com"
          />
          <p v-if="formErrors.email" class="mt-1 text-sm text-red-600">
            {{ formErrors.email }}
          </p>
        </div>

        <div class="grid grid-cols-2 gap-4">
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1"
              >First Name *</label
            >
            <input
              v-model="user.firstName"
              type="text"
              class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
              :class="formErrors.firstName ? 'border-red-500' : ''"
              placeholder="First name"
            />
            <p
              v-if="formErrors.firstName"
              class="mt-1 text-sm text-red-600"
            >
              {{ formErrors.firstName }}
            </p>
          </div>

          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1"
              >Last Name *</label
            >
            <input
              v-model="user.lastName"
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
            >Password *</label
          >
          <input
            v-model="user.password"
            type="password"
            class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
            :class="formErrors.password ? 'border-red-500' : ''"
            placeholder="Minimum 8 characters"
          />
          <p v-if="formErrors.password" class="mt-1 text-sm text-red-600">
            {{ formErrors.password }}
          </p>
        </div>

        <div>
          <label class="block text-sm font-medium text-gray-700 mb-1"
            >Confirm Password *</label
          >
          <input
            v-model="user.confirmPassword"
            type="password"
            class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
            :class="formErrors.confirmPassword ? 'border-red-500' : ''"
            placeholder="Re-enter password"
          />
          <p
            v-if="formErrors.confirmPassword"
            class="mt-1 text-sm text-red-600"
          >
            {{ formErrors.confirmPassword }}
          </p>
        </div>

        <div>
          <label class="block text-sm font-medium text-gray-700 mb-1"
            >Role *</label
          >
          <select
            v-model="user.role"
            class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
            :class="formErrors.role ? 'border-red-500' : ''"
          >
            <option value="orgUser">Organization User</option>
            <option value="orgAdmin">Organization Admin</option>
          </select>
          <p v-if="formErrors.role" class="mt-1 text-sm text-red-600">
            {{ formErrors.role }}
          </p>
          <p class="mt-1 text-xs text-gray-500">
            Organization Users have basic access, while Organization Admins
            can manage users and settings.
          </p>
        </div>
      </div>

      <DialogFooter>
        <Button @click="handleClose" variant="outline">Cancel</Button>
        <Button @click="handleSave" variant="default">Create User</Button>
      </DialogFooter>
    </DialogContent>
  </Dialog>
</template>
