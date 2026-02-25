<script setup lang="ts">
import { ref, onMounted, computed } from "vue";
import { useRouter, useRoute } from "vue-router";
import { toast } from "vue-sonner";
import { ChevronRight, Pencil, Plus } from "lucide-vue-next";
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
      organizationId.value
    );
  } catch (error: any) {
    toast.error(getErrorMessage(error, "Failed to fetch members"));
  } finally {
    isLoadingMembers.value = false;
  }
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
      memberToRemove.value.UserId
    );
    toast.success("Member removed successfully");
    isRemoveMemberDialogOpen.value = false;
    memberToRemove.value = null;
    fetchMembers();
  } catch (error: any) {
    toast.error(getErrorMessage(error, "Failed to remove member"));
    isRemoveMemberDialogOpen.value = false;
    memberToRemove.value = null;
  }
};

const planBadgeClass = (name?: string) => {
  switch (name?.toLowerCase()) {
    case "basic":
      return "bg-gray-100 text-gray-700 border border-gray-200";
    case "advanced":
      return "bg-blue-100 text-blue-700 border border-blue-200";
    case "ultimate":
      return "bg-amber-100 text-amber-700 border border-amber-200";
    default:
      return "bg-gray-100 text-gray-600 border border-gray-200";
  }
};

onMounted(() => {
  fetchOrganization();
  fetchMembers();
});
</script>

