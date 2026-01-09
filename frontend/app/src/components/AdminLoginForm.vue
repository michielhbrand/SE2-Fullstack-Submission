<script setup lang="ts">
import { ref } from 'vue'
import { Button, Input, Label, Card, Alert } from './ui/index'

interface Props {
  tokenExpiredMessage?: boolean
  error?: string
  loading?: boolean
}

interface Emits {
  (e: 'submit', credentials: { username: string; password: string }): void
}

const props = withDefaults(defineProps<Props>(), {
  tokenExpiredMessage: false,
  error: '',
  loading: false
})

const emit = defineEmits<Emits>()

const username = ref('')
const password = ref('')
const showPassword = ref(false)

const handleSubmit = () => {
  emit('submit', {
    username: username.value,
    password: password.value
  })
}

const togglePasswordVisibility = () => {
  showPassword.value = !showPassword.value
}
</script>

<template>
  <div>
    <!-- Header -->
    <div class="text-center mb-8">
      <h2 class="text-2xl font-semibold text-gray-900">Admin Sign in</h2>
      <p class="mt-1 text-sm text-gray-500">Access admin portal securely</p>
    </div>

    <!-- Login Card -->
    <Card class="p-6 space-y-6 border-gray-800">
      <!-- Token Expiration Message -->
      <div v-if="tokenExpiredMessage" class="text-sm text-red-600 text-center">
        Your session has expired. Please sign in again.
      </div>

      <!-- Error Alert -->
      <Alert v-if="error" variant="destructive">
        {{ error }}
      </Alert>

      <!-- Login Form -->
      <form @submit.prevent="handleSubmit" class="space-y-4">
        <div class="space-y-1">
          <Label for="admin-username">Username</Label>
          <Input
            id="admin-username"
            v-model="username"
            type="text"
            placeholder="Enter username"
            :disabled="loading"
            class="h-11 border-gray-800 focus:ring-gray-800"
            required
          />
        </div>

        <div class="space-y-1">
          <div class="flex items-center justify-between">
            <Label for="admin-password">Password</Label>
            <button 
              type="button" 
              class="text-sm text-gray-800 hover:underline"
              tabindex="-1"
            >
              Forgot?
            </button>
          </div>
          <div class="relative">
            <Input
              id="admin-password"
              v-model="password"
              :type="showPassword ? 'text' : 'password'"
              placeholder="Enter password"
              :disabled="loading"
              class="h-11 pr-10 border-gray-800 focus:ring-gray-800"
              required
            />
            <button
              type="button"
              @click="togglePasswordVisibility"
              class="absolute inset-y-0 right-0 pr-3 flex items-center text-gray-400 hover:text-gray-600"
              tabindex="-1"
            >
              <svg v-if="!showPassword" class="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                      d="M15 12a3 3 0 11-6 0 3 3 0 016 0z"/>
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                      d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z"/>
              </svg>
              <svg v-else class="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                      d="M13.875 18.825A10.05 10.05 0 0112 19c-4.478 0-8.268-2.943-9.543-7a9.97 9.97 0 011.563-3.029m5.858.908a3 3 0 114.243 4.243M9.878 9.878l4.242 4.242"/>
              </svg>
            </button>
          </div>
        </div>

        <div class="flex items-center space-x-2">
          <input 
            id="admin-remember" 
            type="checkbox" 
            class="h-4 w-4 text-gray-800 focus:ring-gray-800 border-gray-300 rounded"
          />
          <Label for="admin-remember" class="text-sm text-gray-700">Remember me</Label>
        </div>

        <Button 
          type="submit" 
          class="w-full h-11 bg-gray-900 hover:bg-gray-800 focus:ring-gray-900"
          :disabled="loading"
        >
          <svg v-if="loading" class="animate-spin -ml-1 mr-2 h-5 w-5" fill="none" viewBox="0 0 24 24">
            <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"/>
            <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"/>
          </svg>
          {{ loading ? 'Signing in...' : 'Sign in' }}
        </Button>
      </form>
    </Card>

    <p class="mt-6 text-center text-sm text-gray-500">
      Protected by OAuth2.0
    </p>
  </div>
</template>
