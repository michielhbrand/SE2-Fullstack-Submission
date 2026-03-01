<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '../stores/auth'
import UserLoginForm from '../components/UserLoginForm.vue'
import AdminLoginForm from '../components/AdminLoginForm.vue'
import { toast } from 'vue-sonner'

const router = useRouter()
const authStore = useAuthStore()
const loading = ref(false)
const tokenExpiredMessage = ref(false)
const isAdminMode = ref(false)
const isFlipping = ref(false)

onMounted(() => {
  // Check if user was redirected due to token expiration
  if (authStore.wasRedirectedDueToExpiration()) {
    tokenExpiredMessage.value = true
    authStore.clearExpirationFlag()
  }
})

const toggleAdminMode = () => {
  if (isFlipping.value) return
  
  isFlipping.value = true
  isAdminMode.value = !isAdminMode.value
  tokenExpiredMessage.value = false
  
  // Reset flipping state after animation completes
  setTimeout(() => {
    isFlipping.value = false
  }, 600)
}

const handleLogin = async (credentials: { username: string; password: string }) => {
  tokenExpiredMessage.value = false
  loading.value = true

  try {
    const success = await authStore.login(credentials, isAdminMode.value)

    if (success) {
      toast.success('Login successful!')
      if (isAdminMode.value) {
        router.push('/admin')
      } else {
        router.push('/dashboard')
      }
    }
    // Error toast is already shown by auth store, no need to show another one here
  } catch {
    // Error toast is already shown by auth store, no need to show another one here
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <div class="min-h-screen flex items-center justify-center bg-gray-50 px-4 sm:px-6 lg:px-8">
    <div class="w-full max-w-md">
      <!-- Flip Container -->
      <div class="flip-container" :class="{ 'is-flipped': isAdminMode }">
        <div class="flipper">
          <!-- Front Side (User Login) -->
          <div class="flip-side front">
            <UserLoginForm
              :token-expired-message="tokenExpiredMessage && !isAdminMode"
              :loading="loading"
              @submit="handleLogin"
            />
          </div>

          <!-- Back Side (Admin Login) -->
          <div class="flip-side back">
            <AdminLoginForm
              :token-expired-message="tokenExpiredMessage && isAdminMode"
              :loading="loading"
              @submit="handleLogin"
            />
          </div>
        </div>
      </div>

      <!-- Toggle Buttons -->
      <div class="flex mt-6 rounded-lg overflow-hidden border border-gray-200">
        <button
          @click="isAdminMode && toggleAdminMode()"
          :disabled="isFlipping"
          :class="[
            'flex-1 py-2.5 text-sm font-medium transition-colors',
            !isAdminMode
              ? 'bg-blue-600 text-white'
              : 'bg-white text-gray-500 hover:bg-gray-50',
            isFlipping && 'opacity-50 cursor-not-allowed'
          ]"
        >
          User
        </button>
        <button
          @click="!isAdminMode && toggleAdminMode()"
          :disabled="isFlipping"
          :class="[
            'flex-1 py-2.5 text-sm font-medium transition-colors border-l border-gray-200',
            isAdminMode
              ? 'bg-gray-900 text-white'
              : 'bg-white text-gray-500 hover:bg-gray-50',
            isFlipping && 'opacity-50 cursor-not-allowed'
          ]"
        >
          Admin
        </button>
      </div>
    </div>
  </div>
</template>

<style scoped>
/* 3D Flip Animation */
.flip-container {
  perspective: 1000px;
  width: 100%;
  min-height: 500px; /* Ensure container has minimum height */
  margin-bottom: 2rem; /* Space for toggle button */
}

.flipper {
  position: relative;
  width: 100%;
  min-height: 500px; /* Match container height */
  transition: transform 0.6s;
  transform-style: preserve-3d;
}

.flip-container.is-flipped .flipper {
  transform: rotateY(180deg);
}

.flip-side {
  width: 100%;
  backface-visibility: hidden;
  -webkit-backface-visibility: hidden;
  position: absolute;
  top: 0;
  left: 0;
}

.flip-side.back {
  transform: rotateY(180deg);
}

.flip-side.front {
  transform: rotateY(0deg);
}
</style>
