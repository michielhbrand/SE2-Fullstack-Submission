<script setup lang="ts">
import { onMounted, ref } from "vue";
import { useRouter } from "vue-router";
import { useAuthStore } from "../../stores/auth";
import { Button, Card, Spinner, Input, Label } from "../../components/ui/index";
import { toast } from "vue-sonner";
import { generatedClient } from "../../services/api";
import type { OrganizationResponse } from "../../api/generated/api-client";

const router = useRouter();
const authStore = useAuthStore();
const loading = ref(true);
const saving = ref(false);
const organization = ref<OrganizationResponse | null>(null);

// Form fields
const addressFirstLine = ref("");
const addressSecondLine = ref("");
const city = ref("");
const postalCode = ref("");

onMounted(async () => {
  if (!authStore.isAdmin) {
    router.push("/login");
    return;
  }

  await loadOrganization();
});

const loadOrganization = async () => {
  loading.value = true;
  try {
    // First, get all organizations to find the user's organization
    const organizations = await generatedClient.organization_GetOrganizations();
    if (organizations && organizations.length > 0) {
      const org = organizations[0];
      if (org && org.id) {
        // Now fetch the specific organization by ID for complete details
        const fullOrg = await generatedClient.organization_GetOrganization(org.id);
        if (fullOrg) {
          organization.value = fullOrg;
          
          // Populate form fields
          if (fullOrg.address) {
            addressFirstLine.value = fullOrg.address.firstLine || "";
            addressSecondLine.value = fullOrg.address.secondLine || "";
            city.value = fullOrg.address.city || "";
            postalCode.value = fullOrg.address.code || "";
          }
        }
      }
    }
  } catch (err) {
    toast.error("Failed to load organization details");
    console.error("Error loading organization:", err);
  } finally {
    loading.value = false;
  }
};

const handleSave = async () => {
  if (!organization.value || !organization.value.id) return;

  // Validation
  if (!addressFirstLine.value.trim()) {
    toast.error("Address line 1 is required");
    return;
  }
  if (!city.value.trim()) {
    toast.error("City is required");
    return;
  }
  if (!postalCode.value.trim()) {
    toast.error("Postal code is required");
    return;
  }

  saving.value = true;
  try {
    // Note: The API expects addressId, not the full address object
    // For now, we'll show a message that this feature needs backend support
    toast.info("Address editing requires additional backend API support");
    
    // When the backend supports updating address inline, use:
    // await generatedClient.organization_UpdateOrganization(organization.value.id, {
    //   addressId: organization.value.address?.id,
    // });
    
  } catch (err: any) {
    console.error("Error updating organization:", err);
    if (err?.response?.data?.message) {
      toast.error(err.response.data.message);
    } else {
      toast.error("Failed to update organization details");
    }
  } finally {
    saving.value = false;
  }
};

const handleReset = () => {
  if (organization.value?.address) {
    addressFirstLine.value = organization.value.address.firstLine || "";
    addressSecondLine.value = organization.value.address.secondLine || "";
    city.value = organization.value.address.city || "";
    postalCode.value = organization.value.address.code || "";
  }
};
</script>

<template>
  <div class="p-8">
    <!-- Edit Organization Card -->
    <Card class="p-6">
      <div class="mb-6">
        <h2 class="text-2xl font-semibold text-gray-900">Edit Organization</h2>
        <p class="mt-1 text-sm text-gray-500">Update your organization's address and details</p>
      </div>

      <!-- Loading State -->
      <div v-if="loading" class="flex justify-center items-center py-12">
        <Spinner class="h-8 w-8 text-gray-900" />
      </div>

      <!-- Form -->
      <div v-else-if="organization" class="space-y-6">
        <!-- Organization Name (Read-only) -->
        <div>
          <Label for="org-name">Organization Name</Label>
          <Input
            id="org-name"
            :value="organization.name"
            disabled
            class="mt-1 bg-gray-100 cursor-not-allowed"
          />
          <p class="mt-1 text-xs text-gray-500">Organization name cannot be changed</p>
        </div>

        <!-- Address Section -->
        <div class="border-t pt-6">
          <h3 class="text-lg font-medium text-gray-900 mb-4">Address Information</h3>
          
          <div class="space-y-4">
            <!-- Address Line 1 -->
            <div>
              <Label for="address-line-1">Address Line 1 *</Label>
              <Input
                id="address-line-1"
                v-model="addressFirstLine"
                placeholder="123 Main Street"
                class="mt-1"
              />
            </div>

            <!-- Address Line 2 -->
            <div>
              <Label for="address-line-2">Address Line 2</Label>
              <Input
                id="address-line-2"
                v-model="addressSecondLine"
                placeholder="Suite 100 (optional)"
                class="mt-1"
              />
            </div>

            <!-- City and Postal Code -->
            <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div>
                <Label for="city">City *</Label>
                <Input
                  id="city"
                  v-model="city"
                  placeholder="New York"
                  class="mt-1"
                />
              </div>

              <div>
                <Label for="postal-code">Postal Code *</Label>
                <Input
                  id="postal-code"
                  v-model="postalCode"
                  placeholder="10001"
                  class="mt-1"
                />
              </div>
            </div>
          </div>
        </div>

        <!-- Action Buttons -->
        <div class="flex items-center justify-end gap-3 pt-6 border-t">
          <Button
            @click="handleReset"
            variant="outline"
            :disabled="saving"
          >
            Reset
          </Button>
          <Button
            @click="handleSave"
            class="bg-gray-900 hover:bg-gray-800 text-white"
            :disabled="saving"
          >
            <Spinner v-if="saving" class="h-4 w-4 mr-2" />
            {{ saving ? "Saving..." : "Save Changes" }}
          </Button>
        </div>
      </div>

      <!-- Error State -->
      <div v-else class="text-center py-12">
        <svg
          class="mx-auto h-12 w-12 text-gray-400"
          fill="none"
          stroke="currentColor"
          viewBox="0 0 24 24"
        >
          <path
            stroke-linecap="round"
            stroke-linejoin="round"
            stroke-width="2"
            d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
          />
        </svg>
        <h3 class="mt-2 text-sm font-medium text-gray-900">No organization found</h3>
        <p class="mt-1 text-sm text-gray-500">
          Unable to load organization details.
        </p>
      </div>
    </Card>

    <!-- Info Card -->
    <Card class="mt-6 p-6 bg-amber-50 border-amber-200">
      <div class="flex">
        <div class="flex-shrink-0">
          <svg
            class="h-5 w-5 text-amber-700"
            fill="currentColor"
            viewBox="0 0 20 20"
          >
            <path
              fill-rule="evenodd"
              d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a1 1 0 000 2v3a1 1 0 001 1h1a1 1 0 100-2v-3a1 1 0 00-1-1H9z"
              clip-rule="evenodd"
            />
          </svg>
        </div>
        <div class="ml-3">
          <h3 class="text-sm font-medium text-amber-900">
            Organization Information
          </h3>
          <div class="mt-2 text-sm text-amber-700">
            <ul class="list-disc list-inside space-y-1">
              <li>Organization name cannot be changed through this interface</li>
              <li>Address information is used on invoices and quotes</li>
              <li>All fields marked with * are required</li>
            </ul>
          </div>
        </div>
      </div>
    </Card>
  </div>
</template>
