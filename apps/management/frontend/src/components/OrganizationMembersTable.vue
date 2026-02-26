<script setup lang="ts">
import Button from "./ui/Button.vue";
import type { OrganizationMemberResponse } from "../api/generated/api-client";

interface Props {
  members: OrganizationMemberResponse[];
  isLoading: boolean;
}

defineProps<Props>();

const emit = defineEmits<{
  edit: [member: OrganizationMemberResponse];
  remove: [member: OrganizationMemberResponse];
}>();
</script>

<template>
  <!-- Loading -->
  <div v-if="isLoading" class="text-center py-8">
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
                @click="emit('edit', member)"
              >
                Edit
              </Button>
              <Button
                variant="outline"
                size="sm"
                @click="emit('remove', member)"
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
    <slot name="empty-action" />
  </div>
</template>
