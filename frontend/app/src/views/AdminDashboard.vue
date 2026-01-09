<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { authService, type UserInfo } from '../services/auth'
import { Button, Card, Alert, Spinner } from '../components/ui/index'

const router = useRouter()
const users = ref<UserInfo[]>([])
const loading = ref(true)
const error = ref('')
const successMessage = ref('')
const updatingUserId = ref<string | null>(null)
const currentUserId = ref<string | null>(null)

onMounted(async () => {
  // Verify admin access
  if (!authService.isAdmin()) {
    router.push('/login')
    return
  }

  // Get current user ID
  currentUserId.value = authService.getCurrentUserId()

  await loadUsers()
})

const loadUsers = async () => {
  loading.value = true
  error.value = ''
  try {
    users.value = await authService.getAllUsers()
  } catch (err) {
    error.value = 'Failed to load users'
    console.error('Error loading users:', err)
  } finally {
    loading.value = false
  }
}

const handleLogout = async () => {
  await authService.logout()
}

const toggleUserRole = async (user: UserInfo) => {
  // Prevent self-demotion
  if (user.id === currentUserId.value && isAdmin(user)) {
    error.value = 'You cannot demote yourself. This would lock you out of the admin portal.'
    return
  }

  updatingUserId.value = user.id
  error.value = ''
  successMessage.value = ''

  try {
    const newAdminStatus = !user.roles.includes('admin')
    const success = await authService.updateUserRole(user.id, newAdminStatus)

    if (success) {
      successMessage.value = `Successfully ${newAdminStatus ? 'promoted' : 'demoted'} ${user.username}`
      // Reload users to get updated data
      await loadUsers()
    } else {
      error.value = 'Failed to update user role'
    }
  } catch (err) {
    error.value = 'An error occurred while updating user role'
    console.error('Error updating user role:', err)
  } finally {
    updatingUserId.value = null
  }
}

const isAdmin = (user: UserInfo) => {
  return user.roles.includes('admin')
}

const getUserRoleBadge = (user: UserInfo) => {
  return isAdmin(user) ? 'Admin' : 'User'
}

const isCurrentUser = (user: UserInfo) => {
  return user.id === currentUserId.value
}

const canToggleRole = (user: UserInfo) => {
  // Cannot demote yourself if you're an admin
  return !(isCurrentUser(user) && isAdmin(user))
}
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
      <!-- Messages -->
      <div class="mb-6 space-y-4">
        <Alert v-if="error" variant="destructive">
          {{ error }}
        </Alert>
        <Alert v-if="successMessage" class="bg-green-50 text-green-800 border-green-200">
          {{ successMessage }}
        </Alert>
      </div>

      <!-- Users Management Card -->
      <Card class="p-6">
        <div class="flex items-center justify-between mb-6">
          <h2 class="text-2xl font-semibold text-gray-900">User Management</h2>
          <Button
            @click="loadUsers"
            :disabled="loading"
            class="bg-gray-900 hover:bg-gray-800"
            title="Refresh users"
          >
            <svg class="h-5 w-5" :class="{ 'animate-spin': loading }" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15"/>
            </svg>
          </Button>
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
                <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  User
                </th>
                <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Email
                </th>
                <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Status
                </th>
                <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Role
                </th>
                <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Actions
                </th>
              </tr>
            </thead>
            <tbody class="bg-white divide-y divide-gray-200">
              <tr v-for="user in users" :key="user.id" class="hover:bg-gray-50">
                <td class="px-6 py-4 whitespace-nowrap">
                  <div class="flex items-center">
                    <div class="flex-shrink-0 h-10 w-10 bg-gray-200 rounded-full flex items-center justify-center">
                      <span class="text-gray-700 font-semibold text-sm">
                        {{ user.username.substring(0, 2).toUpperCase() }}
                      </span>
                    </div>
                    <div class="ml-4">
                      <div class="text-sm font-medium text-gray-900">{{ user.username }}</div>
                      <div class="text-sm text-gray-500">{{ user.firstName }} {{ user.lastName }}</div>
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
                        : 'bg-red-100 text-red-800'
                    ]"
                  >
                    {{ user.enabled ? 'Active' : 'Inactive' }}
                  </span>
                </td>
                <td class="px-6 py-4 whitespace-nowrap">
                  <span
                    :class="[
                      'px-2 inline-flex text-xs leading-5 font-semibold rounded-full',
                      isAdmin(user)
                        ? 'bg-gray-800 text-white'
                        : 'bg-blue-100 text-blue-800'
                    ]"
                  >
                    {{ getUserRoleBadge(user) }}
                  </span>
                </td>
                <td class="px-6 py-4 whitespace-nowrap text-sm font-medium">
                  <div class="relative group">
                    <Button
                      @click="toggleUserRole(user)"
                      :disabled="updatingUserId === user.id || !canToggleRole(user)"
                      :class="[
                        'text-sm',
                        isAdmin(user)
                          ? 'bg-orange-600 hover:bg-orange-700 disabled:bg-orange-300'
                          : 'bg-green-600 hover:bg-green-700 disabled:bg-green-300'
                      ]"
                    >
                      <svg v-if="updatingUserId === user.id" class="animate-spin -ml-1 mr-2 h-4 w-4" fill="none" viewBox="0 0 24 24">
                        <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"/>
                        <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"/>
                      </svg>
                      {{ isAdmin(user) ? 'Demote to User' : 'Promote to Admin' }}
                    </Button>
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
          <svg class="mx-auto h-12 w-12 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4.354a4 4 0 110 5.292M15 21H3v-1a6 6 0 0112 0v1zm0 0h6v-1a6 6 0 00-9-5.197M13 7a4 4 0 11-8 0 4 4 0 018 0z"/>
          </svg>
          <h3 class="mt-2 text-sm font-medium text-gray-900">No users found</h3>
          <p class="mt-1 text-sm text-gray-500">Get started by adding users in Keycloak.</p>
        </div>
      </Card>

      <!-- Info Card -->
      <Card class="mt-6 p-6 bg-gray-100 border-gray-300">
        <div class="flex">
          <div class="flex-shrink-0">
            <svg class="h-5 w-5 text-gray-700" fill="currentColor" viewBox="0 0 20 20">
              <path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a1 1 0 000 2v3a1 1 0 001 1h1a1 1 0 100-2v-3a1 1 0 00-1-1H9z" clip-rule="evenodd"/>
            </svg>
          </div>
          <div class="ml-3">
            <h3 class="text-sm font-medium text-gray-900">Admin Portal Information</h3>
            <div class="mt-2 text-sm text-gray-700">
              <ul class="list-disc list-inside space-y-1">
                <li>Admin users can access both the regular portal and admin portal</li>
                <li>Regular users can only access the normal portal</li>
                <li>Use the buttons above to promote users to admin or demote admins to regular users</li>
              </ul>
            </div>
          </div>
        </div>
      </Card>
    </main>
  </div>
</template>
