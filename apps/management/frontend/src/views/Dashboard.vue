<script setup lang="ts">
import { ref, onMounted } from "vue";
import { useRouter } from "vue-router";
import { useAuthStore } from "../stores/auth";
import { toast } from "vue-sonner";
import Button from "../components/ui/Button.vue";
import Card from "../components/ui/Card.vue";
import CreateOrganizationDialog from "../components/CreateOrganizationDialog.vue";
import EditOrganizationDialog from "../components/EditOrganizationDialog.vue";
import { organizationService } from "../services/organizations";
import type { OrganizationResponse } from "../api/generated/api-client";
import { getErrorMessage } from "../lib/error-utils";

const router = useRouter();
const authStore = useAuthStore();
const isCreateDialogOpen = ref(false);
const isEditDialogOpen = ref(false);
const organizations = ref<OrganizationResponse[]>([]);
const selectedOrganization = ref<OrganizationResponse | null>(null);
const isLoading = ref(false);

const handleLogout = async () => {
  await authStore.logout();
  toast.success("Logged out successfully");
  router.push("/login");
};

const openCreateDialog = () => {
  isCreateDialogOpen.value = true;
};

const navigateToOrganizations = () => {
  router.push("/organizations");
};

const fetchOrganizations = async () => {
  isLoading.value = true;
  try {
    organizations.value = await organizationService.getAll();
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
          <div class="flex items-center">
            <h1 class="text-2xl font-bold text-gray-900">Management Portal</h1>
          </div>
          <div class="flex items-center gap-4">
            <span class="text-sm text-gray-600">
              {{ authStore.user?.username }}
            </span>
            <Button variant="outline" size="sm" @click="handleLogout">
              Logout
            </Button>
          </div>
        </div>
      </div>
    </header>

    <!-- Main Content -->
    <main class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
      <!-- Welcome Section -->
      <div class="mb-8">
        <h2 class="text-3xl font-bold text-gray-900 mb-2">
          Welcome, {{ authStore.user?.username }}
        </h2>
        <p class="text-gray-600">
          Manage organizations and system settings from this portal.
        </p>
      </div>

      <!-- Stats Grid -->
      <div class="grid grid-cols-1 md:grid-cols-3 gap-6 mb-8">
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
              <p class="text-sm font-medium text-gray-600">Active Users</p>
              <p class="text-3xl font-bold text-gray-900 mt-2">0</p>
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
                  d="M12 4.354a4 4 0 110 5.292M15 21H3v-1a6 6 0 0112 0v1zm0 0h6v-1a6 6 0 00-9-5.197M13 7a4 4 0 11-8 0 4 4 0 018 0z"
                />
              </svg>
            </div>
          </div>
        </Card>

        <Card class="p-6">
          <div class="flex items-center justify-between">
            <div>
              <p class="text-sm font-medium text-gray-600">System Status</p>
              <p class="text-3xl font-bold text-green-600 mt-2">Healthy</p>
            </div>
            <div
              class="h-12 w-12 bg-purple-100 rounded-lg flex items-center justify-center"
            >
              <svg
                class="h-6 w-6 text-purple-600"
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
      </div>

      <!-- Quick Actions -->
      <div class="mb-8">
        <h3 class="text-xl font-semibold text-gray-900 mb-4">Quick Actions</h3>
        <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
          <Card
            class="p-6 hover:shadow-lg transition-shadow cursor-pointer"
            @click="navigateToOrganizations"
          >
            <div class="text-center">
              <div
                class="h-12 w-12 bg-blue-100 rounded-lg flex items-center justify-center mx-auto mb-3"
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
              <h4 class="font-semibold text-gray-900 mb-1">
                View Organizations
              </h4>
              <p class="text-sm text-gray-600">Manage all organizations</p>
            </div>
          </Card>

          <Card
            class="p-6 hover:shadow-lg transition-shadow cursor-pointer"
            @click="openCreateDialog"
          >
            <div class="text-center">
              <div
                class="h-12 w-12 bg-green-100 rounded-lg flex items-center justify-center mx-auto mb-3"
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
                    d="M12 6v6m0 0v6m0-6h6m-6 0H6"
                  />
                </svg>
              </div>
              <h4 class="font-semibold text-gray-900 mb-1">New Organization</h4>
              <p class="text-sm text-gray-600">Create a new organization</p>
            </div>
          </Card>

          <Card class="p-6 hover:shadow-lg transition-shadow cursor-pointer">
            <div class="text-center">
              <div
                class="h-12 w-12 bg-green-100 rounded-lg flex items-center justify-center mx-auto mb-3"
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
                    d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0zm6 3a2 2 0 11-4 0 2 2 0 014 0zM7 10a2 2 0 11-4 0 2 2 0 014 0z"
                  />
                </svg>
              </div>
              <h4 class="font-semibold text-gray-900 mb-1">Manage Users</h4>
              <p class="text-sm text-gray-600">View and manage users</p>
            </div>
          </Card>

          <Card class="p-6 hover:shadow-lg transition-shadow cursor-pointer">
            <div class="text-center">
              <div
                class="h-12 w-12 bg-purple-100 rounded-lg flex items-center justify-center mx-auto mb-3"
              >
                <svg
                  class="h-6 w-6 text-purple-600"
                  fill="none"
                  stroke="currentColor"
                  viewBox="0 0 24 24"
                >
                  <path
                    stroke-linecap="round"
                    stroke-linejoin="round"
                    stroke-width="2"
                    d="M10.325 4.317c.426-1.756 2.924-1.756 3.35 0a1.724 1.724 0 002.573 1.066c1.543-.94 3.31.826 2.37 2.37a1.724 1.724 0 001.065 2.572c1.756.426 1.756 2.924 0 3.35a1.724 1.724 0 00-1.066 2.573c.94 1.543-.826 3.31-2.37 2.37a1.724 1.724 0 00-2.572 1.065c-.426 1.756-2.924 1.756-3.35 0a1.724 1.724 0 00-2.573-1.066c-1.543.94-3.31-.826-2.37-2.37a1.724 1.724 0 00-1.065-2.572c-1.756-.426-1.756-2.924 0-3.35a1.724 1.724 0 001.066-2.573c-.94-1.543.826-3.31 2.37-2.37.996.608 2.296.07 2.572-1.065z"
                  />
                  <path
                    stroke-linecap="round"
                    stroke-linejoin="round"
                    stroke-width="2"
                    d="M15 12a3 3 0 11-6 0 3 3 0 016 0z"
                  />
                </svg>
              </div>
              <h4 class="font-semibold text-gray-900 mb-1">System Settings</h4>
              <p class="text-sm text-gray-600">Configure system settings</p>
            </div>
          </Card>

          <Card class="p-6 hover:shadow-lg transition-shadow cursor-pointer">
            <div class="text-center">
              <div
                class="h-12 w-12 bg-orange-100 rounded-lg flex items-center justify-center mx-auto mb-3"
              >
                <svg
                  class="h-6 w-6 text-orange-600"
                  fill="none"
                  stroke="currentColor"
                  viewBox="0 0 24 24"
                >
                  <path
                    stroke-linecap="round"
                    stroke-linejoin="round"
                    stroke-width="2"
                    d="M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z"
                  />
                </svg>
              </div>
              <h4 class="font-semibold text-gray-900 mb-1">View Reports</h4>
              <p class="text-sm text-gray-600">Access system reports</p>
            </div>
          </Card>
        </div>
      </div>
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
