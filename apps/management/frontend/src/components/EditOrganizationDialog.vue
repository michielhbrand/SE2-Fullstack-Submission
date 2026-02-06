<script setup lang="ts">
import { ref, reactive, watch } from "vue";
import Dialog from "./ui/Dialog.vue";
import Input from "./ui/Input.vue";
import Label from "./ui/Label.vue";
import Button from "./ui/Button.vue";
import { organizationService } from "../services/organizations";
import { toast } from "vue-sonner";
import type {
  OrganizationResponse,
  UpdateOrganizationRequest,
} from "../api/generated/api-client";
import { getErrorMessage } from "../lib/error-utils";

interface Props {
  open?: boolean;
  organization: OrganizationResponse | null;
}

const props = withDefaults(defineProps<Props>(), {
  open: false,
});

const emit = defineEmits<{
  "update:open": [value: boolean];
  success: [];
}>();

const isSubmitting = ref(false);

const formData = reactive<UpdateOrganizationRequest>({
  Name: "",
  TaxNumber: "",
  RegistrationNumber: "",
  Email: "",
  Phone: "",
  Website: "",
  Active: true,
  Address: {
    Street: "",
    City: "",
    State: "",
    PostalCode: "",
    Country: "",
  },
});

const loadOrganizationData = () => {
  if (props.organization) {
    formData.Name = props.organization.Name || "";
    formData.TaxNumber = props.organization.TaxNumber || "";
    formData.RegistrationNumber = props.organization.RegistrationNumber || "";
    formData.Email = props.organization.Email || "";
    formData.Phone = props.organization.Phone || "";
    formData.Website = props.organization.Website || "";
    formData.Active = props.organization.Active ?? true;
    if (props.organization.Address) {
      formData.Address = {
        Street: props.organization.Address.Street || "",
        City: props.organization.Address.City || "",
        State: props.organization.Address.State || "",
        PostalCode: props.organization.Address.PostalCode || "",
        Country: props.organization.Address.Country || "",
      };
    }
  }
};

const handleClose = () => {
  if (!isSubmitting.value) {
    emit("update:open", false);
  }
};

const handleSubmit = async () => {
  if (!formData.Name?.trim()) {
    toast.error("Organization name is required");
    return;
  }

  if (!formData.Address?.Street?.trim()) {
    toast.error("Street address is required");
    return;
  }

  if (!props.organization?.Id) {
    toast.error("Organization ID is missing");
    return;
  }

  isSubmitting.value = true;

  try {
    await organizationService.update(props.organization.Id, formData);
    toast.success("Organization updated successfully");
    emit("success");
    emit("update:open", false);
  } catch (error: any) {
    console.error("Failed to update organization:", error);
    toast.error(getErrorMessage(error, "Failed to update organization"));
  } finally {
    isSubmitting.value = false;
  }
};

// Load organization data when dialog is opened or organization changes
watch(
  [() => props.open, () => props.organization],
  ([newOpen]) => {
    if (newOpen) {
      loadOrganizationData();
    }
  },
  { immediate: true },
);
</script>

