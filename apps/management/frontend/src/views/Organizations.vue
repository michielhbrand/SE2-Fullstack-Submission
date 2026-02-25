<script setup lang="ts">
import { ref, onMounted, computed, watch } from "vue";
import { useRouter } from "vue-router";
import { toast } from "vue-sonner";
import { Plus } from "lucide-vue-next";
import Button from "../components/ui/Button.vue";
import Card from "../components/ui/Card.vue";
import Input from "../components/ui/Input.vue";
import CreateOrganizationDialog from "../components/CreateOrganizationDialog.vue";
import EditOrganizationDialog from "../components/EditOrganizationDialog.vue";
import OrganizationsTable from "../components/OrganizationsTable.vue";
import { organizationService } from "../services/organizations";
import type { OrganizationResponse } from "../api/generated/api-client";
import { getErrorMessage } from "../lib/error-utils";

const router = useRouter();
const isCreateDialogOpen = ref(false);
const isEditDialogOpen = ref(false);
const organizations = ref<OrganizationResponse[]>([]);
const selectedOrganization = ref<OrganizationResponse | null>(null);
const isLoading = ref(false);

const searchQuery = ref("");
const statusFilter = ref<"all" | "active" | "inactive">("all");
const sortColumn = ref<"name" | "email" | "city" | "status" | "created">(
  "name"
);
const sortDirection = ref<"asc" | "desc">("asc");

let searchDebounceTimer: ReturnType<typeof setTimeout> | null = null;

const openEditDialog = (org: OrganizationResponse, event: Event) => {
  event.stopPropagation();
  selectedOrganization.value = org;
  isEditDialogOpen.value = true;
};

const navigateToOrganization = (orgId: number) => {
  router.push(`/organizations/${orgId}`);
};

const fetchOrganizations = async () => {
  isLoading.value = true;
  try {
    organizations.value = await organizationService.getAll({
      search: searchQuery.value || undefined,
      status: statusFilter.value,
      sortBy: sortColumn.value,
      sortDirection: sortDirection.value,
    });
  } catch (error: any) {
    toast.error(getErrorMessage(error, "Failed to fetch organizations"));
  } finally {
    isLoading.value = false;
  }
};

const handleSort = (column: typeof sortColumn.value) => {
  if (sortColumn.value === column) {
    sortDirection.value = sortDirection.value === "asc" ? "desc" : "asc";
  } else {
    sortColumn.value = column;
    sortDirection.value = "asc";
  }
  fetchOrganizations();
};

const clearFilters = () => {
  searchQuery.value = "";
  statusFilter.value = "all";
};

const activeOrganizationsCount = computed(
  () => organizations.value.filter((org) => org.Active).length
);

watch(searchQuery, () => {
  if (searchDebounceTimer) clearTimeout(searchDebounceTimer);
  searchDebounceTimer = setTimeout(() => fetchOrganizations(), 300);
});

watch(statusFilter, () => fetchOrganizations());

onMounted(fetchOrganizations);
</script>

<template>
  <div class="min-h-full bg-gray-50">
    <!-- Page header -->
    <div class="bg-white border-b border-gray-200 px-8 py-6">
      <div class="flex items-center justify-between">
        <div>
          <h1 class="text-2xl font-bold text-gray-900">Organizations</h1>
          <p class="text-sm text-gray-500 mt-0.5">
            {{ organizations.length }} organization{{
              organizations.length !== 1 ? "s" : ""
            }}
            &middot; {{ activeOrganizationsCount }} active
          </p>
        </div>
        <Button @click="isCreateDialogOpen = true">
          <Plus class="h-4 w-4 mr-2" />
          Add Organization
        </Button>
      </div>
    </div>

    <div class="px-8 py-6 space-y-4">
      <!-- Stats row -->
      <div class="grid grid-cols-3 gap-4">
        <Card class="p-4">
          <p class="text-xs font-semibold text-gray-500 uppercase tracking-wide">
            Total
          </p>
          <p class="text-2xl font-bold text-gray-900 mt-1">
            {{ organizations.length }}
          </p>
        </Card>
        <Card class="p-4">
          <p class="text-xs font-semibold text-gray-500 uppercase tracking-wide">
            Active
          </p>
          <p class="text-2xl font-bold text-green-600 mt-1">
            {{ activeOrganizationsCount }}
          </p>
        </Card>
        <Card class="p-4">
          <p class="text-xs font-semibold text-gray-500 uppercase tracking-wide">
            Inactive
          </p>
          <p class="text-2xl font-bold text-red-500 mt-1">
            {{ organizations.length - activeOrganizationsCount }}
          </p>
        </Card>
      </div>

      <!-- Search and filter -->
      <Card class="p-4">
        <div class="flex flex-col md:flex-row gap-3">
          <div class="flex-1 relative">
            <svg
              class="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-gray-400"
              fill="none"
              stroke="currentColor"
              viewBox="0 0 24 24"
            >
              <path
                stroke-linecap="round"
                stroke-linejoin="round"
                stroke-width="2"
                d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z"
              />
            </svg>
            <Input
              v-model="searchQuery"
              type="text"
              placeholder="Search by name, email, phone, city..."
              class="pl-9"
            />
          </div>
          <div class="flex gap-2">
            <Button
              :variant="statusFilter === 'all' ? 'default' : 'outline'"
              size="sm"
              @click="statusFilter = 'all'"
            >
              All
            </Button>
            <Button
              :variant="statusFilter === 'active' ? 'default' : 'outline'"
              size="sm"
              @click="statusFilter = 'active'"
            >
              Active
            </Button>
            <Button
              :variant="statusFilter === 'inactive' ? 'default' : 'outline'"
              size="sm"
              @click="statusFilter = 'inactive'"
            >
              Inactive
            </Button>
          </div>
        </div>
      </Card>

      <!-- Empty — no orgs at all -->
      <Card
        v-if="
          !isLoading &&
          organizations.length === 0 &&
          !searchQuery &&
          statusFilter === 'all'
        "
        class="p-6"
      >
        <div class="text-center py-10">
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
              d="M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5M9 7h1m-1 4h1m4-4h1m-1 4h1m-5 10v-5a1 1 0 011-1h2a1 1 0 011 1v5m-4 0h4"
            />
          </svg>
          <h4 class="text-base font-semibold text-gray-900 mb-1">
            No organizations yet
          </h4>
          <p class="text-sm text-gray-500 mb-4">
            Create your first organization to get started
          </p>
          <Button @click="isCreateDialogOpen = true">
            Create Organization
          </Button>
        </div>
      </Card>

      <!-- Empty — no results -->
      <Card
        v-else-if="!isLoading && organizations.length === 0"
        class="p-6"
      >
        <div class="text-center py-10">
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
              d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z"
            />
          </svg>
          <h4 class="text-base font-semibold text-gray-900 mb-1">
            No results found
          </h4>
          <p class="text-sm text-gray-500 mb-4">
            Try adjusting your search or filter criteria
          </p>
          <Button variant="outline" @click="clearFilters">Clear Filters</Button>
        </div>
      </Card>

      <OrganizationsTable
        :organizations="organizations"
        :is-loading="isLoading"
        :sort-column="sortColumn"
        :sort-direction="sortDirection"
        @sort="handleSort"
        @edit="openEditDialog"
        @row-click="navigateToOrganization"
      />
    </div>

    <CreateOrganizationDialog
      v-model:open="isCreateDialogOpen"
      @success="fetchOrganizations"
    />
    <EditOrganizationDialog
      v-model:open="isEditDialogOpen"
      :organization="selectedOrganization"
      @success="fetchOrganizations"
    />
  </div>
</template>
