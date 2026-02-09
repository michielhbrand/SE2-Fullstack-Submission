<script setup lang="ts">
import { ref, onMounted, computed } from "vue";
import { useRouter, useRoute } from "vue-router";
import { toast } from "vue-sonner";
import Button from "../components/ui/Button.vue";
import Card from "../components/ui/Card.vue";
import EditOrganizationDialog from "../components/EditOrganizationDialog.vue";
import AddMemberDialog from "../components/AddMemberDialog.vue";
import EditMemberDialog from "../components/EditMemberDialog.vue";
import RemoveMemberDialog from "../components/RemoveMemberDialog.vue";
import { organizationService } from "../services/organizations";
import { apiClient } from "../api/client";
import type {
  OrganizationResponse,
  OrganizationMemberResponse,
} from "../api/generated/api-client";
import { getErrorMessage } from "../lib/error-utils";

const router = useRouter();
const route = useRoute();
const organization = ref<OrganizationResponse | null>(null);
const members = ref<OrganizationMemberResponse[]>([]);
const isLoading = ref(false);
const isLoadingMembers = ref(false);
const isEditDialogOpen = ref(false);
const isAddMemberDialogOpen = ref(false);
const isEditMemberDialogOpen = ref(false);
const isRemoveMemberDialogOpen = ref(false);
const selectedMember = ref<OrganizationMemberResponse | null>(null);
const memberToRemove = ref<OrganizationMemberResponse | null>(null);

const organizationId = computed(() => {
  const id = route.params.id;
  return typeof id === "string" ? parseInt(id, 10) : null;
});

const fetchOrganization = async () => {
  if (!organizationId.value) {
    toast.error("Invalid organization ID");
    router.push("/organizations");
    return;
  }

  isLoading.value = true;
  try {
    const org = await organizationService.getById(organizationId.value);
    if (!org) {
      toast.error("Organization not found");
      router.push("/organizations");
      return;
    }
    organization.value = org;
  } catch (error: any) {
    console.error("Failed to fetch organization:", error);
    toast.error(getErrorMessage(error, "Failed to fetch organization"));
    router.push("/organizations");
  } finally {
    isLoading.value = false;
  }
};

const fetchMembers = async () => {
  if (!organizationId.value) return;

  isLoadingMembers.value = true;
  try {
    members.value = await apiClient.getOrganizationMembers(
      organizationId.value,
    );
  } catch (error: any) {
    console.error("Failed to fetch members:", error);
    toast.error(getErrorMessage(error, "Failed to fetch members"));
  } finally {
    isLoadingMembers.value = false;
  }
};

const openEditDialog = () => {
  isEditDialogOpen.value = true;
};

const openAddMemberDialog = () => {
  isAddMemberDialogOpen.value = true;
};

const openEditMemberDialog = (member: OrganizationMemberResponse) => {
  selectedMember.value = member;
  isEditMemberDialogOpen.value = true;
};

const handleOrganizationUpdated = () => {
  fetchOrganization();
};

const handleMemberAdded = () => {
  fetchMembers();
};

const handleMemberUpdated = () => {
  fetchMembers();
};

const openRemoveMemberDialog = (member: OrganizationMemberResponse) => {
  memberToRemove.value = member;
  isRemoveMemberDialogOpen.value = true;
};

const handleRemoveMember = async () => {
  if (!organizationId.value || !memberToRemove.value?.UserId) return;

  try {
    await apiClient.removeUserFromOrganization(
      organizationId.value,
      memberToRemove.value.UserId,
    );
    toast.success("Member removed successfully");
    isRemoveMemberDialogOpen.value = false;
    memberToRemove.value = null;
    fetchMembers();
  } catch (error: any) {
    console.error("Failed to remove member:", error);
    toast.error(getErrorMessage(error, "Failed to remove member"));
    isRemoveMemberDialogOpen.value = false;
    memberToRemove.value = null;
  }
};

const goBack = () => {
  router.push("/organizations");
};

onMounted(() => {
  fetchOrganization();
  fetchMembers();
});
</script>

