<script setup lang="ts">
import { onMounted, ref } from "vue";
import { useRouter } from "vue-router";
import { useAuthStore, type UserInfo } from "../stores/auth";
import { Button, Card, Spinner } from "../components/ui/index";
import { toast } from "vue-sonner";
import NewUserModal from "../components/modals/NewUserModal.vue";

const router = useRouter();
const authStore = useAuthStore();
const users = ref<UserInfo[]>([]);
const loading = ref(true);
const updatingUserId = ref<string | null>(null);
const currentUserId = ref<string | null>(null);
const showNewUserModal = ref(false);
const creatingUser = ref(false);

onMounted(async () => {
  if (!authStore.isAdmin) {
    router.push("/login");
    return;
  }

  currentUserId.value = authStore.getCurrentUserId();

  await loadUsers();
});

const loadUsers = async () => {
  loading.value = true;
  try {
    const fetchedUsers = await authStore.getAllUsers();
    users.value = fetchedUsers;
  } catch (err) {
    toast.error("Failed to load users");
    console.error("Error loading users:", err);
  } finally {
    loading.value = false;
  }
};

const handleLogout = async () => {
  await authStore.logout();
};

const updateUserRole = async (user: UserInfo, newRole: string) => {
  // Prevent self-demotion
  if (
    user.id === currentUserId.value &&
    newRole === "orgUser" &&
    isAdmin(user)
  ) {
    toast.error(
      "You cannot demote yourself. This would lock you out of the admin portal.",
    );
    return;
  }

  updatingUserId.value = user.id;

  try {
    const success = await authStore.updateUserRole(user.id, newRole);

    if (success) {
      toast.success(
        `Successfully updated ${user.username || user.email || "user"} to ${getRoleDisplayName(newRole)}`,
      );
      // Reload users to get updated data
      await loadUsers();
    } else {
      toast.error("Failed to update user role");
    }
  } catch (err) {
    toast.error("An error occurred while updating user role");
    console.error("Error updating user role:", err);
  } finally {
    updatingUserId.value = null;
  }
};

const isAdmin = (user: UserInfo) => {
  return (
    user.roles?.includes("orgAdmin") ||
    user.roles?.includes("systemAdmin") ||
    false
  );
};

const getUserRole = (user: UserInfo): string => {
  if (user.roles?.includes("systemAdmin")) return "systemAdmin";
  if (user.roles?.includes("orgAdmin")) return "orgAdmin";
  return "orgUser";
};

const getRoleDisplayName = (role: string): string => {
  switch (role) {
    case "orgAdmin":
      return "Org Admin";
    case "orgUser":
      return "Org User";
    default:
      return role;
  }
};

const getUserRoleBadge = (user: UserInfo) => {
  return getRoleDisplayName(getUserRole(user));
};

const isCurrentUser = (user: UserInfo) => {
  return user.id === currentUserId.value;
};

const canChangeRole = (user: UserInfo) => {
  // Cannot demote yourself if you're an admin
  return !(isCurrentUser(user) && isAdmin(user));
};

const handleCreateUser = async (userData: {
  username: string;
  email: string;
  firstName: string;
  lastName: string;
  password: string;
  role: string;
}) => {
  creatingUser.value = true;
  try {
    await authStore.createUser(userData);
    toast.success(`Successfully created user ${userData.username}`);
    showNewUserModal.value = false;
    // Reload users to show the new user
    await loadUsers();
  } catch (err: any) {
    console.error("Error creating user:", err);

    // Check for specific error messages
    if (err?.response?.data?.message) {
      toast.error(err.response.data.message);
    } else if (err?.message) {
      toast.error(err.message);
    } else {
      toast.error("Failed to create user. Please try again.");
    }
  } finally {
    creatingUser.value = false;
  }
};
</script>

