<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { authService } from '../services/auth'
import UserLoginForm from '../components/UserLoginForm.vue'
import AdminLoginForm from '../components/AdminLoginForm.vue'

const router = useRouter()
const error = ref('')
const loading = ref(false)
const tokenExpiredMessage = ref(false)
const isAdminMode = ref(false)
const isFlipping = ref(false)

onMounted(() => {
  // Check if user was redirected due to token expiration
  if (authService.wasRedirectedDueToExpiration()) {
    tokenExpiredMessage.value = true
    authService.clearExpirationFlag()
  }
})

const toggleAdminMode = () => {
  if (isFlipping.value) return
  
  isFlipping.value = true
  isAdminMode.value = !isAdminMode.value
  error.value = ''
  tokenExpiredMessage.value = false
  
  // Reset flipping state after animation completes
  setTimeout(() => {
    isFlipping.value = false
  }, 600)
}

const handleLogin = async (credentials: { username: string; password: string }) => {
  error.value = ''
  tokenExpiredMessage.value = false
  loading.value = true

  try {
    const success = await authService.login(credentials, isAdminMode.value)

    if (success) {
      if (isAdminMode.value) {
        router.push('/admin')
      } else {
        router.push('/dashboard')
      }
    } else {
      error.value = isAdminMode.value
        ? 'Invalid credentials or insufficient permissions'
        : 'Invalid username or password'
    }
  } catch {
    error.value = 'An error occurred during login'
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
              :error="!isAdminMode ? error : ''"
              :loading="loading"
              @submit="handleLogin"
            />
          </div>

          <!-- Back Side (Admin Login) -->
          <div class="flip-side back">
            <AdminLoginForm
              :token-expired-message="tokenExpiredMessage && isAdminMode"
              :error="isAdminMode ? error : ''"
              :loading="loading"
              @submit="handleLogin"
            />
          </div>
        </div>
      </div>

      <!-- Toggle Button at Bottom Center -->
      <div class="flex justify-center mt-8">
        <button
          @click="toggleAdminMode"
          :disabled="isFlipping"
          :class="[
            'px-6 py-2 rounded-lg font-medium transition-colors',
            isAdminMode
              ? 'bg-gray-900 text-white hover:bg-gray-800'
              : 'bg-blue-600 text-white hover:bg-blue-700',
            isFlipping && 'opacity-50 cursor-not-allowed'
          ]"
        >
          {{ isAdminMode ? 'User' : 'Admin' }}
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
