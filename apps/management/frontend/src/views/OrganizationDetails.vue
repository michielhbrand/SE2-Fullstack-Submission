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
import OrganizationMembersTable from "../components/OrganizationMembersTable.vue";
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

        <OrganizationMembersTable
          :members="members"
          :is-loading="isLoadingMembers"
          @edit="selectedMember = $event; isEditMemberDialogOpen = true"
          @remove="openRemoveMemberDialog($event)"
        >
          <template #empty-action>
            <Button size="sm" @click="isAddMemberDialogOpen = true">
              Add first member
            </Button>
          </template>
        </OrganizationMembersTable>
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
