<script setup lang="ts">
import { ref, reactive, watch } from "vue";
import Dialog from "./ui/Dialog.vue";
import Input from "./ui/Input.vue";
import Label from "./ui/Label.vue";
import Button from "./ui/Button.vue";
import { organizationService } from "../services/organizations";
import { paymentPlanService } from "../services/paymentPlans";
import { toast } from "vue-sonner";
import type { CreateOrganizationRequest, PaymentPlanResponse } from "../api/generated/api-client";
import { getErrorMessage } from "../lib/error-utils";

interface Props {
  open?: boolean;
}

const props = withDefaults(defineProps<Props>(), {
  open: false,
});

const emit = defineEmits<{
  "update:open": [value: boolean];
  success: [];
}>();

const isSubmitting = ref(false);
const paymentPlans = ref<PaymentPlanResponse[]>([]);
const selectedPlanId = ref<number>(1);

const fetchPlans = async () => {
  try {
    paymentPlans.value = await paymentPlanService.getAll();
    if (paymentPlans.value.length > 0 && !selectedPlanId.value) {
      selectedPlanId.value = paymentPlans.value[0]!.Id!;
    }
  } catch {
    // Plans will fall back to default (Basic = 1)
  }
};

const formData = reactive<CreateOrganizationRequest>({
  Name: "",
  TaxNumber: "",
  RegistrationNumber: "",
  Email: "",
  Phone: "",
  Website: "",
  Address: {
    Street: "",
    City: "",
    State: "",
    PostalCode: "",
    Country: "",
  },
});

const resetForm = () => {
  formData.Name = "";
  formData.TaxNumber = "";
  formData.RegistrationNumber = "";
  formData.Email = "";
  formData.Phone = "";
  formData.Website = "";
  if (formData.Address) {
    formData.Address.Street = "";
    formData.Address.City = "";
    formData.Address.State = "";
    formData.Address.PostalCode = "";
    formData.Address.Country = "";
  }
};

