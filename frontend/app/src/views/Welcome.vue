<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '../stores/auth'
import Button from '../components/ui/Button.vue'
import Card from '../components/ui/Card.vue'

const router = useRouter()
const authStore = useAuthStore()
const username = ref('')
const loading = ref(true)

onMounted(async () => {
  const userInfo = authStore.getUserInfo()
  if (userInfo) {
    username.value = userInfo.username
  }
  loading.value = false
})

const handleLogout = async () => {
  await authStore.logout()
  // Router push is handled by the logout method
}
</script>

<template>
  <div class="min-h-screen flex items-center justify-center bg-gradient-to-br from-green-50 to-emerald-100 p-4">
    <Card class="w-full max-w-2xl p-12">
      <div class="text-center space-y-6">
        <div class="space-y-2">
          <h1 class="text-4xl font-bold text-gray-900">
            Welcome {{ username || 'User' }}! 🎉
          </h1>
          <p class="text-lg text-gray-600">
            You have successfully logged in to the application
          </p>
        </div>

        <div v-if="loading" class="text-gray-500">
          Loading user information...
        </div>

        <div class="pt-6 space-y-4">
          <div class="bg-blue-50 border border-blue-200 rounded-lg p-4">
            <p class="text-sm text-blue-800">
              <strong>Authentication Status:</strong> Authenticated via Keycloak OAuth2.0
            </p>
          </div>

          <div class="bg-green-50 border border-green-200 rounded-lg p-4">
            <p class="text-sm text-green-800">
              <strong>Backend API:</strong> Connected to .NET Web API
            </p>
          </div>
        </div>

        <div class="pt-6">
          <Button
            @click="handleLogout"
            variant="destructive"
            size="lg"
          >
            Sign Out
          </Button>
        </div>
      </div>
    </Card>
  </div>
</template>