<template>
  <div class="min-h-screen bg-gray-50">
    <!-- Header -->
    <header class="bg-gray-900 text-white shadow-lg">
      <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-6">
        <div class="flex items-center justify-between">
          <div>
            <h1 class="text-3xl font-bold">Admin Portal</h1>
            <p class="mt-1 text-gray-300">User Management Dashboard</p>
          </div>
          <Button
            @click="handleLogout"
            class="bg-gray-800 hover:bg-gray-700 text-white"
          >
            Logout
          </Button>
        </div>
      </div>
    </header>

    <!-- Main Content -->
    <main class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
      <!-- Users Management Card -->
      <Card class="p-6">
        <div class="flex items-center justify-between mb-6">
          <h2 class="text-2xl font-semibold text-gray-900">User Management</h2>
          <div class="flex items-center gap-3">
            <Button
              @click="showNewUserModal = true"
              class="bg-gray-900 hover:bg-gray-800 text-white"
            >
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
                  d="M12 4v16m8-8H4"
                />
              </svg>
              Create User
            </Button>
            <button
              @click="loadUsers"
              :disabled="loading"
              title="Refresh users"
              class="text-gray-600 hover:text-gray-900 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
            >
              <svg
                class="h-5 w-5"
                :class="{ 'animate-spin': loading }"
                fill="none"
                stroke="currentColor"
                viewBox="0 0 24 24"
              >
                <path
                  stroke-linecap="round"
                  stroke-linejoin="round"
                  stroke-width="2"
                  d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15"
                />
              </svg>
            </button>
          </div>
        </div>

        <!-- Loading State -->
        <div v-if="loading" class="flex justify-center items-center py-12">
          <Spinner class="h-8 w-8 text-gray-900" />
        </div>

        <!-- Users Table -->
        <div v-else-if="users.length > 0" class="overflow-x-auto">
          <table class="min-w-full divide-y divide-gray-200">
            <thead class="bg-gray-50">
              <tr>
                <th
                  class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider"
                >
                  User
                </th>
                <th
                  class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider"
                >
                  Email
                </th>
                <th
                  class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider"
                >
                  Status
                </th>
                <th
                  class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider"
                >
                  Role
                </th>
                <th
                  class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider"
                >
                  Actions
                </th>
              </tr>
            </thead>
            <tbody class="bg-white divide-y divide-gray-200">
              <tr v-for="user in users" :key="user.id" class="hover:bg-gray-50">
                <td class="px-6 py-4 whitespace-nowrap">
                  <div class="flex items-center">
                    <div
                      class="flex-shrink-0 h-10 w-10 bg-gray-200 rounded-full flex items-center justify-center"
                    >
                      <span class="text-gray-700 font-semibold text-sm">
                        {{
                          (user.username || user.email || "??")
                            .substring(0, 2)
                            .toUpperCase()
                        }}
                      </span>
                    </div>
                    <div class="ml-4">
                      <div class="text-sm font-medium text-gray-900">
                        {{ user.username || user.email || "N/A" }}
                      </div>
                      <div class="text-sm text-gray-500">
                        {{ user.firstName || "" }} {{ user.lastName || "" }}
                      </div>
                    </div>
                  </div>
                </td>
                <td class="px-6 py-4 whitespace-nowrap">
                  <div class="text-sm text-gray-900">{{ user.email }}</div>
                </td>
                <td class="px-6 py-4 whitespace-nowrap">
                  <span
                    :class="[
                      'px-2 inline-flex text-xs leading-5 font-semibold rounded-full',
                      user.enabled
                        ? 'bg-green-100 text-green-800'
                        : 'bg-red-100 text-red-800',
                    ]"
                  >
                    {{ user.enabled ? "Active" : "Inactive" }}
                  </span>
                </td>
                <td class="px-6 py-4 whitespace-nowrap">
                  <span
                    :class="[
                      'px-2 inline-flex text-xs leading-5 font-semibold rounded-full',
                      isAdmin(user)
                        ? 'bg-gray-800 text-white'
                        : 'bg-blue-100 text-blue-800',
                    ]"
                  >
                    {{ getUserRoleBadge(user) }}
                  </span>
                </td>
                <td class="px-6 py-4 whitespace-nowrap text-sm font-medium">
                  <div class="relative group">
                    <select
                      :value="getUserRole(user)"
                      @change="
                        (e) =>
                          updateUserRole(
                            user,
                            (e.target as HTMLSelectElement).value,
                          )
                      "
                      :disabled="
                        updatingUserId === user.id || !canChangeRole(user)
                      "
                      class="block w-full px-3 py-2 text-sm border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-gray-900 focus:border-gray-900 disabled:bg-gray-100 disabled:cursor-not-allowed"
                    >
                      <option value="orgUser">Org User</option>
                      <option value="orgAdmin">Org Admin</option>
                    </select>
                    <!-- Tooltip for current user -->
                    <div
                      v-if="isCurrentUser(user) && isAdmin(user)"
                      class="absolute bottom-full left-1/2 transform -translate-x-1/2 mb-2 px-3 py-2 bg-gray-900 text-white text-xs rounded opacity-0 group-hover:opacity-100 transition-opacity pointer-events-none whitespace-nowrap z-10"
                    >
                      You cannot demote yourself
                    </div>
                  </div>
                </td>
              </tr>
            </tbody>
          </table>
        </div>

        <!-- Empty State -->
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
              d="M12 4.354a4 4 0 110 5.292M15 21H3v-1a6 6 0 0112 0v1zm0 0h6v-1a6 6 0 00-9-5.197M13 7a4 4 0 11-8 0 4 4 0 018 0z"
            />
          </svg>
          <h3 class="mt-2 text-sm font-medium text-gray-900">No users found</h3>
          <p class="mt-1 text-sm text-gray-500">
            Get started by adding users in Keycloak.
          </p>
        </div>
      </Card>

      <!-- Info Card -->
      <Card class="mt-6 p-6 bg-gray-100 border-gray-300">
        <div class="flex">
          <div class="flex-shrink-0">
            <svg
              class="h-5 w-5 text-gray-700"
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
            <h3 class="text-sm font-medium text-gray-900">
              Admin Portal Information
            </h3>
            <div class="mt-2 text-sm text-gray-700">
              <ul class="list-disc list-inside space-y-1">
                <li>
                  <strong>Org User:</strong> Can log in and use the full user
                  portal
                </li>
                <li>
                  <strong>Org Admin:</strong> Can log in and use both the user
                  portal and admin portal
                </li>
                <li>Use the dropdown above to change user roles</li>
              </ul>
            </div>
          </div>
        </div>
      </Card>
    </main>

    <!-- New User Modal -->
    <NewUserModal
      :show="showNewUserModal"
      @close="showNewUserModal = false"
      @save="handleCreateUser"
    />
  </div>
</template>