<template>
  <div class="min-h-full bg-gray-50">
    <!-- Page header -->
    <div class="bg-white border-b border-gray-200 px-8 py-5">
      <!-- Breadcrumb -->
      <nav class="flex items-center gap-1.5 text-sm text-gray-500 mb-3">
        <button
          @click="router.push('/organizations')"
          class="hover:text-gray-700 transition-colors"
        >
          Organizations
        </button>
        <ChevronRight class="h-3.5 w-3.5 text-gray-400" />
        <span class="text-gray-900 font-medium truncate">
          {{ organization?.Name ?? "Loading..." }}
        </span>
      </nav>

      <div class="flex items-center justify-between">
        <div class="flex items-center gap-3">
          <h1 class="text-2xl font-bold text-gray-900">
            {{ organization?.Name ?? "Organization Details" }}
          </h1>
          <span
            v-if="organization"
            :class="[
              'px-2.5 py-0.5 text-xs font-semibold rounded-full',
              organization.Active
                ? 'bg-green-100 text-green-700'
                : 'bg-red-100 text-red-600',
            ]"
          >
            {{ organization.Active ? "Active" : "Inactive" }}
          </span>
          <span
            v-if="organization?.PaymentPlan"
            :class="[
              'px-2.5 py-0.5 text-xs font-semibold rounded-full',
              planBadgeClass(organization.PaymentPlan.Name),
            ]"
          >
            {{ organization.PaymentPlan.Name }}
          </span>
        </div>
        <Button
          v-if="organization"
          variant="outline"
          @click="isEditDialogOpen = true"
        >
          <Pencil class="h-4 w-4 mr-2" />
          Edit
        </Button>
      </div>
    </div>

    <!-- Loading -->
    <div v-if="isLoading" class="flex items-center justify-center py-24">
      <div
        class="h-8 w-8 animate-spin rounded-full border-4 border-blue-600 border-r-transparent"
      />
    </div>

    <div v-else-if="organization" class="px-8 py-6 space-y-6">
      <!-- Info grid -->
      <div class="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <!-- Organization Information -->
        <Card class="p-5">
          <h2 class="text-sm font-semibold text-gray-700 mb-4">
            Organization Information
          </h2>
          <dl class="space-y-3">
            <div class="grid grid-cols-2 gap-2">
              <div>
                <dt class="text-xs font-medium text-gray-400 uppercase tracking-wide">
                  Name
                </dt>
                <dd class="mt-0.5 text-sm text-gray-900">
                  {{ organization.Name || "—" }}
                </dd>
              </div>
              <div>
                <dt class="text-xs font-medium text-gray-400 uppercase tracking-wide">
                  Registration No.
                </dt>
                <dd class="mt-0.5 text-sm text-gray-900">
                  {{ organization.RegistrationNumber || "—" }}
                </dd>
              </div>
              <div>
                <dt class="text-xs font-medium text-gray-400 uppercase tracking-wide">
                  Tax Number
                </dt>
                <dd class="mt-0.5 text-sm text-gray-900">
                  {{ organization.TaxNumber || "—" }}
                </dd>
              </div>
              <div>
                <dt class="text-xs font-medium text-gray-400 uppercase tracking-wide">
                  Email
                </dt>
                <dd class="mt-0.5 text-sm text-gray-900">
                  {{ organization.Email || "—" }}
                </dd>
              </div>
              <div>
                <dt class="text-xs font-medium text-gray-400 uppercase tracking-wide">
                  Phone
                </dt>
                <dd class="mt-0.5 text-sm text-gray-900">
                  {{ organization.Phone || "—" }}
                </dd>
              </div>
              <div>
                <dt class="text-xs font-medium text-gray-400 uppercase tracking-wide">
                  Website
                </dt>
                <dd class="mt-0.5 text-sm text-gray-900">
                  <a
                    v-if="organization.Website"
                    :href="organization.Website"
                    target="_blank"
                    rel="noopener noreferrer"
                    class="text-blue-600 hover:underline"
                  >
                    {{ organization.Website }}
                  </a>
                  <span v-else>—</span>
                </dd>
              </div>
              <div>
                <dt class="text-xs font-medium text-gray-400 uppercase tracking-wide">
                  Created
                </dt>
                <dd class="mt-0.5 text-sm text-gray-900">
                  {{
                    new Date(organization.CreatedAt!).toLocaleDateString(
                      "en-ZA"
                    )
                  }}
                </dd>
              </div>
            </div>
          </dl>
        </Card>

        <!-- Address + Plan info -->
        <div class="space-y-4">
          <!-- Address -->
          <Card v-if="organization.Address" class="p-5">
            <h2 class="text-sm font-semibold text-gray-700 mb-4">Address</h2>
            <dl class="grid grid-cols-2 gap-3">
              <div>
                <dt class="text-xs font-medium text-gray-400 uppercase tracking-wide">
                  Street
                </dt>
                <dd class="mt-0.5 text-sm text-gray-900">
                  {{ organization.Address.Street || "—" }}
                </dd>
              </div>
              <div>
                <dt class="text-xs font-medium text-gray-400 uppercase tracking-wide">
                  City
                </dt>
                <dd class="mt-0.5 text-sm text-gray-900">
                  {{ organization.Address.City || "—" }}
                </dd>
              </div>
              <div>
                <dt class="text-xs font-medium text-gray-400 uppercase tracking-wide">
                  State/Province
                </dt>
                <dd class="mt-0.5 text-sm text-gray-900">
                  {{ organization.Address.State || "—" }}
                </dd>
              </div>
              <div>
                <dt class="text-xs font-medium text-gray-400 uppercase tracking-wide">
                  Postal Code
                </dt>
                <dd class="mt-0.5 text-sm text-gray-900">
                  {{ organization.Address.PostalCode || "—" }}
                </dd>
              </div>
              <div>
                <dt class="text-xs font-medium text-gray-400 uppercase tracking-wide">
                  Country
                </dt>
                <dd class="mt-0.5 text-sm text-gray-900">
                  {{ organization.Address.Country || "—" }}
                </dd>
              </div>
            </dl>
          </Card>

          <!-- Payment plan summary -->
          <Card v-if="organization.PaymentPlan" class="p-5">
            <h2 class="text-sm font-semibold text-gray-700 mb-4">
              Subscription
            </h2>
            <div class="flex items-center justify-between">
              <div>
                <span
                  :class="[
                    'px-2.5 py-1 text-xs font-semibold rounded-full',
                    planBadgeClass(organization.PaymentPlan.Name),
                  ]"
                >
                  {{ organization.PaymentPlan.Name }}
                </span>
                <p class="text-xs text-gray-400 mt-2">
                  {{
                    organization.PaymentPlan.MaxUsers === -1
                      ? "Unlimited users"
                      : `Up to ${organization.PaymentPlan.MaxUsers} users`
                  }}
                </p>
              </div>
              <div class="text-right">
                <p class="text-lg font-bold text-gray-900">
                  R
                  {{
                    organization.PaymentPlan.MonthlyCostRand?.toLocaleString(
                      "en-ZA"
                    )
                  }}
                </p>
                <p class="text-xs text-gray-400">per month</p>
              </div>
            </div>
          </Card>
        </div>
      </div>

      <!-- Members Section -->
      <Card class="p-5">
        <div class="flex items-center justify-between mb-4">
          <div>
            <h2 class="text-sm font-semibold text-gray-700">Members</h2>
            <p class="text-xs text-gray-400 mt-0.5">
              {{ members.length }} member{{ members.length !== 1 ? "s" : "" }}
            </p>
          </div>
          <Button size="sm" @click="isAddMemberDialogOpen = true">
            <Plus class="h-4 w-4 mr-2" />
            Add Member
          </Button>
        </div>

        <!-- Loading -->
        <div v-if="isLoadingMembers" class="text-center py-8">
          <div
            class="inline-block h-6 w-6 animate-spin rounded-full border-4 border-blue-600 border-r-transparent"
          />
        </div>

        <!-- Members table -->
        <div v-else-if="members.length > 0" class="overflow-x-auto">
          <table class="w-full text-sm">
            <thead>
              <tr class="border-b border-gray-100">
                <th
                  class="text-left text-xs font-semibold text-gray-400 uppercase tracking-wide pb-2"
                >
                  Member
                </th>
                <th
                  class="text-left text-xs font-semibold text-gray-400 uppercase tracking-wide pb-2"
                >
                  Email
                </th>
                <th
                  class="text-left text-xs font-semibold text-gray-400 uppercase tracking-wide pb-2"
                >
                  Role
                </th>
                <th
                  class="text-left text-xs font-semibold text-gray-400 uppercase tracking-wide pb-2"
                >
                  Status
                </th>
                <th
                  class="text-left text-xs font-semibold text-gray-400 uppercase tracking-wide pb-2"
                >
                  Joined
                </th>
                <th class="pb-2" />
              </tr>
            </thead>
            <tbody>
              <tr
                v-for="member in members"
                :key="member.UserId"
                class="border-b border-gray-50 last:border-0"
              >
                <td class="py-3 font-medium text-gray-800">
                  {{
                    member.FirstName || member.LastName
                      ? `${member.FirstName ?? ""} ${member.LastName ?? ""}`.trim()
                      : "—"
                  }}
                </td>
                <td class="py-3 text-gray-600">{{ member.Email }}</td>
                <td class="py-3">
                  <span
                    :class="[
                      'px-2 py-0.5 text-xs font-semibold rounded-full',
                      member.Role?.toLowerCase() === 'orgadmin'
                        ? 'bg-blue-100 text-blue-700'
                        : member.Role?.toLowerCase() === 'systemadmin'
                          ? 'bg-purple-100 text-purple-700'
                          : 'bg-gray-100 text-gray-600',
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
                <td class="py-3">
                  <span
                    :class="[
                      'px-2 py-0.5 text-xs font-semibold rounded-full',
                      member.Active
                        ? 'bg-green-100 text-green-700'
                        : 'bg-red-100 text-red-600',
                    ]"
                  >
                    {{ member.Active ? "Active" : "Inactive" }}
                  </span>
                </td>
                <td class="py-3 text-gray-500 text-xs">
                  {{ new Date(member.JoinedAt!).toLocaleDateString("en-ZA") }}
                </td>
                <td class="py-3 text-right">
                  <div class="flex justify-end gap-2">
                    <Button
                      variant="outline"
                      size="sm"
                      @click="
                        selectedMember = member;
                        isEditMemberDialogOpen = true;
                      "
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

        <!-- Empty state -->
        <div v-else class="text-center py-8">
          <svg
            class="h-10 w-10 text-gray-300 mx-auto mb-3"
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
          <p class="text-sm text-gray-500 mb-3">No members yet</p>
          <Button size="sm" @click="isAddMemberDialogOpen = true">
            Add first member
          </Button>
        </div>
      </Card>
    </div>

    <EditOrganizationDialog
      v-model:open="isEditDialogOpen"
      :organization="organization"
      @success="fetchOrganization"
    />
    <AddMemberDialog
      v-if="organizationId"
      v-model:open="isAddMemberDialogOpen"
      :organization-id="organizationId"
      @success="fetchMembers"
    />
    <EditMemberDialog
      v-model:open="isEditMemberDialogOpen"
      :member="selectedMember"
      @success="fetchMembers"
    />
    <RemoveMemberDialog
      v-model:open="isRemoveMemberDialogOpen"
      :member="memberToRemove"
      @confirm="handleRemoveMember"
    />
  </div>
</template>
