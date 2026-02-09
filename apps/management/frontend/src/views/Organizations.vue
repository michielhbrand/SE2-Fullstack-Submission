<script setup lang="ts">
import { ref, onMounted, computed, watch } from "vue";
import { useRouter } from "vue-router";
import { toast } from "vue-sonner";
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

// Search and filter state
const searchQuery = ref("");
const statusFilter = ref<"all" | "active" | "inactive">("all");
const sortColumn = ref<"name" | "email" | "city" | "status" | "created">(
  "name",
);
const sortDirection = ref<"asc" | "desc">("asc");

let searchDebounceTimer: ReturnType<typeof setTimeout> | null = null;

const openCreateDialog = () => {
  isCreateDialogOpen.value = true;
};

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
    console.error("Failed to fetch organizations:", error);
    toast.error(getErrorMessage(error, "Failed to fetch organizations"));
  } finally {
    isLoading.value = false;
  }
};

const handleOrganizationCreated = () => {
  fetchOrganizations();
};

const handleOrganizationUpdated = () => {
  fetchOrganizations();
};

const goBack = () => {
  router.push("/dashboard");
};

const activeOrganizationsCount = computed(() => {
  return organizations.value.filter((org) => org.Active).length;
});

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

watch(searchQuery, () => {
  if (searchDebounceTimer) {
    clearTimeout(searchDebounceTimer);
  }
  searchDebounceTimer = setTimeout(() => {
    fetchOrganizations();
  }, 300);
});

watch(statusFilter, () => {
  fetchOrganizations();
});

onMounted(() => {
  fetchOrganizations();
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
              Back
            </Button>
            <h1 class="text-2xl font-bold text-gray-900">Organizations</h1>
          </div>
          <Button @click="openCreateDialog">
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
            Add Organization
          </Button>
        </div>
      </div>
    </header>

    <!-- Main Content -->
    <main class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
      <!-- Stats -->
      <div class="mb-6 grid grid-cols-1 md:grid-cols-3 gap-4">
        <Card class="p-6">
          <div class="flex items-center justify-between">
            <div>
              <p class="text-sm font-medium text-gray-600">
                Total Organizations
              </p>
              <p class="text-3xl font-bold text-gray-900 mt-2">
                {{ organizations.length }}
              </p>
            </div>
            <div
              class="h-12 w-12 bg-blue-100 rounded-lg flex items-center justify-center"
            >
              <svg
                class="h-6 w-6 text-blue-600"
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
            </div>
          </div>
        </Card>

        <Card class="p-6">
          <div class="flex items-center justify-between">
            <div>
              <p class="text-sm font-medium text-gray-600">Active</p>
              <p class="text-3xl font-bold text-green-600 mt-2">
                {{ activeOrganizationsCount }}
              </p>
            </div>
            <div
              class="h-12 w-12 bg-green-100 rounded-lg flex items-center justify-center"
            >
              <svg
                class="h-6 w-6 text-green-600"
                fill="none"
                stroke="currentColor"
                viewBox="0 0 24 24"
              >
                <path
                  stroke-linecap="round"
                  stroke-linejoin="round"
                  stroke-width="2"
                  d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z"
                />
              </svg>
            </div>
          </div>
        </Card>

        <Card class="p-6">
          <div class="flex items-center justify-between">
            <div>
              <p class="text-sm font-medium text-gray-600">Inactive</p>
              <p class="text-3xl font-bold text-red-600 mt-2">
                {{ organizations.length - activeOrganizationsCount }}
              </p>
            </div>
            <div
              class="h-12 w-12 bg-red-100 rounded-lg flex items-center justify-center"
            >
              <svg
                class="h-6 w-6 text-red-600"
                fill="none"
                stroke="currentColor"
                viewBox="0 0 24 24"
              >
                <path
                  stroke-linecap="round"
                  stroke-linejoin="round"
                  stroke-width="2"
                  d="M10 14l2-2m0 0l2-2m-2 2l-2-2m2 2l2 2m7-2a9 9 0 11-18 0 9 9 0 0118 0z"
                />
              </svg>
            </div>
          </div>
        </Card>
      </div>

      <!-- Search and Filter Bar -->
      <Card class="p-4 mb-6">
        <div class="flex flex-col md:flex-row gap-4">
          <!-- Search Input -->
          <div class="flex-1">
            <div class="relative">
              <svg
                class="absolute left-3 top-1/2 transform -translate-y-1/2 h-5 w-5 text-gray-400"
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
                placeholder="Search organizations by name, email, phone, city..."
                class="pl-10"
              />
            </div>
          </div>

          <!-- Status Filter -->
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

        <!-- Results count -->
        <div class="mt-3 text-sm text-gray-600">
          Showing {{ organizations.length }} organization{{
            organizations.length !== 1 ? "s" : ""
          }}
        </div>
      </Card>

      <!-- Empty State - No organizations at all -->
      <Card
        v-if="
          !isLoading &&
          organizations.length === 0 &&
          !searchQuery &&
          statusFilter === 'all'
        "
        class="p-6"
      >
        <div class="text-center py-12">
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
              d="M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5M9 7h1m-1 4h1m4-4h1m-1 4h1m-5 10v-5a1 1 0 011-1h2a1 1 0 011 1v5m-4 0h4"
            />
          </svg>
          <h4 class="text-lg font-semibold text-gray-900 mb-2">
            No Organizations Yet
          </h4>
          <p class="text-gray-600 mb-4">
            Get started by creating your first organization
          </p>
          <Button @click="openCreateDialog">Create Organization</Button>
        </div>
      </Card>

      <!-- Empty State - No results from filter/search -->
      <Card v-else-if="!isLoading && organizations.length === 0" class="p-6">
        <div class="text-center py-12">
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
              d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z"
            />
          </svg>
          <h4 class="text-lg font-semibold text-gray-900 mb-2">
            No Organizations Found
          </h4>
          <p class="text-gray-600 mb-4">
            Try adjusting your search or filter criteria
          </p>
          <Button variant="outline" @click="clearFilters">
            Clear Filters
          </Button>
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
    </main>

    <CreateOrganizationDialog
      v-model:open="isCreateDialogOpen"
      @success="handleOrganizationCreated"
    />

    <EditOrganizationDialog
      v-model:open="isEditDialogOpen"
      :organization="selectedOrganization"
      @success="handleOrganizationUpdated"
    />
  </div>
</template>