const handleClose = () => {
  if (!isSubmitting.value) {
    emit("update:open", false);
    resetForm();
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

  isSubmitting.value = true;

  try {
    await organizationService.create({ ...formData, PaymentPlanId: selectedPlanId.value });
    toast.success("Organization created successfully");
    emit("success");
    emit("update:open", false);
    resetForm();
  } catch (error: any) {
    console.error("Failed to create organization:", error);
    toast.error(getErrorMessage(error, "Failed to create organization"));
  } finally {
    isSubmitting.value = false;
  }
};

// Reset form and fetch plans when dialog is opened
watch(
  () => props.open,
  (newValue) => {
    if (newValue) {
      resetForm();
      fetchPlans();
    }
  },
);
</script>

<template>
  <Dialog
    :open="open"
    title="Create New Organization"
    description="Add a new organization to the system"
    max-width="2xl"
    @update:open="(value) => emit('update:open', value)"
    @close="handleClose"
  >
    <form @submit.prevent="handleSubmit" class="space-y-6">
      <!-- Organization Details Section -->
      <div>
        <h3 class="text-sm font-semibold text-gray-900 mb-4">
          Organization Details
        </h3>
        <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
          <div class="md:col-span-2">
            <Label for="name">
              Organization Name <span class="text-red-500">*</span>
            </Label>
            <Input
              id="name"
              v-model="formData.Name"
              type="text"
              placeholder="Enter organization name"
              :disabled="isSubmitting"
              required
            />
          </div>

          <div>
            <Label for="taxNumber">Tax Number</Label>
            <Input
              id="taxNumber"
              :model-value="formData.TaxNumber ?? ''"
              @update:model-value="(value) => (formData.TaxNumber = value as string)"
              type="text"
              placeholder="Enter tax number"
              :disabled="isSubmitting"
            />
          </div>

          <div>
            <Label for="registrationNumber">Registration Number</Label>
            <Input
              id="registrationNumber"
              :model-value="formData.RegistrationNumber ?? ''"
              @update:model-value="(value) => (formData.RegistrationNumber = value as string)"
              type="text"
              placeholder="Enter registration number"
              :disabled="isSubmitting"
            />
          </div>

          <div>
            <Label for="email">Email</Label>
            <Input
              id="email"
              :model-value="formData.Email ?? ''"
              @update:model-value="(value) => (formData.Email = value as string)"
              type="email"
              placeholder="Enter email address"
              :disabled="isSubmitting"
            />
          </div>

          <div>
            <Label for="phone">Phone</Label>
            <Input
              id="phone"
              :model-value="formData.Phone ?? ''"
              @update:model-value="(value) => (formData.Phone = value as string)"
              type="tel"
              placeholder="Enter phone number"
              :disabled="isSubmitting"
            />
          </div>

          <div class="md:col-span-2">
            <Label for="website">Website</Label>
            <Input
              id="website"
              :model-value="formData.Website ?? ''"
              @update:model-value="(value) => (formData.Website = value as string)"
              type="url"
              placeholder="https://example.com"
              :disabled="isSubmitting"
            />
          </div>
        </div>
      </div>

      <!-- Payment Plan Section -->
      <div>
        <h3 class="text-sm font-semibold text-gray-900 mb-4">
          Payment Plan <span class="text-red-500">*</span>
        </h3>
        <div class="grid grid-cols-1 md:grid-cols-3 gap-3">
          <label
            v-for="plan in paymentPlans"
            :key="plan.Id"
            :class="[
              'flex flex-col gap-1 border rounded-lg p-4 cursor-pointer transition-colors',
              selectedPlanId === plan.Id
                ? 'border-blue-500 bg-blue-50'
                : 'border-gray-200 hover:border-gray-400',
            ]"
          >
            <input
              type="radio"
              :value="plan.Id"
              v-model="selectedPlanId"
              class="sr-only"
            />
            <span class="font-semibold text-gray-900">{{ plan.Name }}</span>
            <span class="text-sm text-gray-600">
              {{ plan.MaxUsers === -1 ? 'Unlimited users' : `Up to ${plan.MaxUsers} users` }}
            </span>
            <span class="text-sm font-medium text-blue-700">R{{ plan.MonthlyCostRand?.toLocaleString('en-ZA') }}/mo</span>
          </label>
        </div>
      </div>

      <!-- Address Section -->
      <div>
        <h3 class="text-sm font-semibold text-gray-900 mb-4">Address</h3>
        <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
          <div class="md:col-span-2">
            <Label for="street">
              Street <span class="text-red-500">*</span>
            </Label>
            <Input
              id="street"
              v-model="formData.Address!.Street"
              type="text"
              placeholder="Enter street address"
              :disabled="isSubmitting"
              required
            />
          </div>

          <div>
            <Label for="city">City</Label>
            <Input
              id="city"
              :model-value="formData.Address!.City ?? ''"
              @update:model-value="(value) => (formData.Address!.City = value as string)"
              type="text"
              placeholder="Enter city"
              :disabled="isSubmitting"
            />
          </div>

          <div>
            <Label for="state">State/Province</Label>
            <Input
              id="state"
              :model-value="formData.Address!.State ?? ''"
              @update:model-value="(value) => (formData.Address!.State = value as string)"
              type="text"
              placeholder="Enter state or province"
              :disabled="isSubmitting"
            />
          </div>

          <div>
            <Label for="postalCode">Postal Code</Label>
            <Input
              id="postalCode"
              :model-value="formData.Address!.PostalCode ?? ''"
              @update:model-value="(value) => (formData.Address!.PostalCode = value as string)"
              type="text"
              placeholder="Enter postal code"
              :disabled="isSubmitting"
            />
          </div>

          <div>
            <Label for="country">Country</Label>
            <Input
              id="country"
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
        <Button @click="handleSubmit" :disabled="isSubmitting" type="submit">
          <span v-if="isSubmitting">Creating...</span>
          <span v-else>Create Organization</span>
        </Button>
      </div>
    </template>
  </Dialog>
</template>
