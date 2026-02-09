<script setup lang="ts">
import Button from "./ui/Button.vue";
import Card from "./ui/Card.vue";
import type { OrganizationResponse } from "../api/generated/api-client";

type SortColumn = "name" | "email" | "city" | "status" | "created";

interface Props {
  organizations: OrganizationResponse[];
  isLoading?: boolean;
  sortColumn?: SortColumn;
  sortDirection?: "asc" | "desc";
}

interface Emits {
  (e: "sort", column: SortColumn): void;
  (e: "edit", organization: OrganizationResponse, event: Event): void;
  (e: "row-click", organizationId: number): void;
}

const props = withDefaults(defineProps<Props>(), {
  isLoading: false,
  sortColumn: "name",
  sortDirection: "asc",
});

const emit = defineEmits<Emits>();

const handleSort = (column: SortColumn) => {
  emit("sort", column);
};

const handleEdit = (org: OrganizationResponse, event: Event) => {
  emit("edit", org, event);
};

const handleRowClick = (orgId: number) => {
  emit("row-click", orgId);
};

const getSortIcon = (column: SortColumn) => {
  if (props.sortColumn !== column) {
    return "M7 10l5 5 5-5H7z"; // Default sort icon
  }
  return props.sortDirection === "asc"
    ? "M7 14l5-5 5 5H7z" // Up arrow
    : "M7 10l5 5 5-5H7z"; // Down arrow
};
</script>

<template>
  <!-- Loading State -->
  <Card v-if="isLoading" class="p-6">
    <div class="text-center py-12">
      <div
        class="inline-block h-8 w-8 animate-spin rounded-full border-4 border-solid border-blue-600 border-r-transparent"
      ></div>
      <p class="mt-4 text-gray-600">Loading organizations...</p>
    </div>
  </Card>

  <!-- Organizations Table -->
  <Card v-else-if="organizations.length > 0" class="overflow-hidden">
    <div class="overflow-x-auto">
      <table class="min-w-full divide-y divide-gray-200">
        <thead class="bg-gray-50">
          <tr>
            <th
              class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider cursor-pointer hover:bg-gray-100 transition-colors"
              @click="handleSort('name')"
            >
              <div class="flex items-center gap-2">
                <span>Organization</span>
                <svg
                  class="h-4 w-4"
                  :class="
                    sortColumn === 'name' ? 'text-blue-600' : 'text-gray-400'
                  "
                  fill="currentColor"
                  viewBox="0 0 24 24"
                >
                  <path :d="getSortIcon('name')" />
                </svg>
              </div>
            </th>
            <th
              class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider cursor-pointer hover:bg-gray-100 transition-colors"
              @click="handleSort('email')"
            >
              <div class="flex items-center gap-2">
                <span>Contact</span>
                <svg
                  class="h-4 w-4"
                  :class="
                    sortColumn === 'email' ? 'text-blue-600' : 'text-gray-400'
                  "
                  fill="currentColor"
                  viewBox="0 0 24 24"
                >
                  <path :d="getSortIcon('email')" />
                </svg>
              </div>
            </th>
            <th
              class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider cursor-pointer hover:bg-gray-100 transition-colors"
              @click="handleSort('city')"
            >
              <div class="flex items-center gap-2">
                <span>Location</span>
                <svg
                  class="h-4 w-4"
                  :class="
                    sortColumn === 'city' ? 'text-blue-600' : 'text-gray-400'
                  "
                  fill="currentColor"
                  viewBox="0 0 24 24"
                >
                  <path :d="getSortIcon('city')" />
                </svg>
              </div>
            </th>
            <th
              class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider cursor-pointer hover:bg-gray-100 transition-colors"
              @click="handleSort('status')"
            >
              <div class="flex items-center gap-2">
                <span>Status</span>
                <svg
                  class="h-4 w-4"
                  :class="
                    sortColumn === 'status' ? 'text-blue-600' : 'text-gray-400'
                  "
                  fill="currentColor"
                  viewBox="0 0 24 24"
                >
                  <path :d="getSortIcon('status')" />
                </svg>
              </div>
            </th>
            <th
              class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider cursor-pointer hover:bg-gray-100 transition-colors"
              @click="handleSort('created')"
            >
              <div class="flex items-center gap-2">
                <span>Created</span>
                <svg
                  class="h-4 w-4"
                  :class="
                    sortColumn === 'created' ? 'text-blue-600' : 'text-gray-400'
                  "
                  fill="currentColor"
                  viewBox="0 0 24 24"
                >
                  <path :d="getSortIcon('created')" />
                </svg>
              </div>
            </th>
            <th
              class="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider"
            >
              Actions
            </th>
          </tr>
        </thead>
        <tbody class="bg-white divide-y divide-gray-200">
          <tr
            v-for="org in organizations"
            :key="org.Id"
            class="hover:bg-gray-50 transition-colors cursor-pointer"
            @click="handleRowClick(org.Id!)"
          >
            <td class="px-6 py-4 whitespace-nowrap">
              <div class="flex items-center">
                <div
                  class="flex-shrink-0 h-10 w-10 bg-blue-100 rounded-lg flex items-center justify-center"
                >
                  <svg
                    class="h-5 w-5 text-blue-600"
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
                <div class="ml-4">
                  <div class="text-sm font-medium text-gray-900">
                    {{ org.Name }}
                  </div>
                  <div
                    v-if="org.RegistrationNumber"
                    class="text-sm text-gray-500"
                  >
                    Reg: {{ org.RegistrationNumber }}
                  </div>
                </div>
              </div>
            </td>
            <td class="px-6 py-4 whitespace-nowrap">
              <div class="text-sm text-gray-900">
                {{ org.Email || "—" }}
              </div>
              <div class="text-sm text-gray-500">
                {{ org.Phone || "—" }}
              </div>
            </td>
            <td class="px-6 py-4 whitespace-nowrap">
              <div class="text-sm text-gray-900">
                {{ org.Address?.City || "—" }}
              </div>
              <div class="text-sm text-gray-500">
                {{ org.Address?.Country || "—" }}
              </div>
            </td>
            <td class="px-6 py-4 whitespace-nowrap">
              <span
                :class="[
                  'px-2 inline-flex text-xs leading-5 font-semibold rounded-full',
                  org.Active
                    ? 'bg-green-100 text-green-800'
                    : 'bg-red-100 text-red-800',
                ]"
              >
                {{ org.Active ? "Active" : "Inactive" }}
              </span>
            </td>
            <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
              {{ new Date(org.CreatedAt!).toLocaleDateString() }}
            </td>
            <td
              class="px-6 py-4 whitespace-nowrap text-right text-sm font-medium"
            >
              <div class="flex justify-end gap-2">
                <Button
                  variant="outline"
                  size="sm"
                  @click="handleEdit(org, $event)"
                >
                  <svg
                    class="h-4 w-4"
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
                  <span class="ml-1">Edit</span>
                </Button>
              </div>
            </td>
          </tr>
        </tbody>
      </table>
    </div>
  </Card>
</template>