<template>
  <Dialog
    :open="open"
    title="Edit Organization"
    description="Update organization details"
    max-width="2xl"
    @update:open="(value) => emit('update:open', value)"
    @close="handleClose"
  >
    <form @submit.prevent="handleSubmit" class="space-y-6" id="edit-organization-form">
      <!-- Organization Details Section -->
      <div>
        <h3 class="text-sm font-semibold text-gray-900 mb-4">
          Organization Details
        </h3>
        <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
          <div class="md:col-span-2">
            <Label for="edit-name">
              Organization Name <span class="text-red-500">*</span>
            </Label>
            <Input
              id="edit-name"
              :model-value="formData.Name ?? ''"
              @update:model-value="(value) => (formData.Name = value as string)"
              type="text"
              placeholder="Enter organization name"
              :disabled="isSubmitting"
              required
            />
          </div>

          <div>
            <Label for="edit-taxNumber">Tax Number</Label>
            <Input
              id="edit-taxNumber"
              :model-value="formData.TaxNumber ?? ''"
              @update:model-value="(value) => (formData.TaxNumber = value as string)"
              type="text"
              placeholder="Enter tax number"
              :disabled="isSubmitting"
            />
          </div>

          <div>
            <Label for="edit-registrationNumber">Registration Number</Label>
            <Input
              id="edit-registrationNumber"
              :model-value="formData.RegistrationNumber ?? ''"
              @update:model-value="(value) => (formData.RegistrationNumber = value as string)"
              type="text"
              placeholder="Enter registration number"
              :disabled="isSubmitting"
            />
          </div>

          <div>
            <Label for="edit-email">Email</Label>
            <Input
              id="edit-email"
              :model-value="formData.Email ?? ''"
              @update:model-value="(value) => (formData.Email = value as string)"
              type="email"
              placeholder="Enter email address"
              :disabled="isSubmitting"
            />
          </div>

          <div>
            <Label for="edit-phone">Phone</Label>
            <Input
              id="edit-phone"
              :model-value="formData.Phone ?? ''"
              @update:model-value="(value) => (formData.Phone = value as string)"
              type="tel"
              placeholder="Enter phone number"
              :disabled="isSubmitting"
            />
          </div>

          <div class="md:col-span-2">
            <Label for="edit-website">Website</Label>
            <Input
              id="edit-website"
              :model-value="formData.Website ?? ''"
              @update:model-value="(value) => (formData.Website = value as string)"
              type="url"
              placeholder="https://example.com"
              :disabled="isSubmitting"
            />
          </div>

          <div class="md:col-span-2">
            <div class="flex items-center space-x-2">
              <input
                id="edit-active"
                type="checkbox"
                :checked="formData.Active ?? true"
                @change="(e) => (formData.Active = (e.target as HTMLInputElement).checked)"
                :disabled="isSubmitting"
                class="h-4 w-4 rounded border-gray-300 text-blue-600 focus:ring-blue-500"
              />
              <Label for="edit-active" class="cursor-pointer">
                Active (uncheck to deactivate organization)
              </Label>
            </div>
          </div>
        </div>
      </div>

      <!-- Address Section -->
      <div>
        <h3 class="text-sm font-semibold text-gray-900 mb-4">Address</h3>
        <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
          <div class="md:col-span-2">
            <Label for="edit-street">
              Street <span class="text-red-500">*</span>
            </Label>
            <Input
              id="edit-street"
              :model-value="formData.Address!.Street ?? ''"
              @update:model-value="(value) => (formData.Address!.Street = value as string)"
              type="text"
              placeholder="Enter street address"
              :disabled="isSubmitting"
              required
            />
          </div>

          <div>
            <Label for="edit-city">City</Label>
            <Input
              id="edit-city"
              :model-value="formData.Address!.City ?? ''"
              @update:model-value="(value) => (formData.Address!.City = value as string)"
              type="text"
              placeholder="Enter city"
              :disabled="isSubmitting"
            />
          </div>

          <div>
            <Label for="edit-state">State/Province</Label>
            <Input
              id="edit-state"
              :model-value="formData.Address!.State ?? ''"
              @update:model-value="(value) => (formData.Address!.State = value as string)"
              type="text"
              placeholder="Enter state or province"
              :disabled="isSubmitting"
            />
          </div>

          <div>
            <Label for="edit-postalCode">Postal Code</Label>
            <Input
              id="edit-postalCode"
              :model-value="formData.Address!.PostalCode ?? ''"
              @update:model-value="(value) => (formData.Address!.PostalCode = value as string)"
              type="text"
              placeholder="Enter postal code"
              :disabled="isSubmitting"
            />
          </div>

          <div>
            <Label for="edit-country">Country</Label>
            <Input
              id="edit-country"
              :model-value="formData.Address!.Country ?? ''"
              @update:model-value="(value) => (formData.Address!.Country = value as string)"
              type="text"
              placeholder="Enter country"
              :disabled="isSubmitting"
            />
          </div>
        </div>
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
        <Button
          type="submit"
          form="edit-organization-form"
          :disabled="isSubmitting"
        >
          <span v-if="isSubmitting">Updating...</span>
          <span v-else>Update Organization</span>
        </Button>
      </div>
    </template>
  </Dialog>
</template>
