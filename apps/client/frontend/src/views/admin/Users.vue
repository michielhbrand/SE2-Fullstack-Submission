<script setup lang="ts">
import { onMounted, ref } from "vue";
import { useRouter } from "vue-router";
import { useAuthStore, type UserInfo } from "../../stores/auth";
import { Button, Card, Spinner, Skeleton, Badge, Avatar, AvatarFallback, ToggleGroup, ToggleGroupItem, Table, TableHeader, TableBody, TableHead, TableRow, TableCell } from "../../components/ui/index";
import { toast } from "vue-sonner";
import NewUserModal from "../../components/modals/NewUserModal.vue";
import EditUserModal from "../../components/modals/EditUserModal.vue";

const router = useRouter();
const authStore = useAuthStore();
const users = ref<UserInfo[]>([]);
const allUsers = ref<UserInfo[]>([]);
const loading = ref(true);
const currentUserId = ref<string | null>(null);
const showNewUserModal = ref(false);
const showEditUserModal = ref(false);
const selectedUser = ref<UserInfo | null>(null);
const creatingUser = ref(false);
const updatingUser = ref(false);
const showActiveOnly = ref(true);

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
    allUsers.value = fetchedUsers;
    filterUsers();
  } catch (err) {
    toast.error("Failed to load users");
    console.error("Error loading users:", err);
  } finally {
    loading.value = false;
  }
};

const filterUsers = () => {
  if (showActiveOnly.value) {
    users.value = allUsers.value.filter((user) => user.enabled);
  } else {
    users.value = allUsers.value.filter((user) => !user.enabled);
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

const handleEditUser = (user: UserInfo) => {
  selectedUser.value = user;
  showEditUserModal.value = true;
};

const handleUpdateUser = async (userData: {
  firstName: string;
  lastName: string;
  role: string;
  active: boolean;
}) => {
  if (!selectedUser.value) return;

  updatingUser.value = true;
  try {
    const success = await authStore.updateUser(selectedUser.value.id, userData);

    if (success) {
      toast.success(
        `Successfully updated ${selectedUser.value.username || selectedUser.value.email || "user"}`,
      );
      showEditUserModal.value = false;
      selectedUser.value = null;
      await loadUsers();
    } else {
      toast.error("Failed to update user details");
    }
  } catch (err: any) {
    console.error("Error updating user:", err);
    toast.error("An error occurred while updating user details");
  } finally {
    updatingUser.value = false;
  }
};
</script>

<template>
  <div class="p-8">
    <!-- Users Management Card -->
    <Card class="p-6">
      <div class="flex items-center justify-between mb-6">
        <div class="flex items-center gap-4">
          <h2 class="text-2xl font-semibold text-gray-900">User Management</h2>
          <ToggleGroup type="single" :model-value="showActiveOnly ? 'active' : 'inactive'" @update:model-value="(value) => { showActiveOnly = value === 'active'; filterUsers(); }">
            <ToggleGroupItem value="active">
              Active ({{ allUsers.filter(u => u.enabled).length }})
            </ToggleGroupItem>
            <ToggleGroupItem value="inactive">
              Inactive ({{ allUsers.filter(u => !u.enabled).length }})
            </ToggleGroupItem>
          </ToggleGroup>
        </div>
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
      <div v-if="loading" class="space-y-3">
        <Skeleton class="h-16 w-full" />
        <Skeleton class="h-16 w-full" />
        <Skeleton class="h-16 w-full" />
        <Skeleton class="h-16 w-full" />
      </div>

      <!-- Users Table -->
      <div v-else-if="users.length > 0" class="overflow-x-auto">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>User</TableHead>
              <TableHead>Email</TableHead>
              <TableHead>Status</TableHead>
              <TableHead>Role</TableHead>
              <TableHead>Actions</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            <TableRow v-for="user in users" :key="user.id">
              <TableCell>
                <div class="flex items-center">
                  <Avatar class="h-10 w-10">
                    <AvatarFallback>
                      {{
                        (user.username || user.email || "??")
                          .substring(0, 2)
                          .toUpperCase()
                      }}
                    </AvatarFallback>
                  </Avatar>
                  <div class="ml-4">
                    <div class="text-sm font-medium text-gray-900">
                      {{ user.username || user.email || "N/A" }}
                    </div>
                    <div class="text-sm text-gray-500">
                      {{ user.firstName || "" }} {{ user.lastName || "" }}
                    </div>
                  </div>
                </div>
              </TableCell>
              <TableCell>{{ user.email }}</TableCell>
              <TableCell>
                <Badge :variant="user.enabled ? 'default' : 'destructive'">
                  {{ user.enabled ? "Active" : "Inactive" }}
                </Badge>
              </TableCell>
              <TableCell>
                <Badge :variant="isAdmin(user) ? 'default' : 'secondary'">
                  {{ getUserRoleBadge(user) }}
                </Badge>
              </TableCell>
              <TableCell>
                <button
                  @click="handleEditUser(user)"
                  class="inline-flex items-center px-3 py-2 text-sm font-medium text-blue-600 hover:text-blue-900 hover:bg-blue-50 rounded-md transition-colors"
                  title="Edit user"
                >
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
                      d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z"
                    />
                  </svg>
                  Edit
                </button>
              </TableCell>
            </TableRow>
          </TableBody>
        </Table>
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
            User Management Information
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
              <li>Click the <strong>Edit</strong> button to modify user details, role, and active status</li>
            </ul>
          </div>
        </div>
      </div>
    </Card>

    <NewUserModal
      :show="showNewUserModal"
      @close="showNewUserModal = false"
      @save="handleCreateUser"
    />

    <EditUserModal
      :show="showEditUserModal"
      :user="selectedUser"
      :current-user-id="currentUserId"
      :loading="updatingUser"
      @close="showEditUserModal = false"
      @save="handleUpdateUser"
    />
  </div>
</template>