<template>
  <div class="min-h-screen bg-gray-50">
    <!-- Header -->
    <header
      class="sticky top-0 z-50 bg-white border-b border-gray-200 shadow-sm"
    >
      <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <div class="flex justify-between items-center h-16">
          <div class="flex items-center gap-4">
            <Button variant="outline" size="sm" @click="goBack">
              <svg
                class="h-4 w-4 mr-1"
                fill="none"
                stroke="currentColor"
                viewBox="0 0 24 24"
              >
                <path
                  stroke-linecap="round"
                  stroke-linejoin="round"
                  stroke-width="2"
                  d="M15 19l-7-7 7-7"
                />
              </svg>
              Back to Organizations
            </Button>
            <h1 class="text-2xl font-bold text-gray-900">
              {{ organization?.Name || "Organization Details" }}
            </h1>
          </div>
          <div v-if="organization" class="flex items-center gap-3">
            <Button variant="outline" @click="openEditDialog">
              <svg
                class="h-4 w-4 mr-2"
                fill="none"
                stroke="currentColor"
                viewBox="0 0 24 24"
              >
                <path
                  stroke-linecap="round"
                  stroke-linejoin="round"
                  stroke-width="2"
                  d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z"
                />
              </svg>
              Edit
            </Button>
          </div>
        </div>
      </div>
    </header>

    <!-- Main Content -->
    <main class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
      <!-- Loading State -->
      <Card v-if="isLoading" class="p-6">
        <div class="text-center py-12">
          <div
            class="inline-block h-8 w-8 animate-spin rounded-full border-4 border-solid border-blue-600 border-r-transparent"
          ></div>
          <p class="mt-4 text-gray-600">Loading organization...</p>
        </div>
      </Card>

      <!-- Organization Details -->
      <div v-else-if="organization" class="space-y-6">
        <!-- Status Badge -->
        <div class="flex items-center gap-3">
          <span
            :class="[
              'px-3 py-1 inline-flex text-sm font-semibold rounded-full',
              organization.Active
                ? 'bg-green-100 text-green-800'
                : 'bg-red-100 text-red-800',
            ]"
          >
            {{ organization.Active ? "Active" : "Inactive" }}
          </span>
          <span class="text-sm text-gray-500">
            Created on
            {{ new Date(organization.CreatedAt!).toLocaleDateString() }}
          </span>
        </div>

        <!-- Organization Information -->
        <Card class="p-6">
          <h2 class="text-lg font-semibold text-gray-900 mb-4">
            Organization Information
          </h2>
          <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
            <div>
              <label class="text-sm font-medium text-gray-500">Name</label>
              <p class="mt-1 text-sm text-gray-900">
                {{ organization.Name || "—" }}
              </p>
            </div>
            <div>
              <label class="text-sm font-medium text-gray-500"
                >Registration Number</label
              >
              <p class="mt-1 text-sm text-gray-900">
                {{ organization.RegistrationNumber || "—" }}
              </p>
            </div>
            <div>
              <label class="text-sm font-medium text-gray-500"
                >Tax Number</label
              >
              <p class="mt-1 text-sm text-gray-900">
                {{ organization.TaxNumber || "—" }}
              </p>
            </div>
            <div>
              <label class="text-sm font-medium text-gray-500">Email</label>
              <p class="mt-1 text-sm text-gray-900">
                {{ organization.Email || "—" }}
              </p>
            </div>
            <div>
              <label class="text-sm font-medium text-gray-500">Phone</label>
              <p class="mt-1 text-sm text-gray-900">
                {{ organization.Phone || "—" }}
              </p>
            </div>
            <div>
              <label class="text-sm font-medium text-gray-500">Website</label>
              <p class="mt-1 text-sm text-gray-900">
                <a
                  v-if="organization.Website"
                  :href="organization.Website"
                  target="_blank"
                  rel="noopener noreferrer"
                  class="text-blue-600 hover:text-blue-800 hover:underline"
                >
                  {{ organization.Website }}
                </a>
                <span v-else>—</span>
              </p>
            </div>
          </div>
        </Card>

        <!-- Address Information -->
        <Card v-if="organization.Address" class="p-6">
          <h2 class="text-lg font-semibold text-gray-900 mb-4">Address</h2>
          <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
            <div>
              <label class="text-sm font-medium text-gray-500">Street</label>
              <p class="mt-1 text-sm text-gray-900">
                {{ organization.Address.Street || "—" }}
              </p>
            </div>
            <div>
              <label class="text-sm font-medium text-gray-500">City</label>
              <p class="mt-1 text-sm text-gray-900">
                {{ organization.Address.City || "—" }}
              </p>
            </div>
            <div>
              <label class="text-sm font-medium text-gray-500"
                >State/Province</label
              >
              <p class="mt-1 text-sm text-gray-900">
                {{ organization.Address.State || "—" }}
              </p>
            </div>
            <div>
              <label class="text-sm font-medium text-gray-500"
                >Postal Code</label
              >
              <p class="mt-1 text-sm text-gray-900">
                {{ organization.Address.PostalCode || "—" }}
              </p>
            </div>
            <div>
              <label class="text-sm font-medium text-gray-500">Country</label>
              <p class="mt-1 text-sm text-gray-900">
                {{ organization.Address.Country || "—" }}
              </p>
            </div>
          </div>
        </Card>

        <!-- Members Section -->
        <Card class="p-6">
          <div class="flex justify-between items-center mb-4">
            <h2 class="text-lg font-semibold text-gray-900">Members</h2>
            <Button size="sm" @click="openAddMemberDialog">
              <svg
                class="h-4 w-4 mr-2"
                fill="none"
                stroke="currentColor"
                viewBox="0 0 24 24"
              >
                <path
                  stroke-linecap="round"
                  stroke-linejoin="round"
                  stroke-width="2"
                  d="M12 6v6m0 0v6m0-6h6m-6 0H6"
                />
              </svg>
              Add Member
            </Button>
          </div>

          <!-- Loading State -->
          <div v-if="isLoadingMembers" class="text-center py-8">
            <div
              class="inline-block h-8 w-8 animate-spin rounded-full border-4 border-solid border-blue-600 border-r-transparent"
            ></div>
            <p class="mt-4 text-gray-600">Loading members...</p>
          </div>

          <!-- Members List -->
          <div v-else-if="members.length > 0" class="overflow-x-auto">
            <table class="min-w-full divide-y divide-gray-200">
              <thead class="bg-gray-50">
                <tr>
                  <th
                    scope="col"
                    class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider"
                  >
                    Member
                  </th>
                  <th
                    scope="col"
                    class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider"
                  >
                    Email
                  </th>
                  <th
                    scope="col"
                    class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider"
                  >
                    Role
                  </th>
                  <th
                    scope="col"
                    class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider"
                  >
                    Status
                  </th>
                  <th
                    scope="col"
                    class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider"
                  >
                    Joined
                  </th>
                  <th scope="col" class="relative px-6 py-3">
                    <span class="sr-only">Actions</span>
                  </th>
                </tr>
              </thead>
              <tbody class="bg-white divide-y divide-gray-200">
                <tr v-for="member in members" :key="member.UserId">
                  <td class="px-6 py-4 whitespace-nowrap">
                    <div class="text-sm font-medium text-gray-900">
                      {{
                        member.FirstName || member.LastName
                          ? `${member.FirstName || ""} ${member.LastName || ""}`.trim()
                          : "—"
                      }}
                    </div>
                  </td>
                  <td class="px-6 py-4 whitespace-nowrap">
                    <div class="text-sm text-gray-900">{{ member.Email }}</div>
                  </td>
                  <td class="px-6 py-4 whitespace-nowrap">
                    <span
                      :class="[
                        'px-2 py-1 inline-flex text-xs leading-5 font-semibold rounded-full',
                        member.Role?.toLowerCase() === 'orgadmin'
                          ? 'bg-blue-100 text-blue-800'
                          : member.Role?.toLowerCase() === 'systemadmin'
                          ? 'bg-purple-100 text-purple-800'
                          : 'bg-gray-100 text-gray-800',
                      ]"
                    >
                      {{
                        member.Role?.toLowerCase() === "orgadmin"
                          ? "Org Admin"
                          : member.Role?.toLowerCase() === "systemadmin"
                          ? "System Admin"
                          : "Org User"
                      }}
                    </span>
                  </td>
                  <td class="px-6 py-4 whitespace-nowrap">
                    <span
                      :class="[
                        'px-2 py-1 inline-flex text-xs leading-5 font-semibold rounded-full',
                        member.Active
                          ? 'bg-green-100 text-green-800'
                          : 'bg-red-100 text-red-800',
                      ]"
                    >
                      {{ member.Active ? "Active" : "Inactive" }}
                    </span>
                  </td>
                  <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                    {{ new Date(member.JoinedAt!).toLocaleDateString() }}
                  </td>
                  <td
                    class="px-6 py-4 whitespace-nowrap text-right text-sm font-medium"
                  >
                    <div class="flex justify-end gap-2">
                      <Button
                        variant="outline"
                        size="sm"
                        @click="openEditMemberDialog(member)"
                      >
                        Edit
                      </Button>
                      <Button
                        variant="outline"
                        size="sm"
                        @click="openRemoveMemberDialog(member)"
                      >
                        Remove
                      </Button>
                    </div>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>

          <!-- Empty State -->
          <div v-else class="text-center py-8">
            <svg
              class="h-12 w-12 text-gray-400 mx-auto mb-4"
              fill="none"
              stroke="currentColor"
              viewBox="0 0 24 24"
            >
              <path
                stroke-linecap="round"
                stroke-linejoin="round"
                stroke-width="2"
                d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0zm6 3a2 2 0 11-4 0 2 2 0 014 0zM7 10a2 2 0 11-4 0 2 2 0 014 0z"
              />
            </svg>
            <p class="text-gray-600 mb-4">No members yet</p>
            <Button size="sm" @click="openAddMemberDialog"
              >Add your first member</Button
            >
          </div>
        </Card>
      </div>
    </main>

    <EditOrganizationDialog
      v-model:open="isEditDialogOpen"
      :organization="organization"
      @success="handleOrganizationUpdated"
    />

    <AddMemberDialog
      v-if="organizationId"
      v-model:open="isAddMemberDialogOpen"
      :organization-id="organizationId"
      @success="handleMemberAdded"
    />

    <EditMemberDialog
      v-model:open="isEditMemberDialogOpen"
      :member="selectedMember"
      @success="handleMemberUpdated"
    />

    <RemoveMemberDialog
      v-model:open="isRemoveMemberDialogOpen"
      :member="memberToRemove"
      @confirm="handleRemoveMember"
    />
  </div>
</template>
